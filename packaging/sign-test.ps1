# Phase 3 (docs\plans\store-packaging.md): signs the package with a TEST certificate so it can be installed on this PC. Not used for the Store
# (Microsoft signs the Store package). The certificate has the same subject as the manifest's Publisher, which Windows requires.
#   .\packaging\sign-test.ps1 -Package artifacts\msix\Deskair_0.1.0.0_x64.msix
# Writes artifacts\msix\DeskairTest.cer; trust it once with packaging\trust-test-cert.ps1 (needs an administrator window).
param(
    [Parameter(Mandatory)][string]$Package
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$publisher = ([xml](Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw)).Package.Identity.Publisher
$cer = Join-Path $repo 'artifacts\msix\DeskairTest.cer'

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $signtool) { throw 'signtool.exe not found. Install the Windows 11 SDK.' }

$certificate = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $publisher -and $_.FriendlyName -eq 'Deskair test' } | Select-Object -First 1
if (-not $certificate) {
    $certificate = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature -FriendlyName 'Deskair test' `
        -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddMonths(6) `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    Write-Host "Created test certificate $($certificate.Thumbprint)"
}

Export-Certificate -Cert $certificate -FilePath $cer | Out-Null
& $signtool.FullName sign /fd SHA256 /sha1 $certificate.Thumbprint $Package
if ($LASTEXITCODE -ne 0) { throw 'signtool failed' }
Write-Host "Signed $Package. Certificate: $cer"
