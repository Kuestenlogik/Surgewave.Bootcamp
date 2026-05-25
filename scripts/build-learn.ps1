param(
    [switch]$Serve,
    [switch]$Clean,
    [int]$Port = 8081
)

$ErrorActionPreference = 'Stop'

if ($Clean) {
    Write-Host "Cleaning artifacts..." -ForegroundColor Yellow
    if (Test-Path "artifacts/docs-learn") { Remove-Item -Recurse -Force "artifacts/docs-learn" }
}

Write-Host "Building Surgewave Bootcamp documentation..." -ForegroundColor Cyan

Push-Location .docfx
try {
    if ($Serve) {
        docfx build docfx.json --serve --port $Port
    } else {
        docfx build docfx.json
    }
} finally {
    Pop-Location
}

Write-Host "Build complete. Output: artifacts/docs-learn/" -ForegroundColor Green
