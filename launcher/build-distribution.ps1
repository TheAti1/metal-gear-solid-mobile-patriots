param([string]$ReleaseRoot=(Join-Path $PSScriptRoot "..\.."))
$ErrorActionPreference="Stop"
$release=[IO.Path]::GetFullPath($ReleaseRoot)
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$build=@('/nologo','/target:winexe','/platform:x64','/optimize+','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:System.Xaml.dll')
foreach($name in @('PresentationFramework','PresentationCore','WindowsBase')){$build+=('/r:'+(Join-Path $framework ('WPF\'+$name+'.dll')))}
$build+=('/win32icon:'+(Join-Path $release 'Arayuz\game.ico'))
$build+=('/out:'+(Join-Path $release 'Metal Gear Solid Mobile.exe'))
$build+=(Join-Path $PSScriptRoot 'NativeServices.cs')
$build+=(Join-Path $PSScriptRoot 'DiscordPresence.cs')
$build+=(Join-Path $PSScriptRoot 'PatriotsLauncher.cs')
& (Join-Path $framework 'csc.exe') $build
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Write-Output 'Patriots launcher 4.0 compiled.'
