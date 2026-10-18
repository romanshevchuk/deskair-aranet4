# Run ONCE in an administrator PowerShell window: makes Windows trust the Deskair TEST certificate, so the test package installs.
# Remove it afterwards with:  .\packaging\trust-test-cert.ps1 -Remove
param(
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$cer = Join-Path $repo 'artifacts\msix\DeskairTest.cer'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Run this in an administrator PowerShell window.' }

if ($Remove) {
    Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object { $_.FriendlyName -eq 'Deskair test' -or $_.Subject -like 'CN=C6F2FCB9*' } | Remove-Item
    Write-Host 'Test certificate removed from Trusted People.'
    return
}

if (-not (Test-Path $cer)) { throw "Not found: $cer. Run sign-test.ps1 first." }
Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
Write-Host 'Test certificate trusted (Trusted People, this PC only).'
