<#
.SYNOPSIS
  Stores the JWT signing key in the user-secrets of the gateway and every service (SEC-02).

.DESCRIPTION
  Services refuse to start without JwtSettings:SecretKey (min. 32 characters) and the key is
  no longer kept in appsettings*.json. When running the projects outside Docker (Visual Studio,
  dotnet run) this script writes the same key into each project's `dotnet user-secrets` store,
  which lives in the user profile and never reaches the repository.

  For docker compose set JWT_SECRET_KEY in .env instead (see .env.example).

.PARAMETER SecretKey
  Key to store. When omitted a random 48-byte key is generated and printed so you can reuse
  it in .env.

.EXAMPLE
  ./Scripts/Set-Local-Jwt-Secret.ps1
  ./Scripts/Set-Local-Jwt-Secret.ps1 -SecretKey "the-same-key-you-put-in-.env-at-least-32-chars"
#>
param(
  [string]$SecretKey,
  [string]$RootPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

if (-not $SecretKey) {
  # Works on both Windows PowerShell 5.1 and PowerShell 7+
  $bytes = New-Object byte[] 48
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  $rng.GetBytes($bytes)
  $rng.Dispose()
  $SecretKey = [Convert]::ToBase64String($bytes)
  Write-Host "Generated JWT key (copy it to .env as JWT_SECRET_KEY if you also use docker compose):"
  Write-Host "  $SecretKey"
}

if ($SecretKey.Length -lt 32) {
  throw "SecretKey must be at least 32 characters long (got $($SecretKey.Length))."
}

$projects = @(
  "Gateway/APIGateway/Store.GatewayService.csproj",
  "Services/IdentityService/Store.IdentityService.csproj",
  "Services/ProductService/Store.ProductService.csproj",
  "Services/CartService/Store.CartService.csproj",
  "Services/OrderService/Store.OrderService.csproj",
  "Services/AuditLogService/Store.AuditLogService.csproj"
)

foreach ($project in $projects) {
  $path = Join-Path $RootPath $project
  if (-not (Test-Path $path)) {
    throw "Project not found: $path"
  }
  dotnet user-secrets set "JwtSettings:SecretKey" $SecretKey --project $path | Out-Null
  Write-Host "JwtSettings:SecretKey set for $project" -ForegroundColor Green
}
