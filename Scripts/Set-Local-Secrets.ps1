<#
.SYNOPSIS
  Stores the secrets every service needs at startup in dotnet user-secrets.

.DESCRIPTION
  Services refuse to start without:
    - JwtSettings:SecretKey  (gateway + all services, min. 32 characters)
    - InternalApi:ApiKey     (product, cart, order, payment; min. 32 characters)
    - PaymentWebhooks:SigningSecret (order, payment; min. 32 characters)
  and none of them is kept in appsettings*.json. When running the projects outside Docker
  (Visual Studio, dotnet run) this script writes the same values into each project's
  `dotnet user-secrets` store, which lives in the user profile and never reaches the repository.

  For docker compose set JWT_SECRET_KEY, INTERNAL_API_KEY and PAYMENT_WEBHOOK_SECRET in .env
  instead (see .env.example).

.PARAMETER JwtSecretKey
  JWT signing key. When omitted a random 48-byte key is generated and printed.

.PARAMETER InternalApiKey
  Service-to-service key. When omitted a random 48-byte key is generated and printed.

.PARAMETER PaymentWebhookSecret
  Key the payment service signs its webhooks with. When omitted a random 48-byte key is generated
  and printed.

.PARAMETER TrueAdminPassword
  Optional. Password for the seeded true-admin account (IdentityService). It is never generated
  or logged by the service; without it a fresh database gets no true admin.

.EXAMPLE
  ./Scripts/Set-Local-Secrets.ps1
  ./Scripts/Set-Local-Secrets.ps1 -JwtSecretKey "<same value as JWT_SECRET_KEY in .env>" -InternalApiKey "<same as INTERNAL_API_KEY>"
#>
param(
  [string]$JwtSecretKey,
  [string]$InternalApiKey,
  [string]$PaymentWebhookSecret,
  [string]$TrueAdminPassword,
  [string]$RootPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

function New-RandomKey {
  # Works on both Windows PowerShell 5.1 and PowerShell 7+
  $bytes = New-Object byte[] 48
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  $rng.GetBytes($bytes)
  $rng.Dispose()
  return [Convert]::ToBase64String($bytes)
}

function Assert-KeyLength([string]$Name, [string]$Value) {
  if ($Value.Length -lt 32) {
    throw "$Name must be at least 32 characters long (got $($Value.Length))."
  }
}

$generated = @()
if (-not $JwtSecretKey) { $JwtSecretKey = New-RandomKey; $generated += "JWT_SECRET_KEY=$JwtSecretKey" }
if (-not $InternalApiKey) { $InternalApiKey = New-RandomKey; $generated += "INTERNAL_API_KEY=$InternalApiKey" }
if (-not $PaymentWebhookSecret) { $PaymentWebhookSecret = New-RandomKey; $generated += "PAYMENT_WEBHOOK_SECRET=$PaymentWebhookSecret" }

Assert-KeyLength "JwtSecretKey" $JwtSecretKey
Assert-KeyLength "InternalApiKey" $InternalApiKey
Assert-KeyLength "PaymentWebhookSecret" $PaymentWebhookSecret
if ($TrueAdminPassword -and $TrueAdminPassword.Length -lt 8) { throw "TrueAdminPassword must be at least 8 characters long." }

if ($generated.Count -gt 0) {
  Write-Host "Generated keys (copy them to .env if you also use docker compose):"
  $generated | ForEach-Object { Write-Host "  $_" }
}

# project path => secrets that project needs
$projects = [ordered]@{
  "Gateway/APIGateway/Store.GatewayService.csproj"          = @{ "JwtSettings:SecretKey" = $JwtSecretKey }
  "Services/IdentityService/Store.IdentityService.csproj"   = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey }
  "Services/ProductService/Store.ProductService.csproj"     = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey }
  "Services/CartService/Store.CartService.csproj"           = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey }
  "Services/OrderService/Store.OrderService.csproj"         = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey; "PaymentWebhooks:SigningSecret" = $PaymentWebhookSecret }
  "Services/AuditLogService/Store.AuditLogService.csproj"   = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey }
  "Services/ContentService/Store.ContentService.csproj"     = @{ "JwtSettings:SecretKey" = $JwtSecretKey }
  "Services/PaymentService/Store.PaymentService.csproj"     = @{ "JwtSettings:SecretKey" = $JwtSecretKey; "InternalApi:ApiKey" = $InternalApiKey; "PaymentWebhooks:SigningSecret" = $PaymentWebhookSecret }
}

if ($TrueAdminPassword) {
  $projects["Services/IdentityService/Store.IdentityService.csproj"]["TrueAdmin:Password"] = $TrueAdminPassword
}

foreach ($project in $projects.Keys) {
  $path = Join-Path $RootPath $project
  if (-not (Test-Path $path)) {
    throw "Project not found: $path"
  }
  foreach ($secret in $projects[$project].GetEnumerator()) {
    dotnet user-secrets set $secret.Key $secret.Value --project $path | Out-Null
  }
  Write-Host "Secrets set for $project ($($projects[$project].Keys -join ', '))" -ForegroundColor Green
}
