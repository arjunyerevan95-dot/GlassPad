# SPDX-License-Identifier: GPL-3.0-or-later
param([Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$glassRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$glassOutput=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $glassOutput){throw 'Choose a new output folder; existing packages are preserved.'}
& (Join-Path $glassRepo 'src\GlassPad.XInputBridge\build.cmd')
if($LASTEXITCODE){throw 'Native bridge build failed.'}
& dotnet publish (Join-Path $glassRepo 'src\GlassPad') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $glassOutput
if($LASTEXITCODE){throw 'Self-contained publish failed.'}
New-Item -ItemType Directory -Path (Join-Path $glassOutput 'tools') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-QueensBridge.ps1') -Destination (Join-Path $glassOutput 'tools')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'New-QueensShortcut.ps1') -Destination (Join-Path $glassOutput 'tools')
Copy-Item -LiteralPath (Join-Path $glassRepo 'docs') -Destination $glassOutput -Recurse
Copy-Item -LiteralPath (Join-Path $glassRepo 'README.md') -Destination $glassOutput
Copy-Item -LiteralPath (Join-Path $glassRepo 'CONTINUATION.md') -Destination $glassOutput
[IO.File]::WriteAllText((Join-Path $glassOutput 'Launch-GlassPad.cmd'),"@echo off`r`nstart `"`" `"%~dp0GlassPad.Queens.exe`" --live`r`n",[Text.Encoding]::ASCII)
$glassCommit=(& git -C $glassRepo rev-parse HEAD).Trim()
if($LASTEXITCODE){throw 'Could not record source commit.'}
$glassManifest=[ordered]@{
    version='1.1.0-R4'; sourceCommit=$glassCommit
    upstreamBase='94fd8c46610a5c614dc938f06648e1f7a30c7d4c'
    sourceUrl="https://github.com/arjunyerevan95-dot/GlassPad/tree/$glassCommit"
    branch='wo-glasspad-queens-001'; runtime='Self-contained Windows x64 .NET 10'
    driver='Uses existing ViGEm; no driver installer is run'
    physicalAcceptance='Owner confirmed reliable move/look; four-contact gameplay, gyro feel, physical resume and performance comparison remain pending'
}
$glassManifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $glassOutput 'BUILD.json') -Encoding UTF8
Write-Output "Packaged $glassCommit in $glassOutput"
