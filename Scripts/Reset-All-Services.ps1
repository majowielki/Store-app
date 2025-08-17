param(
  [string]$ProductMigration = "AddSaleAndGroupsToProduct",
  [string]$IdentityMigration = "InitialCreate",
  [string]$OrderMigration = "InitialCreate",
  [string]$CartMigration = "InitialMigration",
  [string]$AuditMigration = "InitialCreate",
  [string]$RootPath = "D:\Projekty\store-app"
)

$ErrorActionPreference = 'Stop'

# Base workspace path (allows running the script from anywhere)
$base = $RootPath
if (-not (Test-Path $base)) {
  Write-Error "RootPath not found: $base"
  exit 1
}

# Build absolute paths to service scripts
$productScript = Join-Path $base "Services/ProductService/Scripts/Reset-Db-And-Migrations.ps1"
$identityScript = Join-Path $base "Services/IdentityService/Scripts/Reset-Db-And-Migrations.ps1"
$orderScript = Join-Path $base "Services/OrderService/Scripts/Reset-Db-And-Migrations.ps1"
$cartScript = Join-Path $base "Services/CartService/Scripts/Reset-Db-And-Migrations.ps1"
$auditScript = Join-Path $base "Services/AuditLogService/Scripts/Reset-Db-And-Migrations.ps1"

& $productScript -MigrationName $ProductMigration
& $identityScript -MigrationName $IdentityMigration
& $orderScript -MigrationName $OrderMigration
& $cartScript -MigrationName $CartMigration
& $auditScript -MigrationName $AuditMigration

Write-Host "All services reset completed." -ForegroundColor Green
