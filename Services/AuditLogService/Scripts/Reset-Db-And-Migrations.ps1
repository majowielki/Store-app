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

  Write-Host "[AuditLogService] Restoring tools..." -ForegroundColor Cyan
  dotnet tool restore

  if (-not $KeepMigrations -and (Test-Path "Migrations")) {
    Write-Host "[AuditLogService] Removing existing Migrations folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "Migrations"
  }

  if (-not $NoDrop) {
    Write-Host "[AuditLogService] Dropping database (if exists)..." -ForegroundColor Yellow
    dotnet ef database drop -f --no-build
  } else {
    Write-Host "[AuditLogService] Skipping database drop" -ForegroundColor DarkYellow
  }

  Write-Host "[AuditLogService] Adding migration: $MigrationName" -ForegroundColor Cyan
  dotnet ef migrations add $MigrationName

  Write-Host "[AuditLogService] Updating database..." -ForegroundColor Cyan
  dotnet ef database update

  Write-Host "[AuditLogService] Done." -ForegroundColor Green
}
finally {
  Pop-Location
}
