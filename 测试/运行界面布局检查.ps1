$ErrorActionPreference = 'Stop'

$testDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDirectory = Split-Path -Parent $testDirectory
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = (Get-ChildItem -LiteralPath $testDirectory -Filter '*.cs' | Select-Object -First 1).FullName
$testProgram = Join-Path $projectDirectory 'layout-check.exe'
$outputArgument = '/out:' + $testProgram
$programName = ([string][char]0x70B9) + ([string][char]0x540D) + ([string][char]0x5566)
$application = Join-Path $projectDirectory ($programName + ' V1.1.exe')
$preview = Join-Path $testDirectory 'layout-preview.png'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Windows C# compiler was not found.'
}

Push-Location $projectDirectory
try {
    & $compiler `
        /nologo `
        /target:exe `
        /platform:anycpu `
        /codepage:65001 `
        $outputArgument `
        /reference:System.dll `
        /reference:System.Core.dll `
        /reference:System.Drawing.dll `
        /reference:System.Windows.Forms.dll `
        $source

    if ($LASTEXITCODE -ne 0) {
        throw "Layout test build failed. Exit code: $LASTEXITCODE"
    }

    & $testProgram $application $preview
    if ($LASTEXITCODE -ne 0) {
        throw "Layout test failed. Exit code: $LASTEXITCODE"
    }
}
finally {
    Pop-Location
    if (Test-Path -LiteralPath $testProgram) {
        Remove-Item -LiteralPath $testProgram -Force
    }
}

Write-Host "Preview: $preview"
