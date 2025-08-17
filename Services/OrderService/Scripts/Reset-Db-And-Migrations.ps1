param(
  [string]$MigrationName,
  [switch]$NoDrop = $false,
  [switch]$KeepMigrations = $false
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projDir = Join-Path $scriptDir '..'
Push-Location $projDir

try {
  $timestamp = Get-Date -Format "yyyyMMddHHmmss"
  if (-not $MigrationName -or [string]::IsNullOrWhiteSpace($MigrationName)) {
    $MigrationName = "Auto_${timestamp}"
  }

  Write-Host "[OrderService] Restoring tools..." -ForegroundColor Cyan
  dotnet tool restore

  if (-not $KeepMigrations -and (Test-Path "Migrations")) {
    Write-Host "[OrderService] Removing existing Migrations folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "Migrations"
  }

  if (-not $NoDrop) {
    Write-Host "[OrderService] Dropping database (if exists)..." -ForegroundColor Yellow
    dotnet ef database drop -f --no-build
  } else {
    Write-Host "[OrderService] Skipping database drop" -ForegroundColor DarkYellow
  }

  Write-Host "[OrderService] Adding migration: $MigrationName" -ForegroundColor Cyan
  dotnet ef migrations add $MigrationName

  Write-Host "[OrderService] Updating database..." -ForegroundColor Cyan
  dotnet ef database update

  Write-Host "[OrderService] Done." -ForegroundColor Green
}
finally {
  Pop-Location
}
