<#
.SYNOPSIS
  Screenshots the window of the HTML prototype (design/prototype/index.html), or of the polish concept (design/polish/deskair-polish-concept.html)
  with -Concept, in a given state, to compare it with the app.

.EXAMPLE
  .\prototype-shot.ps1 -Name hist-week-co2 -Js "V='hist';H={m:'co2',r:'week',hf:'all'}"
  .\prototype-shot.ps1 -Name live-elev -Js "K='elev';V='live'"
  .\prototype-shot.ps1 -Concept -View live -Name concept-live-high -Js "document.querySelector('.st [data-s=h]').click()"
  .\prototype-shot.ps1 -Concept -View history -Name concept-hist-month-dark -Js "document.querySelector('#hrange [data-r=month]').click();document.querySelector('.th [data-t=dark]').click()"

  Prototype: -Js runs before the prototype draws its window: K is the zone (good, elev, high), V the view (live, hist, set), H the History choices
  (m metric, r range day/week/month/year, hf hours all/night).
  Concept: -View picks the window (live or history) and -Js runs after the page has drawn, clicking its own controls:
  '.st [data-s=g|e|h]' (zone), '.th [data-t=light|dark]' (theme), '#hrange [data-r=day|week|month|year]' (History range),
  '.dm [data-d=oct|year]' (History data), '#hprev' (previous period). The note pins are hidden.
  The picture goes to artifacts/proto/<Name>.png (git-ignored), 1200 x 728 at 100 %; -Scale 2 gives 2400 x 1456 like the app renders.
  Needs Microsoft Edge.
#>
param(
    [Parameter(Mandatory)][string]$Name,
    [string]$Js = '',
    [int]$Width = 1200,
    [int]$Height = 728,
    [double]$Scale = 1,
    [switch]$Concept,
    [ValidateSet('live', 'history')][string]$View = 'live'
)

$root = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $root 'artifacts\proto'
New-Item -ItemType Directory -Force $folder | Out-Null

if ($Concept) {
    # The concept file is a page body without <html> or <body>, published as an artifact; wrap it so Edge renders it in standards mode.
    $source = Join-Path $root 'design\polish\deskair-polish-concept.html'
    $fit = if ($View -eq 'history') { 'hfit' } else { 'fit' }
    $win = if ($View -eq 'history') { 'hwin' } else { 'win' }
    $css = "<style>.pg>*:not(#$fit){display:none!important}body{padding:0!important;margin:0!important;background:#fff}.pg{max-width:none!important;margin:0!important;padding:0!important}#$fit{position:fixed!important;left:0!important;top:0!important;width:1200px!important;height:728px!important;border-radius:0!important}#$win{transform:none!important;border-radius:0!important}.pin,.fpin{display:none!important}</style>"
    $script = '<script>' + $Js + '</script>'
    $body = [System.IO.File]::ReadAllText($source)
    $content = '<!doctype html><html><head><meta charset="utf-8"></head><body>' + $body + $css + $script + '</body></html>'
}
else {
    $source = Join-Path $root 'design\prototype\index.html'
    $css = '<style>.pg>*:not(#fit){display:none!important}body{padding:0!important;background:#fff}.pg{max-width:none!important;margin:0!important}#fit{position:fixed!important;left:0!important;top:0!important;width:1200px!important;height:728px!important}#win{transform:none!important}</style>'
    $script = '<script>' + $Js + ';render();document.getElementById("win").style.transform="none";</script>'
    $content = [System.IO.File]::ReadAllText($source).Replace('</body>', $css + $script + '</body>')
}

$page = Join-Path $folder "$Name.html"
[System.IO.File]::WriteAllText($page, $content)

$png = Join-Path $folder "$Name.png"
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'Microsoft Edge was not found.' }

& $edge --headless=new --disable-gpu --hide-scrollbars "--window-size=$Width,$Height" "--force-device-scale-factor=$Scale" "--screenshot=$png" ('file:///' + $page.Replace('\', '/')) 2>$null | Out-Null
Start-Sleep -Milliseconds 500
Write-Output $png
