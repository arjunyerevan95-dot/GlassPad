# SPDX-License-Identifier: GPL-3.0-or-later
param(
    [Parameter(Mandatory=$true)][string]$GameLauncher,
    [Parameter(Mandatory=$true)][string]$PilotExecutable
)
$ErrorActionPreference='Stop'
$glassGame=[IO.Path]::GetFullPath($GameLauncher)
$glassPilot=[IO.Path]::GetFullPath($PilotExecutable)
if(-not (Test-Path -LiteralPath $glassGame -PathType Leaf)){throw 'Game launcher was not found.'}
if(-not (Test-Path -LiteralPath $glassPilot -PathType Leaf)){throw 'Touch-control launcher was not found.'}
if([IO.Path]::GetFileName($glassPilot) -ne 'GlassPad.Queens.exe'){throw 'Select this pilot executable.'}
function ConvertTo-QueensLiteral([string]$Value){return "'"+$Value.Replace("'","''")+"'"}
$glassGameLiteral=ConvertTo-QueensLiteral $glassGame
$glassPilotLiteral=ConvertTo-QueensLiteral $glassPilot
$glassGameDirectory=ConvertTo-QueensLiteral ([IO.Path]::GetDirectoryName($glassGame))
$glassPilotDirectory=ConvertTo-QueensLiteral ([IO.Path]::GetDirectoryName($glassPilot))
$glassCommand="if(-not(Get-Process -Name 'QueensDomain-Win64-Shipping' -ErrorAction SilentlyContinue)){Start-Process -FilePath $glassGameLiteral -WorkingDirectory $glassGameDirectory}; if(-not(Get-Process -Name 'GlassPad.Queens' -ErrorAction SilentlyContinue)){Start-Process -FilePath $glassPilotLiteral -WorkingDirectory $glassPilotDirectory -ArgumentList '--live'}"
$glassShortcutPath=Join-Path ([Environment]::GetFolderPath('Desktop')) 'GlassPad Queens Touch Controls.lnk'
$glassShell=New-Object -ComObject WScript.Shell
$glassShortcut=$glassShell.CreateShortcut($glassShortcutPath)
$glassShortcut.TargetPath=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$glassShortcut.Arguments="-NoProfile -WindowStyle Hidden -Command `"& { $glassCommand }`""
$glassShortcut.WorkingDirectory=[IO.Path]::GetDirectoryName($glassPilot)
$glassShortcut.WindowStyle=7
$glassShortcut.Description='Launch Queen''s Domain and its touch controls together'
$glassShortcut.IconLocation=$glassPilot+',0'
$glassShortcut.Save()
Write-Output "Created combined game and touch shortcut: $glassShortcutPath"
