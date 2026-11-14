[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'docker-compose.loadtest.yml'
$dockerfile = Join-Path $repoRoot 'infrastructure\docker\backend.Dockerfile'

Push-Location $repoRoot
try {
    Write-Host 'Stopping any previous load-test containers to free build resources...'
    & docker compose -f $composeFile stop
    if ($LASTEXITCODE -ne 0) { throw "Could not stop the previous stack (code $LASTEXITCODE)." }

    Write-Host 'Building the .NET API image once...'
    & docker build -f $dockerfile -t ebay-clone-buyer-api:loadtest .
    if ($LASTEXITCODE -ne 0) { throw "Docker build failed with code $LASTEXITCODE." }

    if ($env:OS -eq 'Windows_NT') {
        $os = Get-CimInstance Win32_OperatingSystem
        $freeVirtualMb = [math]::Round($os.FreeVirtualMemory / 1024)
        if ($freeVirtualMb -lt 1024) {
            Write-Host "Only $freeVirtualMb MB virtual memory remains; restarting Docker Desktop before the test stack starts..."
            & docker desktop restart
            if ($LASTEXITCODE -ne 0) { throw "Docker Desktop restart failed with code $LASTEXITCODE." }
        }
    }

    Write-Host 'Starting SQL Server, two API instances and Nginx...'
    & docker compose -f $composeFile up -d --no-build
    if ($LASTEXITCODE -ne 0) { throw "Docker Compose failed with code $LASTEXITCODE." }

    & docker compose -f $composeFile ps
    Write-Host 'PASS: load-test stack is running.'
    Write-Host 'Rate-limited endpoint: http://localhost:8080'
    Write-Host 'Capacity endpoint:     http://localhost:8081'
} finally {
    Pop-Location
}
