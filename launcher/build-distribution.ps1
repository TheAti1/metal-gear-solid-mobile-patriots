param([string]$ReleaseRoot=(Join-Path $PSScriptRoot '..\package'))
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -ReleaseRoot $ReleaseRoot
