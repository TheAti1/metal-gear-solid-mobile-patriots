param([string]$ReleaseRoot=(Join-Path $PSScriptRoot '..\package'))
$ErrorActionPreference='Stop'
$release=[IO.Path]::GetFullPath($ReleaseRoot)
New-Item -ItemType Directory -Path $release -Force | Out-Null
$assets=Join-Path $release 'Arayuz'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$repoAssets=Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
if(Test-Path -LiteralPath $repoAssets){foreach($name in @('game.ico','patriots.jpg','hero.jpg')){Copy-Item -LiteralPath (Join-Path $repoAssets $name) -Destination (Join-Path $assets $name) -Force}}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$build=@('/nologo','/target:winexe','/platform:x64','/optimize+','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:System.Xaml.dll')
foreach($name in @('PresentationFramework','PresentationCore','WindowsBase')){$build+=('/r:'+(Join-Path $framework ('WPF\'+$name+'.dll')))}
$build+=('/win32icon:'+(Join-Path $release 'Arayuz\game.ico'))
$build+=('/out:'+(Join-Path $release 'Metal Gear Solid Mobile.exe'))
$build+=(Join-Path $PSScriptRoot 'Locale.cs')
$build+=('/resource:'+(Join-Path $PSScriptRoot 'launcher.en.json')+',Patriots.launcher.en.json')
$build+=(Join-Path $PSScriptRoot 'NativeServices.cs')
$build+=(Join-Path $PSScriptRoot 'DiscordPresence.cs')
$build+=(Join-Path $PSScriptRoot 'PatriotsLauncher.cs')
& (Join-Path $framework 'csc.exe') $build
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Write-Output 'Patriots launcher 5.0 compiled.'

