$ErrorActionPreference = 'Stop'

$outDir = Join-Path $PSScriptRoot '..\output\presentations'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$pptxPath = Join-Path $outDir 'SmartBins_Staff_Slide_Muestra.pptx'
$pngPath = Join-Path $outDir 'SmartBins_Staff_Slide_Muestra.png'
$aacute = [char]0x00E1
$iacute = [char]0x00ED
$oacute = [char]0x00F3
$bullet = [char]0x2022

function Rgb([int]$r, [int]$g, [int]$b) { return $r + ($g * 256) + ($b * 65536) }

$ppt = New-Object -ComObject PowerPoint.Application
$ppt.Visible = -1
$presentation = $ppt.Presentations.Add()
$presentation.PageSetup.SlideWidth = 960
$presentation.PageSetup.SlideHeight = 540
$slide = $presentation.Slides.Add(1, 12)

function Add-Box($name, $left, $top, $width, $height, $fill, $line, $radius = $false) {
    $type = if ($radius) { 5 } else { 1 }
    $s = $slide.Shapes.AddShape($type, $left, $top, $width, $height)
    $s.Name = $name
    $s.Fill.ForeColor.RGB = $fill
    $s.Line.ForeColor.RGB = $line
    $s.Line.Weight = 0.7
    return $s
}

function Add-Text($name, $text, $left, $top, $width, $height, $size, $color, $bold = $false, $align = 1, $font = 'Aptos') {
    $s = $slide.Shapes.AddTextbox(1, $left, $top, $width, $height)
    $s.Name = $name
    $s.TextFrame2.TextRange.Text = $text
    $s.TextFrame2.MarginLeft = 0
    $s.TextFrame2.MarginRight = 0
    $s.TextFrame2.MarginTop = 0
    $s.TextFrame2.MarginBottom = 0
    $s.TextFrame2.TextRange.Font.Name = $font
    $s.TextFrame2.TextRange.Font.Size = [single]$size
    $s.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = $color
    $s.TextFrame2.TextRange.Font.Bold = $(if ($bold) { -1 } else { 0 })
    $s.TextFrame2.TextRange.ParagraphFormat.Alignment = $align
    return $s
}

$navy = Rgb 0 91 127
$orange = Rgb 232 135 58
$ink = Rgb 21 51 62
$muted = Rgb 83 106 115
$paper = Rgb 246 244 239
$white = Rgb 255 255 255
$border = Rgb 216 224 226

$slide.Background.Fill.ForeColor.RGB = $paper
(Add-Box 'header-band' 0 0 960 70.5 $navy $navy) | Out-Null
(Add-Box 'header-accent' 0 70.5 960 5.25 $orange $orange) | Out-Null
(Add-Text 'title' "Smart Bins convierte un modelo en producci$($oacute)n guiada" 44 14 800 42 25.5 $white $true 1 'Aptos Display') | Out-Null
(Add-Text 'subtitle' "Un flujo digital, desde la preparaci$($oacute)n de ingenier$($iacute)a hasta la ejecuci$($oacute)n del operador" 45 90 810 25 14.5 (Rgb 53 80 92)) | Out-Null

$steps = @(
    @{ N='01'; Title='Preparar el modelo'; Body="Componentes, bins, im$($aacute)genes y tiempos de ciclo"; Color=(Rgb 216 90 106); Tag="INGENIER$($iacute)A" },
    @{ N='02'; Title='Definir el proceso'; Body='Crear o importar la secuencia de ensamble'; Color=$orange; Tag="INGENIER$($iacute)A" },
    @{ N='03'; Title="Balancear la l$($iacute)nea"; Body="Elegir operadores y distribuir la carga autom$($aacute)ticamente"; Color=(Rgb 130 168 60); Tag="PRODUCCI$($oacute)N" },
    @{ N='04'; Title="Guiar la ejecuci$($oacute)n"; Body='Instrucciones visuales, avance y resultados de la corrida'; Color=(Rgb 0 139 137); Tag="PRODUCCI$($oacute)N" }
)

$cardLeft = 44
$cardTop = 147
$cardW = 195
$cardH = 220.5
$gap = 28.5

for ($i = 0; $i -lt $steps.Count; $i++) {
    $s = $steps[$i]
    $x = $cardLeft + $i * ($cardW + $gap)
    $card = Add-Box "step-card-$($i+1)" $x $cardTop $cardW $cardH $white $border $true
    $card.Shadow.Visible = -1
    $card.Shadow.Blur = 5
    $card.Shadow.Transparency = 0.82
    $card.Shadow.OffsetX = 1
    $card.Shadow.OffsetY = 2
    (Add-Box "step-number-$($i+1)" ($x+16.5) ($cardTop+16.5) 45 45 $s.Color $s.Color $true) | Out-Null
    (Add-Text "step-number-text-$($i+1)" $s.N ($x+16.5) ($cardTop+25) 45 23 15 $white $true 2 'Aptos Display') | Out-Null
    (Add-Text "step-tag-$($i+1)" $s.Tag ($x+73.5) ($cardTop+26) 100 18 8.5 $s.Color $true 3) | Out-Null
    (Add-Text "step-title-$($i+1)" $s.Title ($x+16.5) ($cardTop+82.5) 162 48 18 $ink $true 1 'Aptos Display') | Out-Null
    (Add-Text "step-body-$($i+1)" $s.Body ($x+16.5) ($cardTop+141) 162 59 12.5 $muted) | Out-Null
    if ($i -lt 3) {
        $arrow = $slide.Shapes.AddShape(52, ($x+$cardW+6), ($cardTop+94.5), 16.5, 31.5)
        $arrow.Fill.ForeColor.RGB = Rgb 170 184 188
        $arrow.Line.Visible = 0
    }
}

$result = Add-Box 'result-band' 44 399 866 84 (Rgb 228 238 240) (Rgb 189 209 214) $true
(Add-Text 'result-label' 'RESULTADO' 62 416 110 18 9.5 $navy $true) | Out-Null
(Add-Text 'result-text' "Cambios de modelo m$($aacute)s $($aacute)giles  $($bullet)  trabajo estandarizado  $($bullet)  base escalable para estaciones inteligentes" 62 438 800 27 15.5 $ink $true 1 'Aptos Display') | Out-Null
(Add-Text 'footer' 'SMART BINS  |  Staff concept sample' 715 505 195 14 7.5 (Rgb 117 136 143) $false 3) | Out-Null

$presentation.SaveAs($pptxPath, 24)
$slide.Export($pngPath, 'PNG', 1920, 1080)
$presentation.Close()
$ppt.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($slide) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($presentation) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($ppt) | Out-Null

Write-Output $pptxPath
Write-Output $pngPath
