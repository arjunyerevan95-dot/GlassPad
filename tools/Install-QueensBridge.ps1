param([Parameter(Mandatory=$true)][string]$GameExecutable,[switch]$Remove)
$ErrorActionPreference='Stop'
$glassExe=[IO.Path]::GetFullPath($GameExecutable)
if([IO.Path]::GetFileName($glassExe) -ne 'QueensDomain-Win64-Shipping.exe'){throw 'Select the Queens Domain shipping executable.'}
if((Get-FileHash -LiteralPath $glassExe -Algorithm SHA256).Hash -ne '8D805CFF01753F9066BF60E4432A02D91533E4741AC0EAF1649B3C1CD17EE9CF'){throw 'Game executable identity differs from the verified build; nothing changed.'}
if(Get-Process -Name 'QueensDomain-Win64-Shipping' -ErrorAction SilentlyContinue){throw 'Quit Queens Domain before installing or removing its local bridge.'}
$glassGameDir=[IO.Path]::GetDirectoryName($glassExe)
$glassTarget=[IO.Path]::GetFullPath((Join-Path $glassGameDir 'xinput1_3.dll'))
if([IO.Path]::GetDirectoryName($glassTarget) -ne $glassGameDir){throw 'Bridge target escaped the game directory.'}
$glassSource=Join-Path $PSScriptRoot '..\bridge\xinput1_3.dll'
if(-not (Test-Path -LiteralPath $glassSource)){$glassSource=Join-Path $PSScriptRoot '..\src\GlassPad.XInputBridge\bin\xinput1_3.dll'}
$glassHash=(Get-FileHash -LiteralPath $glassSource -Algorithm SHA256).Hash
# Recognize the exact earlier pilot installed on this PC, even after rebuilding.
$glassKnownHashes=@($glassHash,'3C8E08CB63F66AE3F9E0B56D285D8A5265C67CAA6664573EC0144B0FDA253542')
$glassSettings=Join-Path $env:APPDATA 'GlassPad-Queens-Pilot'
$glassReceipt=Join-Path $glassSettings 'bridge-install.json'
if($Remove)
{
    if(Test-Path -LiteralPath $glassTarget)
    {
        if((Get-FileHash -LiteralPath $glassTarget -Algorithm SHA256).Hash -notin $glassKnownHashes){throw 'The game DLL differs from known fork bridges. It was preserved.'}
        Remove-Item -LiteralPath $glassTarget
    }
    Write-Output 'Removed only this fork game-local XInput bridge. No driver, firmware or global settings changed.'
    return
}
if(Test-Path -LiteralPath $glassTarget)
{
    if((Get-FileHash -LiteralPath $glassTarget -Algorithm SHA256).Hash -notin $glassKnownHashes){throw 'An existing game-local XInput DLL was found and preserved. No files changed.'}
    Write-Output 'A verified compatible pilot bridge is already installed.'
    return
}
$glassInput=[IO.File]::OpenRead([IO.Path]::GetFullPath($glassSource))
try{$glassOutput=[IO.File]::Open($glassTarget,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write);try{$glassInput.CopyTo($glassOutput);$glassOutput.Flush($true)}finally{$glassOutput.Dispose()}}finally{$glassInput.Dispose()}
if((Get-FileHash -LiteralPath $glassTarget -Algorithm SHA256).Hash -ne $glassHash){throw 'Bridge copy verification failed.'}
New-Item -ItemType Directory -Path $glassSettings -Force | Out-Null
@{Version=1;Target=$glassTarget;Sha256=$glassHash;InstalledAt=[DateTimeOffset]::Now.ToString('O');OriginalFileExisted=$false;GameExecutableSha256='8D805CFF01753F9066BF60E4432A02D91533E4741AC0EAF1649B3C1CD17EE9CF'} | ConvertTo-Json | Set-Content -LiteralPath $glassReceipt
Write-Output "Installed and hash-verified the local bridge: $glassTarget"
