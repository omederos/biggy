# if you don't have VS2013, install standalone MSBuild tools from here
# http://www.microsoft.com/en-us/download/details.aspx?id=39318

function Warning ($text) {
  Write-Host -ForegroundColor Red $text
}

$currentDir = Split-Path $MyInvocation.MyCommand.Path
$nuget = Join-Path $currentDir tools\nuget.exe
$MsBuildDir = (Get-ItemProperty -ea:SilentlyContinue ("HKLM:\Software\Microsoft\MSBuild\ToolsVersions\12.0")).MSBuildToolsPath
$msbuild = Join-Path $MsBuildDir msbuild.exe

if ((Test-Path $msbuild) -eq $false)
{
     Warning "You need to install the MSBuild v12.0 tools to build Biggy!"
     Warning "Install it from http://www.microsoft.com/en-us/download/details.aspx?id=39318 and try again"

     Exit -1
}

. $nuget restore

# ignoring the webapp project as it's not used in the NuGet package
$projects = "Biggy","Biggy.Mongo","Biggy.Mongo.Tests"

foreach ($project in $projects)
{
  Write-Host -ForegroundColor Green "Building project $project"

  $projFile = "$currentDir\$project\$project.csproj"
  . $msbuild $projFile /target:Rebuild /property:Configuration=Release /v:q

  if ($LASTEXITCODE -ne 0)
  {
     Warning "Project failed: $project"
     Warning "Exiting..."

     Exit $LASTEXITCODE
  }
}

. $nuget pack "$currentDir\Biggy.nuspec"