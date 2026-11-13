[CmdletBinding()]
param(
    [ValidateSet('smoke', 'load', 'stress', 'spike', 'soak')]
    [string]$Profile = 'smoke',
    [string]$HostName = 'localhost',
    [int]$Port = 8081,
    [int]$Users = 0,
    [int]$RampSeconds = 0,
    [int]$DurationSeconds = 0,
    [double]$MaxErrorPercent = 1.0,
    [int]$MaxP95Milliseconds = 500
)

$ErrorActionPreference = 'Stop'
$profiles = @{
    smoke  = @{ users = 5;   ramp = 5;   duration = 30;   think = 250 }
    load   = @{ users = 50;  ramp = 60;  duration = 300;  think = 250 }
    stress = @{ users = 200; ramp = 60;  duration = 300;  think = 100 }
    spike  = @{ users = 200; ramp = 10;  duration = 180;  think = 100 }
    soak   = @{ users = 100; ramp = 120; duration = 3600; think = 250 }
}

$selected = $profiles[$Profile]
if ($Users -le 0) { $Users = $selected.users }
if ($RampSeconds -le 0) { $RampSeconds = $selected.ramp }
if ($DurationSeconds -le 0) { $DurationSeconds = $selected.duration }

$jmeterCommand = Get-Command jmeter -ErrorAction SilentlyContinue
if ($jmeterCommand) {
    $jmeter = $jmeterCommand.Source
} elseif ($env:JMETER_HOME -and (Test-Path (Join-Path $env:JMETER_HOME 'bin\jmeter.bat'))) {
    $jmeter = Join-Path $env:JMETER_HOME 'bin\jmeter.bat'
} elseif (Test-Path 'D:\Tools\JMeter\apache-jmeter-5.6.3\bin\jmeter.bat') {
    $jmeter = 'D:\Tools\JMeter\apache-jmeter-5.6.3\bin\jmeter.bat'
} else {
    throw 'JMeter was not found. Add jmeter to PATH or set JMETER_HOME.'
}

$baseUrl = "http://${HostName}:$Port"
try {
    $ready = Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/health/ready" -TimeoutSec 10
    if ($ready.StatusCode -ne 200) { throw "Readiness returned $($ready.StatusCode)" }
} catch {
    throw "Load-test target is not ready at $baseUrl. Start docker-compose.loadtest.yml first. $($_.Exception.Message)"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$plan = Join-Path $repoRoot 'performance-tests\buyer-load.jmx'
$resultRoot = Join-Path $repoRoot 'performance-tests\results'
New-Item -ItemType Directory -Force -Path $resultRoot | Out-Null
$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$runDirectory = Join-Path $resultRoot "$Profile-$runId"
$reportDirectory = Join-Path $runDirectory 'report'
$jtl = Join-Path $runDirectory 'results.jtl'
$jmeterLog = Join-Path $runDirectory 'jmeter.log'
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

$arguments = @(
    '-n', '-t', $plan,
    "-Jhost=$HostName", "-Jport=$Port",
    "-Jusers=$Users", "-Jramp=$RampSeconds", "-Jduration=$DurationSeconds",
    "-Jthink=$($selected.think)",
    '-Jjmeter.save.saveservice.output_format=csv',
    '-Jjmeter.save.saveservice.print_field_names=true',
    '-l', $jtl, '-j', $jmeterLog, '-e', '-o', $reportDirectory
)

Write-Host "Running ${Profile}: users=$Users ramp=${RampSeconds}s duration=${DurationSeconds}s target=$baseUrl"
$previousJavaToolOptions = $env:JAVA_TOOL_OPTIONS
$previousHeap = $env:HEAP
$previousGcAlgorithm = $env:GC_ALGO
try {
    # Keep JMeter usable beside SQL Server and Docker Desktop on an 8 GB workstation.
    $env:JAVA_TOOL_OPTIONS = '-XX:+UseSerialGC -Xms16m -Xmx32m'
    $env:HEAP = '-Xms32m -Xmx128m -XX:MaxMetaspaceSize=96m'
    $env:GC_ALGO = '-XX:+UseSerialGC'
    & $jmeter @arguments
    $jmeterExitCode = $LASTEXITCODE
} finally {
    $env:JAVA_TOOL_OPTIONS = $previousJavaToolOptions
    $env:HEAP = $previousHeap
    $env:GC_ALGO = $previousGcAlgorithm
}
if ($jmeterExitCode -ne 0 -or -not (Test-Path $jtl) -or -not (Test-Path (Join-Path $reportDirectory 'index.html'))) {
    throw "JMeter failed (exit code $jmeterExitCode). See $jmeterLog"
}

$rows = @(Import-Csv $jtl)
if ($rows.Count -eq 0) { throw 'JMeter produced no samples.' }
$elapsed = @($rows | ForEach-Object { [int]$_.elapsed } | Sort-Object)
$p95Index = [Math]::Min($elapsed.Count - 1, [Math]::Max(0, [Math]::Ceiling($elapsed.Count * 0.95) - 1))
$p99Index = [Math]::Min($elapsed.Count - 1, [Math]::Max(0, [Math]::Ceiling($elapsed.Count * 0.99) - 1))
$errors = @($rows | Where-Object { $_.success -ne 'true' }).Count
$errorPercent = [Math]::Round($errors * 100.0 / $rows.Count, 3)
$firstTimestamp = ($rows | Measure-Object -Property timeStamp -Minimum).Minimum
$lastTimestamp = ($rows | Measure-Object -Property timeStamp -Maximum).Maximum
$wallSeconds = [Math]::Max(1, ([double]$lastTimestamp - [double]$firstTimestamp) / 1000.0)
$throughput = [Math]::Round($rows.Count / $wallSeconds, 2)
$summary = [ordered]@{
    profile = $Profile
    target = $baseUrl
    users = $Users
    durationSeconds = $DurationSeconds
    samples = $rows.Count
    errors = $errors
    errorPercent = $errorPercent
    p95Milliseconds = $elapsed[$p95Index]
    p99Milliseconds = $elapsed[$p99Index]
    maxMilliseconds = $elapsed[-1]
    throughputRequestsPerSecond = $throughput
    passed = ($errorPercent -le $MaxErrorPercent -and $elapsed[$p95Index] -le $MaxP95Milliseconds)
    report = $reportDirectory
}
$summary | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $runDirectory 'summary.json')
$summary | Format-List

if (-not $summary.passed) {
    throw "Performance gate failed: error=$errorPercent% (max $MaxErrorPercent%), p95=$($summary.p95Milliseconds)ms (max ${MaxP95Milliseconds}ms)."
}

Write-Host "PASS. Open $reportDirectory\index.html"
