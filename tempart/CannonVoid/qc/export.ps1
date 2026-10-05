$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$palette = @('#171C30','#252C40','#394052','#535C6D','#737D8E','#A0A9B6','#352323','#523124','#75432B','#965A32','#B97943','#D59B5D','#674D2E','#896333','#B38640','#D4AA54','#F1CE77','#F6DCA0','#373439','#514C4D','#686264','#817A75','#9F9789','#B9B09C','#D2C9AE','#751F2B','#AB2933','#D6433B','#F16A4C','#FF6E60','#FFD0BD','#FFF4D6') | ForEach-Object { [System.Drawing.ColorTranslator]::FromHtml($_) }
$report = [System.Collections.Generic.List[string]]::new()
foreach ($variant in @('a','b')) {
    $src = [System.Drawing.Bitmap]::FromFile((Join-Path $root "raw/identity-$variant.png"))
    $minX=$src.Width; $minY=$src.Height; $maxX=-1; $maxY=-1
    for ($y=0; $y -lt $src.Height; $y++) { for ($x=0; $x -lt $src.Width; $x++) {
        if ($src.GetPixel($x,$y).A -ge 200) { $minX=[Math]::Min($minX,$x); $minY=[Math]::Min($minY,$y); $maxX=[Math]::Max($maxX,$x); $maxY=[Math]::Max($maxY,$y) }
    }}
    if ($maxX -lt 0) { throw 'No visible sprite' }
    $bw=$maxX-$minX+1; $bh=$maxY-$minY+1
    $scale=[Math]::Min(70.0/$bh,90.0/$bw)
    $w=[int][Math]::Round($bw*$scale); $h=[int][Math]::Round($bh*$scale)
    $ox=[int][Math]::Floor((144-$w)/2); $oy=97-$h
    $dst=[System.Drawing.Bitmap]::new(144,128)
    $colorCache=@{}
    for ($y=0; $y -lt $h; $y++) { for ($x=0; $x -lt $w; $x++) {
        $sx=$minX+[Math]::Min($bw-1,[int][Math]::Floor(($x+0.5)*$bw/$w))
        $sy=$minY+[Math]::Min($bh-1,[int][Math]::Floor(($y+0.5)*$bh/$h))
        $c=$src.GetPixel($sx,$sy)
        if ($c.A -lt 200) { continue }
        $key=$c.ToArgb()
        if (!$colorCache.ContainsKey($key)) {
            $best=$palette[0]; $distance=[double]::PositiveInfinity
            foreach ($p in $palette) {
                $d=($c.R-$p.R)*($c.R-$p.R)+($c.G-$p.G)*($c.G-$p.G)+($c.B-$p.B)*($c.B-$p.B)
                if ($d -lt $distance) { $best=$p; $distance=$d }
            }
            $colorCache[$key]=$best
        }
        $dst.SetPixel($ox+$x,$oy+$y,$colorCache[$key])
    }}
    $bottom=-1; $count=0; $colors=@{}
    for ($y=0; $y -lt 128; $y++) { for ($x=0; $x -lt 144; $x++) { $c=$dst.GetPixel($x,$y); if ($c.A -gt 0) { $bottom=$y; $count++; $colors[$c.ToArgb()]=$true; if ($c.A -ne 255) { throw 'Partial alpha' }; if ($c.R+$c.G+$c.B -eq 0) { throw 'Pure black pixel' } } }}
    if ($bottom -ne 96) { throw "Wrong base bottom: $bottom" }
    $dst.Save((Join-Path $root "identity/identity-variant-$variant.png"),[System.Drawing.Imaging.ImageFormat]::Png)
    $report.Add("Variant $variant : canvas 144x128; fitted bounds ${w}x${h}; bottom y=$bottom; opaque pixels=$count; palette colors=$($colors.Count); alpha only 0/255; no pure black.")
    $src.Dispose(); $dst.Dispose()
}
Copy-Item -LiteralPath (Join-Path $root 'identity/identity-variant-b.png') -Destination (Join-Path $root 'identity/identity-1.png')
Copy-Item -LiteralPath (Join-Path $root 'identity/identity-1.png') -Destination (Join-Path $root 'identity/sheet-transparent.png')
$report | Set-Content -Encoding utf8 (Join-Path $root 'qc/validation.md')
$report

