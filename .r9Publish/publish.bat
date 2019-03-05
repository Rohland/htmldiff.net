@echo off
rem ..\nuget.exe pack
rem ..\nuget.exe push -Source "VSTS Proxy" -ApiKey VSTS GaebToolboxV320.*.nupkg
nuget.exe push -Source "ePlatoOwn" -ApiKey VSTS ..\HtmlDiff\bin\Release\htmldiff.net.*.nupkg
