param(
  [string]$RootPath,
  [string]$ProductMigration,
  [string]$IdentityMigration,
  [string]$OrderMigration,
  [string]$CartMigration,
  [string]$AuditMigration,
  [switch]$NoDrop = $false,
  [switch]$KeepMigrations = $false
)

$ErrorActionPreference = 'Stop'

function Resolve-BasePath {
  param([string]$Root)
  # 1) Explicit RootPath
  if ($Root -and (Test-Path $Root)) { return (Resolve-Path $Root).Path }

  # 2) Script location (works when running the .ps1 file)
  if ($PSScriptRoot) {
    $parent = Split-Path $PSScriptRoot -Parent
    if (Test-Path (Join-Path $PSScriptRoot 'Services')) { return $PSScriptRoot }
    if (Test-Path (Join-Path $parent 'Services')) { return $parent }
  }

  # 3) MyInvocation for older PS or special hosts
  if ($MyInvocation.MyCommand.Path) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $parent = Split-Path $scriptDir -Parent
    if (Test-Path (Join-Path $scriptDir 'Services')) { return $scriptDir }
    if (Test-Path (Join-Path $parent 'Services')) { return $parent }
  }

  # 4) Current working directory (when pasting into console)
  $cwd = (Get-Location).Path
  if (Test-Path (Join-Path $cwd 'Services')) { return $cwd }
  $parentCwd = Split-Path $cwd -Parent
  if ($parentCwd -and (Test-Path (Join-Path $parentCwd 'Services'))) { return $parentCwd }

  throw "Unable to resolve RootPath. Pass -RootPath explicitly (e.g. -RootPath 'D:\\Projekty\\store-app')."
}

function Invoke-ServiceReset {
  param(
    [Parameter(Mandatory=$true)][string]$ScriptPath,
    [string]$Name,
    [string]$MigrationName,
    [switch]$NoDrop,
    [switch]$KeepMigrations
  )
  if (-not (Test-Path $ScriptPath)) { throw "Script not found: $ScriptPath" }

  $common = @{}
  if ($MigrationName) { $common.MigrationName = $MigrationName }
  if ($NoDrop) { $common.NoDrop = $true }
  if ($KeepMigrations) { $common.KeepMigrations = $true }

  Write-Host "[$Name] Starting reset..." -ForegroundColor Cyan
  & $ScriptPath @common
  Write-Host "[$Name] Reset completed." -ForegroundColor Green
}

try {
  $base = Resolve-BasePath -Root $RootPath
  Write-Host "Base path: $base" -ForegroundColor DarkCyan

  $productScript  = Join-Path $base 'Services/ProductService/Scripts/Reset-Db-And-Migrations.ps1'
  $identityScript = Join-Path $base 'Services/IdentityService/Scripts/Reset-Db-And-Migrations.ps1'
  $orderScript    = Join-Path $base 'Services/OrderService/Scripts/Reset-Db-And-Migrations.ps1'
  $cartScript     = Join-Path $base 'Services/CartService/Scripts/Reset-Db-And-Migrations.ps1'
  $auditScript    = Join-Path $base 'Services/AuditLogService/Scripts/Reset-Db-And-Migrations.ps1'

  Invoke-ServiceReset -ScriptPath $productScript  -Name 'ProductService'  -MigrationName $ProductMigration  -NoDrop:$NoDrop  -KeepMigrations:$KeepMigrations
  Invoke-ServiceReset -ScriptPath $identityScript -Name 'IdentityService' -MigrationName $IdentityMigration -NoDrop:$NoDrop  -KeepMigrations:$KeepMigrations
  Invoke-ServiceReset -ScriptPath $orderScript    -Name 'OrderService'    -MigrationName $OrderMigration    -NoDrop:$NoDrop  -KeepMigrations:$KeepMigrations
  Invoke-ServiceReset -ScriptPath $cartScript     -Name 'CartService'     -MigrationName $CartMigration     -NoDrop:$NoDrop  -KeepMigrations:$KeepMigrations
  Invoke-ServiceReset -ScriptPath $auditScript    -Name 'AuditLogService' -MigrationName $AuditMigration    -NoDrop:$NoDrop  -KeepMigrations:$KeepMigrations

  Write-Host "All services reset completed." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
