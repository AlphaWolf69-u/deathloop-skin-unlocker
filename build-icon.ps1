# Original vector-style artwork: an open lock inside an orange time loop.
Add-Type -AssemblyName System.Drawing
$images = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.ScaleTransform(($size / 256.0), ($size / 256.0))
    $dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(28, 29, 32))
    $orange = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 102, 45))
    $cream = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 239, 209))
    $loop = New-Object System.Drawing.Pen($orange, 19)
    $shackle = New-Object System.Drawing.Pen($cream, 17)
    $shackle.StartCap = $shackle.EndCap = 'Round'
    $g.FillEllipse($dark, 3, 3, 250, 250)
    $g.DrawArc($loop, 25, 25, 206, 206, 32, 293)
    $points = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new(203, 41), [System.Drawing.PointF]::new(231, 87), [System.Drawing.PointF]::new(181, 77))
    $g.FillPolygon($orange, $points)
    $g.DrawArc($shackle, 91, 65, 66, 70, 175, 205)
    $g.DrawLine($shackle, 91, 100, 91, 129)
    $g.FillRectangle($cream, 77, 120, 105, 79)
    $g.FillEllipse($dark, 119, 140, 22, 22)
    $g.FillRectangle($dark, 124, 152, 12, 24)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,@($size, $stream.ToArray())
    if ($size -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'icon-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $shackle.Dispose(); $loop.Dispose(); $cream.Dispose(); $orange.Dispose(); $dark.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$output = [System.IO.File]::Create((Join-Path $PSScriptRoot 'unlocker.ico'))
$writer = New-Object System.IO.BinaryWriter($output)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
    $offset = 6 + 16 * $images.Count
    foreach ($entry in $images) {
        $dimension = if ($entry[0] -eq 256) { 0 } else { $entry[0] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$entry[1].Length); $writer.Write([uint32]$offset)
        $offset += $entry[1].Length
    }
    foreach ($entry in $images) { $writer.Write([byte[]]$entry[1]) }
} finally { $writer.Dispose(); $output.Dispose() }
