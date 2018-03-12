@echo off
:: a scipt for building nuget package

%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Client\JRPC.Client.nuspec
%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Core\JRPC.Core.nuspec
%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Registry.Ninject\JRPC.Registry.Ninject.nuspec
%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Service\JRPC.Service.nuspec
%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Service.Host.Kestrel\JRPC.Service.Host.Kestrel.nuspec
%LOCALAPPDATA%\NuGet\nuget.exe pack src\JRPC.Service.Host.Owin\JRPC.Service.Host.Owin.nuspec

pause
