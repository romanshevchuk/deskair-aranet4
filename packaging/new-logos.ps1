# Draws the package logos (the app ring, design-tokens.md "Icons": ring radius 11, stroke 2.75, dot radius 4.5 in a 28 unit box)
# into packaging\Assets. The PNGs are committed; run this only to change them.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

$green = [System.Drawing.ColorTranslator]::FromHtml('#1E8A5C')

function New-Logo([string]$name, [int]$width, [int]$height) {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # The ring fills about 70 percent of the shorter side, centred.
    $side = [Math]::Min($width, $height) * 0.7
    $unit = $side / 22.0
    $cx = $width / 2.0
    $cy = $height / 2.0
    $radius = 11 * $unit
    $pen = New-Object System.Drawing.Pen $green, (2.75 * $unit)
    $g.DrawEllipse($pen, ($cx - $radius), ($cy - $radius), (2 * $radius), (2 * $radius))
    $dot = 4.5 * $unit
    $brush = New-Object System.Drawing.SolidBrush $green
    $g.FillEllipse($brush, ($cx - $dot), ($cy - $dot), (2 * $dot), (2 * $dot))

    $bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose(); $pen.Dispose(); $brush.Dispose()
}

New-Logo 'Square44x44Logo.png' 44 44
New-Logo 'Square150x150Logo.png' 150 150
New-Logo 'StoreLogo.png' 50 50
# For the Store listing (uploaded in Partner Center, not part of the package).
New-Logo 'StoreListing-300x300.png' 300 300
Write-Host "Logos written to $assets"
