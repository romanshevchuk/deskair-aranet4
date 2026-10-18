# Builds the Store package: publishes the app (self-contained, win-x64), adds the manifest and logos, and packs them with MakeAppx.
# Output: artifacts\msix\Deskair_<version>_x64.msix (git-ignored). See packaging\README.md and docs\plans\store-packaging.md.
#   .\packaging\build-msix.ps1 -Version 0.1.0.0
param(
    [string]$Version = '0.1.0.0'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$out = Join-Path $repo 'artifacts\msix'
$publish = Join-Path $out 'publish'
$stage = Join-Path $out 'stage'

if ($Version -notmatch '^\d+\.\d+\.\d+\.0$') {
    throw "Version must have four numbers and end in .0 (the last number is reserved by the Store): $Version"
}

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) {
    throw 'makeappx.exe not found. Install the Windows 11 SDK (Visual Studio Installer, Individual components).'
}

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force $publish, $stage | Out-Null

Write-Host 'Publishing (self-contained, win-x64)...'
dotnet publish (Join-Path $repo 'src\Deskair.App\Deskair.App.csproj') -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Copy-Item "$publish\*" $stage -Recurse
Copy-Item (Join-Path $PSScriptRoot 'Assets') (Join-Path $stage 'Assets') -Recurse

$manifest = Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw
$manifest = $manifest -replace '(<Identity [^>]*?Version=")[^"]*(")', "`${1}$Version`${2}"
[System.IO.File]::WriteAllText((Join-Path $stage 'AppxManifest.xml'), $manifest, (New-Object System.Text.UTF8Encoding $false))

$package = Join-Path $out "Deskair_${Version}_x64.msix"
Write-Host "Packing with $($makeappx.FullName)..."
& $makeappx.FullName pack /d $stage /p $package /o
if ($LASTEXITCODE -ne 0) { throw 'makeappx failed' }

$size = [Math]::Round((Get-Item $package).Length / 1MB, 1)
Write-Host "Done: $package ($size MB)"
