<#
.SYNOPSIS
  Uploads the product pictures (Blobs/*.webp) to the product-images container of an Azure
  storage account.

.DESCRIPTION
  Locally docker compose does this on its own: the blobs-seed service in
  docker-compose.dev.yml fills the Azurite emulator on every start. This script is the same
  step for a real storage account. It creates the container when it is missing and uploads
  every WebP file, replacing the ones already there, so it can be run again after pictures
  are regenerated.

  Needs the Azure CLI signed in ("az login") with a data role on the account, such as
  Storage Blob Data Contributor. The pictures must be converted first:
  "dotnet run Scripts/optimize-images.cs".

.PARAMETER AccountName
  The storage account to upload to.

.PARAMETER Container
  The blob container; the catalogue expects "product-images".

.EXAMPLE
  ./Scripts/Upload-Blobs.ps1 -AccountName storeprodst1234
#>
param(
  [Parameter(Mandatory = $true)]
  [string]$AccountName,
  [string]$Container = 'product-images',
  [string]$RootPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$source = Join-Path $RootPath 'Blobs'
$pictures = @(Get-ChildItem -Path $source -Filter '*.webp' -File)
if ($pictures.Count -eq 0) {
  throw "No WebP pictures in $source - run 'dotnet run Scripts/optimize-images.cs' first."
}

az storage container create --name $Container --account-name $AccountName --auth-mode login --only-show-errors --output none
if ($LASTEXITCODE -ne 0) { throw "Could not create or reach the container $Container on $AccountName." }

az storage blob upload-batch --destination $Container --account-name $AccountName --auth-mode login `
  --source $source --pattern '*.webp' --content-type image/webp --content-cache-control 'public, max-age=86400' `
  --overwrite --no-progress --only-show-errors --output none
if ($LASTEXITCODE -ne 0) { throw "The upload to $AccountName/$Container failed." }

Write-Host "Uploaded $($pictures.Count) pictures to $AccountName/$Container."
