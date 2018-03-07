#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin "Cake.ExtendedNuGet"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = Argument("version", "2.0.0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var output = Directory(".");
var outputBinaries = output + Directory("binaries");
var outputBinariesNet451 = outputBinaries + Directory("net451");
var outputBinariesNetstandard = outputBinaries + Directory("netstandard1.5");
var outputPackages = output + Directory("packages");
var outputNuGet = output + Directory("nuget");
var outputPack = output + Directory("pack");

var solutionFile = "./IDEV.JRPC.sln";
var solution = new Lazy<SolutionParserResult>(() => ParseSolution(solutionFile));
var distDir = Directory("./nuget");

var nugetApiKey = "oy2kao4u4vmjvss46mwaao4kkjzimg6e7kicccry3wr5zu";

//var buildDir = Directory("./src/Example/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////



Task("Clean")
	.IsDependentOn("Clean-Outputs")
	.Does(() => 
	{
		DotNetBuild(solutionFile, settings => settings
			.SetConfiguration(configuration)
			.WithTarget("Clean")
			.SetVerbosity(Verbosity.Minimal));
	});

Task("Clean-Outputs")
	.Does(() => 
	{
		CleanDirectory(outputNuGet);
	});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild(solutionFile, settings =>
        settings.SetConfiguration(configuration).UseToolVersion(MSBuildToolVersion.Default));
    }
    else
    {
      // Use XBuild
      XBuild(solutionFile, settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testAssemblies = GetFiles("./src/**/bin/" + configuration + "/*Tests.dll");
    NUnit3(testAssemblies, new NUnit3Settings {
            NoResults = true,
		    Configuration = configuration,
		    Full = true
        });
});

Task("Package")
  .Description("Generates NuGet packages for each project that contains a nuspec")
  .IsDependentOn("Build")
  .Does(() =>
{
  var projects = GetFiles("./src/**/*.nuspec");
  foreach(var project in projects)
  {
		NuGetPack (project.ToString(), new NuGetPackSettings
        {
            BasePath = project.GetDirectory().ToString(),
            OutputDirectory = outputNuGet,
			Version = version
        });
  };
});

Task("Deploy")
  .Does(() => {
    var pkgs = GetFiles("./nuget/*.nupkg");
    NuGetPush(pkgs, new NuGetPushSettings {
      Source = "https://api.nuget.org/v3/index.json",
      ApiKey = nugetApiKey
	});
});
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);