[CmdletBinding()]
param(
    [string]$HostName = 'localhost',
    [int]$Port = 8081,
    [int]$Requests = 20
)

$ErrorActionPreference = 'Stop'
$observed = @{}
for ($i = 1; $i -le $Requests; $i++) {
    $response = Invoke-WebRequest -UseBasicParsing -Uri "http://${HostName}:$Port/api/system/status" -TimeoutSec 10
    $body = $response.Content | ConvertFrom-Json
    $instance = [string]$body.instance
    if (-not $observed.ContainsKey($instance)) { $observed[$instance] = 0 }
    $observed[$instance]++
    Write-Host ("{0,2}: instance={1}, upstream={2}" -f $i, $instance, $response.Headers['X-Upstream-Addr'])
}

Write-Host 'Distribution:'
$observed.GetEnumerator() | Sort-Object Name | Format-Table Name, Value -AutoSize
if ($observed.Keys.Count -lt 2) {
    throw "Load balancing failed: only observed $($observed.Keys -join ', ')."
}
Write-Host 'PASS: requests were served by at least two .NET API instances.'
