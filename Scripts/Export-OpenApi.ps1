<#
.SYNOPSIS
    Writes the OpenAPI document of every service to docs/api/openapi/*.json.

.DESCRIPTION
    Runs the Swashbuckle CLI (a local dotnet tool, see .config/dotnet-tools.json) against the
    built service assemblies. The CLI starts each host with a no-op server (the middleware
    pipeline has to exist for the document), so the hosts run with Database:Schema=None, a
    broker that is never reached and placeholder values for the options validated at start.
    The documents are committed: the UI generates its API types from them and CI fails when a
    service changed its contract without re-exporting (Scripts/Export-OpenApi.ps1 -Check).

.PARAMETER Configuration
    Build configuration whose output is read (Debug by default, Release in CI).

.PARAMETER NoBuild
    Skip "dotnet build" of the solution (CI builds it earlier).

.PARAMETER Check
    Export to a temporary folder and fail when the result differs from the committed documents.
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [switch] $NoBuild,
    [switch] $Check
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$output = Join-Path $root 'docs/api/openapi'

# Document name -> service project (the assembly name is Store.<project>)
$services = [ordered]@{
    identity = 'IdentityService'
    catalog  = 'ProductService'
    cart     = 'CartService'
    orders   = 'OrderService'
    audit    = 'AuditLogService'
    content  = 'ContentService'
    payments = 'PaymentService'
    reviews  = 'ReviewService'
}

if (-not $NoBuild) {
    dotnet build (Join-Path $root 'Store.Microservices.slnx') --nologo -v q -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
}

dotnet tool restore --tool-manifest (Join-Path $root '.config/dotnet-tools.json') | Out-Null

# Values checked when the host is built; nothing is connected to, so placeholders will do
$placeholders = @{
    ASPNETCORE_ENVIRONMENT                 = 'Development'
    Database__Schema                       = 'None'
    ConnectionStrings__DefaultConnection   = 'Host=localhost;Database=openapi;Username=openapi;Password=openapi'
    JwtSettings__SecretKey                 = 'openapi-export-placeholder-key-that-is-long-enough-32'
    InternalApi__ApiKey                    = 'openapi-export-placeholder-internal-key'
    RabbitMQ__Host                         = 'localhost'
    RabbitMQ__Username                     = 'openapi'
    RabbitMQ__Password                     = 'openapi'
    Services__ProductService               = 'http://localhost:5003'
    Services__CartService                  = 'http://localhost:5005'
    Services__PaymentService               = 'http://localhost:5008'
    PaymentWebhooks__SigningSecret         = 'openapi-export-placeholder-webhook-secret-32'
    PaymentWebhooks__Url                   = 'http://localhost:5006/api/v1/webhooks/payments'
}
$previous = @{}
foreach ($name in $placeholders.Keys) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $placeholders[$name])
}

$target = if ($Check) { Join-Path ([IO.Path]::GetTempPath()) "store-openapi-$([Guid]::NewGuid().ToString('N'))" } else { $output }
New-Item -ItemType Directory -Force $target | Out-Null

try {
    foreach ($entry in $services.GetEnumerator()) {
        $assembly = Join-Path $root "Services/$($entry.Value)/bin/$Configuration/net10.0/Store.$($entry.Value).dll"
        $file = Join-Path $target "$($entry.Key).json"
        # The host reads appsettings*.json from its content root: the build output, not the repository root
        [Environment]::SetEnvironmentVariable('ASPNETCORE_CONTENTROOT', (Split-Path $assembly))
        dotnet swagger tofile --output $file $assembly v1
        if ($LASTEXITCODE -ne 0) { throw "Export of $($entry.Key) failed" }
        # Same bytes on every platform: LF line endings, newline at the end. A multi-line XML
        # comment keeps the line endings of its source file, so a description exported on
        # Windows would carry an escaped "\r\n" where a Linux runner writes "\n".
        $json = ([IO.File]::ReadAllText($file) -replace "`r`n", "`n" -replace '\\r\\n', '\n').TrimEnd() + "`n"
        [IO.File]::WriteAllText($file, $json, [Text.UTF8Encoding]::new($false))
        Write-Host "exported $($entry.Key) -> $file"
    }

    if ($Check) {
        $stale = @()
        foreach ($name in $services.Keys) {
            $committed = Join-Path $output "$name.json"
            # Git uses native line endings on checkout; compare content, not CRLF versus LF.
            $expected = [IO.File]::ReadAllText((Join-Path $target "$name.json"))
            if (-not (Test-Path $committed) -or (([IO.File]::ReadAllText($committed) -replace "`r`n", "`n") -cne $expected)) {
                $stale += $name
            }
        }
        if ($stale.Count -gt 0) {
            throw "OpenAPI documents are out of date: $($stale -join ', '). Run Scripts/Export-OpenApi.ps1 and commit docs/api/openapi."
        }
        Write-Host 'OpenAPI documents are up to date'
    }
}
finally {
    foreach ($name in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name])
    }
    [Environment]::SetEnvironmentVariable('ASPNETCORE_CONTENTROOT', $null)
    if ($Check) { Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue }
}