$effectCache=@{}
function Export-Concept([string]$rawName,[int]$index,[int]$columns,[string]$outputName,[int]$logicalWidth,[int]$logicalHeight) {
    $raw=[System.Drawing.Bitmap]::FromFile((Join-Path $root "raw/$rawName"))
    $x0=[int][Math]::Floor($index*$raw.Width/$columns)
    $x1=[int][Math]::Floor(($index+1)*$raw.Width/$columns)
    $cell=$raw.Clone([System.Drawing.Rectangle]::new($x0,0,$x1-$x0,$raw.Height),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $raw.Dispose()
    $left=$cell.Width; $top=$cell.Height; $right=-1; $bottom=-1
    for($y=0;$y -lt $cell.Height;$y++){for($x=0;$x -lt $cell.Width;$x++){
        if($cell.GetPixel($x,$y).A -ge 200){$left=[Math]::Min($left,$x);$top=[Math]::Min($top,$y);$right=[Math]::Max($right,$x);$bottom=[Math]::Max($bottom,$y)}
    }}
    if($right -lt 0){throw 'Empty concept source'}
    $bw=$right-$left+1;$bh=$bottom-$top+1
    $dst=[System.Drawing.Bitmap]::new(128,128)
    $ox=64-[int][Math]::Floor($logicalWidth/2);$oy=64-[int][Math]::Floor($logicalHeight/2)
    for($y=0;$y -lt $logicalHeight;$y++){for($x=0;$x -lt $logicalWidth;$x++){
        $sx=$left+[Math]::Min($bw-1,[int][Math]::Floor(($x+0.5)*$bw/$logicalWidth))
        $sy=$top+[Math]::Min($bh-1,[int][Math]::Floor(($y+0.5)*$bh/$logicalHeight))
        $c=$cell.GetPixel($sx,$sy);if($c.A -lt 200){continue};$key=$c.ToArgb()
        if(!$effectCache.ContainsKey($key)){
            $best=$palette[0];$distance=[double]::PositiveInfinity
            foreach($p in $palette){$d=($c.R-$p.R)*($c.R-$p.R)+($c.G-$p.G)*($c.G-$p.G)+($c.B-$p.B)*($c.B-$p.B);if($d -lt $distance){$best=$p;$distance=$d}}
            $effectCache[$key]=$best
        }
        $dst.SetPixel($ox+$x,$oy+$y,$effectCache[$key])
    }}
    $dst.Save((Join-Path $root "concept/$outputName.png"),[System.Drawing.Imaging.ImageFormat]::Png)
    for($y=0;$y -lt 128;$y++){for($x=0;$x -lt 128;$x++){
        $c=$dst.GetPixel($x,$y);if($c.A -ne 0 -and $c.A -ne 255){throw 'Partial alpha'};if($c.A -gt 0 -and $c.R+$c.G+$c.B -eq 0){throw 'Pure black'}
    }}
    Add-Content -Encoding utf8 (Join-Path $root 'qc/validation.md') -Value "$outputName : canvas128x128; normalized extent ${logicalWidth}x${logicalHeight}; center64,64; binary alpha; no pure black."
    $cell.Dispose();$dst.Dispose()
}
Export-Concept 'vortex-concept.png' 0 2 'vortex-base' 58 32
Export-Concept 'vortex-concept.png' 1 2 'vortex-max' 58 32
Export-Concept 'explosion-concept.png' 0 1 'explosion-max' 64 48

$sheet=[System.Drawing.Bitmap]::new(256,128)
$g=[System.Drawing.Graphics]::FromImage($sheet)
foreach($i in 0,1){$name=if($i -eq 0){'vortex-base'}else{'vortex-max'};$im=[System.Drawing.Bitmap]::FromFile((Join-Path $root "concept/$name.png"));$g.DrawImageUnscaled($im,$i*128,0);$im.Dispose()}
$g.Dispose();$sheet.Save((Join-Path $root 'concept/vortex-sheet-transparent.png'),[System.Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose()

$paths=@('Assets/Art/Generated/CannonRapid/identity/identity-1.png','Assets/Art/Generated/CannonBlast/identity/identity-1.png','Assets/Art/Generated/CannonFrost/identity/identity-1.png','Assets/Art/Generated/CannonBallista/identity/identity-1.png',(Join-Path $root 'identity/identity-variant-a.png'),(Join-Path $root 'identity/identity-variant-b.png'))
$labels=@('LIEN THANH','NO','BANG','NO XUYEN','VOID A','VOID B')
$board=[System.Drawing.Bitmap]::new(1296,852)
$g=[System.Drawing.Graphics]::FromImage($board);$g.Clear([System.Drawing.Color]::FromArgb(224,224,224))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font=[System.Drawing.Font]::new('Arial',16)
for($i=0;$i -lt 6;$i++){
    $im=[System.Drawing.Bitmap]::FromFile((Resolve-Path $paths[$i]));$cx=($i%3)*432;$cy=[int][Math]::Floor($i/3)*426
    $g.DrawString($labels[$i],$font,[System.Drawing.Brushes]::Black,$cx+20,$cy+12)
    $g.DrawImage($im,[System.Drawing.Rectangle]::new($cx+[int]((144-$im.Width)*1.5),$cy+40,$im.Width*3,$im.Height*3),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel)
    $im.Dispose()
}
$board.Save((Join-Path $root 'qc/compare.png'),[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$board.Dispose()

$conceptBoard=[System.Drawing.Bitmap]::new(1296,852)
$g=[System.Drawing.Graphics]::FromImage($conceptBoard);$g.Clear([System.Drawing.Color]::FromArgb(224,224,224))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$conceptPaths=@('identity/identity-variant-a.png','identity/identity-variant-b.png','concept/vortex-base.png','concept/vortex-max.png','concept/explosion-max.png')
$conceptLabels=@('VOID A','VOID B','VORTEX - 0 STACKS','VORTEX - 5 STACKS','FINAL EXPLOSION - 5 STACKS')
for($i=0;$i -lt 5;$i++){
    $slot=if($i -lt 2){$i}else{$i+1};$cx=($slot%3)*432;$cy=[int][Math]::Floor($slot/3)*426
    $im=[System.Drawing.Bitmap]::FromFile((Join-Path $root $conceptPaths[$i]));$g.DrawString($conceptLabels[$i],$font,[System.Drawing.Brushes]::Black,$cx+20,$cy+12)
    $g.DrawImage($im,[System.Drawing.Rectangle]::new($cx+[int]((144-$im.Width)*1.5),$cy+40,$im.Width*3,$im.Height*3),0,0,$im.Width,$im.Height,[System.Drawing.GraphicsUnit]::Pixel);$im.Dispose()
}
$g.DrawString("PULL 1.5s -> COLLAPSE`n`n+15% DAMAGE / STACK`nMAX 5 STACKS`n`n100 -> 175 DAMAGE`nSAME RADIUS",$font,[System.Drawing.Brushes]::Black,890,95)
$conceptBoard.Save((Join-Path $root 'qc/concept-board.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$g.Dispose();$conceptBoard.Dispose()
Add-Content -Encoding utf8 (Join-Path $root 'qc/validation.md') -Value @('Four cannon references at exactly3x in compare.png.','Vortex charge states use the same normalized footprint58x32.','Concept stills only; production clips exported by export-production.ps1.','No cooldown assets or gameplay implementation.','Owner selected identity B.')
