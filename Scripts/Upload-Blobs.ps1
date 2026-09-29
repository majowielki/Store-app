<#
.SYNOPSIS
  Uploads the product pictures (Blobs/*.webp and their smaller copies in Blobs/w*/) to the
  product-images container of an Azure storage account.

.DESCRIPTION
  Locally docker compose does this on its own: the blobs-seed service in docker-compose.yml
  fills the Azurite emulator on every start. This script is the same step for a real storage
  account: the pictures account infra/bicep/main.bicep creates (its output picturesAccount),
  which .github/workflows/cd.yml uploads to after every deployment. It creates the container
  when it is missing and uploads every WebP file, replacing the ones already there, so it can
  be run again after pictures are regenerated.

  Needs the Azure CLI signed in ("az login") with a data role on the account, such as
  Storage Blob Data Contributor, or with -AuthMode key a role that may read the account's keys
  (Contributor on its resource group, as the deployment pipeline has). The pictures must be
  converted first: "dotnet run Scripts/optimize-images.cs", then the smaller copies (ADR 016):
  "dotnet run Scripts/make-image-sizes.cs".

.PARAMETER AccountName
  The storage account to upload to.

.PARAMETER Container
  The blob container; the catalogue expects "product-images".

.PARAMETER AuthMode
  "login" (the default) uses the signed-in identity's data role; "key" the account key, which the
  CLI looks up with the identity's management role.

.EXAMPLE
  ./Scripts/Upload-Blobs.ps1 -AccountName storepx1234
  ./Scripts/Upload-Blobs.ps1 -AccountName storepx1234 -AuthMode key
#>
param(
  [Parameter(Mandatory = $true)]
  [string]$AccountName,
  [string]$Container = 'product-images',
  [ValidateSet('login', 'key')]
  [string]$AuthMode = 'login',
  [string]$RootPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$source = Join-Path $RootPath 'Blobs'
$pictures = @(Get-ChildItem -Path $source -Filter '*.webp' -File -Recurse)
if ($pictures.Count -eq 0) {
  throw "No WebP pictures in $source - run 'dotnet run Scripts/optimize-images.cs' first."
}

az storage container create --name $Container --account-name $AccountName --auth-mode $AuthMode --only-show-errors --output none
if ($LASTEXITCODE -ne 0) { throw "Could not create or reach the container $Container on $AccountName." }

az storage blob upload-batch --destination $Container --account-name $AccountName --auth-mode $AuthMode `
  --source $source --pattern '*.webp' --content-type image/webp --content-cache-control 'public, max-age=86400' `
  --overwrite --no-progress --only-show-errors --output none
if ($LASTEXITCODE -ne 0) { throw "The upload to $AccountName/$Container failed." }

Write-Host "Uploaded $($pictures.Count) pictures to $AccountName/$Container."
