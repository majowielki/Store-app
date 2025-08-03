# Simple PowerShell script to test Docker Compose setup
Write-Host "🚀 Testing Store App Docker Setup..." -ForegroundColor Green

# Change to Infrastructure directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$InfraDir = Split-Path -Parent $ScriptDir
Set-Location $InfraDir

Write-Host "Working directory: $InfraDir" -ForegroundColor Gray

# Test Docker Compose file
Write-Host "📋 Validating docker-compose.yml..." -ForegroundColor Yellow
docker-compose config --quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Docker Compose configuration is valid!" -ForegroundColor Green
} else {
    Write-Host "❌ Docker Compose configuration has errors!" -ForegroundColor Red
    exit 1
}

# Check if Docker is running
Write-Host "🐳 Checking Docker status..." -ForegroundColor Yellow
docker version --format "{{.Server.Version}}" 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Docker is running!" -ForegroundColor Green
} else {
    Write-Host "❌ Docker is not running or not accessible!" -ForegroundColor Red
    Write-Host "💡 Please start Docker Desktop and try again." -ForegroundColor Yellow
    exit 1
}

# Show available services
Write-Host "📊 Available services:" -ForegroundColor Cyan
docker-compose ps -a

Write-Host ""
Write-Host "🎯 Ready to run! Use the following commands:" -ForegroundColor Magenta
Write-Host "   Start all services:  .\build-and-run.ps1 -Build" -ForegroundColor White
Write-Host "   View logs:           .\build-and-run.ps1 -Logs" -ForegroundColor White
Write-Host "   Stop services:       .\build-and-run.ps1 -Stop" -ForegroundColor White
Write-Host "   Clean up:            .\build-and-run.ps1 -Clean" -ForegroundColor White
