[CmdletBinding()]
param(
    [string]$HostName = 'localhost',
    [int]$Port = 8080,
    [int]$Requests = 50
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$plan = Join-Path $repoRoot 'performance-tests\nginx-rate-limit.jmx'
$resultRoot = Join-Path $repoRoot 'performance-tests\results'
New-Item -ItemType Directory -Force -Path $resultRoot | Out-Null
$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$runDirectory = Join-Path $resultRoot "rate-limit-$runId"
$reportDirectory = Join-Path $runDirectory 'report'
$jtl = Join-Path $runDirectory 'results.jtl'
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

$jmeterCommand = Get-Command jmeter -ErrorAction SilentlyContinue
if ($jmeterCommand) { $jmeter = $jmeterCommand.Source }
elseif ($env:JMETER_HOME -and (Test-Path (Join-Path $env:JMETER_HOME 'bin\jmeter.bat'))) { $jmeter = Join-Path $env:JMETER_HOME 'bin\jmeter.bat' }
elseif (Test-Path 'D:\Tools\JMeter\apache-jmeter-5.6.3\bin\jmeter.bat') { $jmeter = 'D:\Tools\JMeter\apache-jmeter-5.6.3\bin\jmeter.bat' }
else { throw 'JMeter was not found. Add jmeter to PATH or set JMETER_HOME.' }

$previousJavaToolOptions = $env:JAVA_TOOL_OPTIONS
$previousHeap = $env:HEAP
$previousGcAlgorithm = $env:GC_ALGO
try {
    # Keep JMeter usable beside SQL Server and Docker Desktop on an 8 GB workstation.
    $env:JAVA_TOOL_OPTIONS = '-XX:+UseSerialGC -Xms16m -Xmx32m'
    $env:HEAP = '-Xms32m -Xmx128m -XX:MaxMetaspaceSize=96m'
    $env:GC_ALGO = '-XX:+UseSerialGC'
    & $jmeter -n -t $plan "-Jhost=$HostName" "-Jport=$Port" "-Jrequests=$Requests" "-JloginRequests=12" `
        -Jjmeter.save.saveservice.output_format=csv `
        -Jjmeter.save.saveservice.print_field_names=true `
        -l $jtl -e -o $reportDirectory
    $jmeterExitCode = $LASTEXITCODE
} finally {
    $env:JAVA_TOOL_OPTIONS = $previousJavaToolOptions
    $env:HEAP = $previousHeap
    $env:GC_ALGO = $previousGcAlgorithm
}
if ($jmeterExitCode -ne 0 -or -not (Test-Path $jtl) -or -not (Test-Path (Join-Path $reportDirectory 'index.html'))) {
    throw "JMeter failed (exit code $jmeterExitCode) or did not produce complete JTL/HTML results."
}

$rows = @(Import-Csv $jtl)
$catalog = @($rows | Where-Object label -eq 'Burst catalog request')
$login = @($rows | Where-Object label -eq 'Login brute-force simulation')
$catalogOk = @($catalog | Where-Object responseCode -eq '200').Count
$catalogLimited = @($catalog | Where-Object responseCode -eq '429').Count
$loginUnauthorized = @($login | Where-Object responseCode -eq '401').Count
$loginLimited = @($login | Where-Object responseCode -eq '429').Count
$unexpected = @($catalog | Where-Object { $_.responseCode -notin @('200', '429') }).Count +
    @($login | Where-Object { $_.responseCode -notin @('401', '429') }).Count
Write-Host "Catalog: HTTP 200=$catalogOk, HTTP 429=$catalogLimited"
Write-Host "Login abuse: HTTP 401=$loginUnauthorized, HTTP 429=$loginLimited, unexpected=$unexpected"
if ($catalogOk -eq 0 -or $catalogLimited -eq 0 -or $loginUnauthorized -eq 0 -or $loginLimited -eq 0 -or $unexpected -gt 0) {
    throw 'Rate-limit verification failed for the catalog burst or login abuse scenario.'
}

$retryAfterObserved = $false
for ($i = 0; $i -lt 50; $i++) {
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "http://${HostName}:$Port/api/products?pageSize=1" -TimeoutSec 5 | Out-Null
    } catch {
        $response = $_.Exception.Response
        if ($response -and [int]$response.StatusCode -eq 429) {
            $retryAfterObserved = -not [string]::IsNullOrWhiteSpace($response.Headers['Retry-After'])
            break
        }
        throw
    }
}
if (-not $retryAfterObserved) { throw 'HTTP 429 did not include the required Retry-After header.' }
Write-Host 'PASS: Nginx allowed normal traffic and rejected the burst with HTTP 429.'
Write-Host 'PASS: HTTP 429 included Retry-After.'
Write-Host "Report: $reportDirectory\index.html"
