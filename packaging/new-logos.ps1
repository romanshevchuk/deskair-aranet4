# Draws the package logos into packaging\Assets: the app ring (a white ring and dot, design-tokens.md "Icons") on a green gradient.
# The squares are filled to the edge on purpose: a logo with see-through parts gets Windows' blue accent square behind it in the taskbar.
# The PNGs are committed; run this only to change them.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function New-Logo([string]$name, [int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'

    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.ColorTranslator]::FromHtml('#34C38A')), ([System.Drawing.ColorTranslator]::FromHtml('#157A55')), 60
    $g.FillRectangle($gradient, $rect)

    $centre = $size / 2.0
    $radius = $size * 0.27
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ($size * 0.065)
    $g.DrawEllipse($pen, ($centre - $radius), ($centre - $radius), (2 * $radius), (2 * $radius))
    $dot = $size * 0.105
    $g.FillEllipse([System.Drawing.Brushes]::White, ($centre - $dot), ($centre - $dot), (2 * $dot), (2 * $dot))

    $bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose(); $gradient.Dispose(); $pen.Dispose()
}

New-Logo 'Square44x44Logo.png' 44
New-Logo 'Square150x150Logo.png' 150
New-Logo 'StoreLogo.png' 50
# For the Store listing (uploaded in Partner Center, not part of the package; the Store rounds the corners itself).
New-Logo 'StoreListing-300x300.png' 300
Write-Host "Logos written to $assets"
