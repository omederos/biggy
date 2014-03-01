# assumes you have VS2013 or the standalone MSBuild tools available

$currentDir = Split-Path $MyInvocation.MyCommand.Path
$nuget = Join-Path $currentDir tools\nuget.exe
$MsBuildDir = (Get-ItemProperty -ea:SilentlyContinue ("HKLM:\Software\Microsoft\MSBuild\ToolsVersions\12.0")).MSBuildToolsPath
$msbuild = Join-Path $MsBuildDir msbuild.exe

. $nuget restore
. $msbuild "$currentDir\Biggy.sln" /target:Rebuild /property:Configuration=Release /v:q
. $nuget pack "$currentDir\Biggy.nuspec"