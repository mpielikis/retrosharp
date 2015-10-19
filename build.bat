@echo off

cls
.nuget\NuGet.exe restore
"%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"
