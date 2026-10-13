<#
.SYNOPSIS
  Screenshots the window of the HTML prototype (design/prototype/index.html) in a given state, to compare it with the app.

.EXAMPLE
  .\prototype-shot.ps1 -Name hist-week-co2 -Js "V='hist';H={m:'co2',r:'week',hf:'all'}"
  .\prototype-shot.ps1 -Name live-elev -Js "K='elev';V='live'"

  -Js runs before the prototype draws its window: K is the zone (good, elev, high), V the view (live, hist, set), H the History choices
  (m metric, r range day/week/month/year, hf hours all/night). The picture goes to artifacts/proto/<Name>.png (git-ignored), 1200 x 728 at 100 %.
  Needs Microsoft Edge.
#>
param(
    [Parameter(Mandatory)][string]$Name,
    [string]$Js = '',
    [int]$Width = 1200,
    [int]$Height = 728
)

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'design\prototype\index.html'
$folder = Join-Path $root 'artifacts\proto'
New-Item -ItemType Directory -Force $folder | Out-Null

$html = [System.IO.File]::ReadAllText($source)
$css = '<style>.pg>*:not(#fit){display:none!important}body{padding:0!important;background:#fff}.pg{max-width:none!important;margin:0!important}#fit{position:fixed!important;left:0!important;top:0!important;width:1200px!important;height:728px!important}#win{transform:none!important}</style>'
$script = '<script>' + $Js + ';render();document.getElementById("win").style.transform="none";</script>'
$page = Join-Path $folder "$Name.html"
[System.IO.File]::WriteAllText($page, $html.Replace('</body>', $css + $script + '</body>'))

$png = Join-Path $folder "$Name.png"
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'Microsoft Edge was not found.' }

& $edge --headless=new --disable-gpu --hide-scrollbars "--window-size=$Width,$Height" "--screenshot=$png" ('file:///' + $page.Replace('\', '/')) 2>$null | Out-Null
Start-Sleep -Milliseconds 500
Write-Output $png
