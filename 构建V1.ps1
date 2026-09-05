$ErrorActionPreference = 'Stop'

$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$frameworkDirectory = Split-Path -Parent $compiler
$webExtensions = Join-Path $frameworkDirectory 'System.Web.Extensions.dll'
$windowsBase = Join-Path $frameworkDirectory 'WPF\WindowsBase.dll'
$presentationCore = Join-Path $frameworkDirectory 'WPF\PresentationCore.dll'
$sourceItem = Get-ChildItem -LiteralPath $projectDirectory -Filter '*V1.0.cs' | Select-Object -First 1
$sources = Get-ChildItem -LiteralPath $projectDirectory -Filter '*.cs' | Select-Object -ExpandProperty FullName
$programName = ([string][char]0x70B9) + ([string][char]0x540D) + ([string][char]0x5566)
$output = Join-Path $projectDirectory ($programName + '.exe')
$icon = (Get-ChildItem -LiteralPath (Join-Path $projectDirectory 'assets') -Filter '*.ico' | Select-Object -First 1).FullName
$musicDirectory = Join-Path $projectDirectory 'music'
$takeTheRide = (Get-ChildItem -LiteralPath $musicDirectory -Filter '*Take the Ride.mp3' | Select-Object -First 1).FullName
$rush = (Get-ChildItem -LiteralPath $musicDirectory -Filter '*Rush.mp3' | Select-Object -First 1).FullName
$springChicken = (Get-ChildItem -LiteralPath $musicDirectory -Filter '*Spring Chicken.mp3' | Select-Object -First 1).FullName
$takeTheRideResource = '/resource:' + $takeTheRide + ',DianMingLa.Music.TakeTheRide.mp3'
$rushResource = '/resource:' + $rush + ',DianMingLa.Music.Rush.mp3'
$springChickenResource = '/resource:' + $springChicken + ',DianMingLa.Music.SpringChicken.mp3'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Windows C# compiler was not found.'
}

& $compiler `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    /win32icon:$icon `
    $takeTheRideResource `
    $rushResource `
    $springChickenResource `
    /codepage:65001 `
    /out:$output `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:$webExtensions `
    /reference:$windowsBase `
    /reference:$presentationCore `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Exit code: $LASTEXITCODE"
}

Write-Host "Built: $output"
