#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = Argument("version", "1.0.0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var output = Directory(".");
var outputBinaries = output + Directory("binaries");
var outputBinariesNet452 = outputBinaries + Directory("net452");
var outputBinariesNetstandard = outputBinaries + Directory("netstandard1.5");
var outputPackages = output + Directory("packages");
var outputNuGet = output + Directory("nuget");

var buildDir = Directory("./src/Example/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./IDEV.JRPC.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./IDEV.JRPC.sln", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild("./IDEV.JRPC.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

Task("Package-NuGet")
  .Description("Generates NuGet packages for each project that contains a nuspec")
  .IsDependentOn("Build")
  .Does(() =>
{
  var projects = GetFiles("./src/**/*.csproj");
  foreach(var project in projects)
  {
		NuGetPack (project.ChangeExtension("nuspec").ToString(), new NuGetPackSettings
        {
            BasePath = project.GetDirectory().ToString(),
            OutputDirectory = outputNuGet,
			Version = version
        });
  }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Package-NuGet");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);