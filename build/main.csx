#load "build-utils.csx"

#r "nuget: SimpleExec, 11.0.0"
#r "nuget: CommandLineParser, 2.9.1"
#r "nuget: Bullseye, 3.8.0"

using System.IO;
using CommandLine;
using System.IO.Compression;
using static SimpleExec.Command;
using static Bullseye.Targets;
using static System.Console;
using System;

string version => $"1.0.0"; 

var artifactsDir = "artifacts";
var publishDir = $"{artifactsDir}\\publish";
var obfuscateDir = $"{artifactsDir}\\publish_obfuscar";

string zipFilePath => Path.Join(artifactsDir,"binaries.zip");

string msiFileName => $"MyConsole.{version}";
string msiFilePath  => Path.Join(artifactsDir, msiFileName + ".msi");

public void DeleteDirectory(string path)
{
    if( Directory.Exists(path))
        Directory.Delete(path, recursive: true);
}

public void CopyFiles(string sourceDir, string destDir)
{
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var dest = Path.Combine(destDir, Path.GetFileName(file));
        File.Copy(file, dest, true);
    }
}

public sealed class Options
{
    [Option('t', "target", Required = false, Default = "build-msi")]
    public string Target { get; set; }
    
    [Option("buildNumber", Required = false, Default = "0")]
    public string BuildNumber { get; set; }
}

Options options;

Target("delete-artifacts", () =>
    DeleteDirectory(artifactsDir));

Target("tool-restore",
    DependsOn("delete-artifacts"), () => 
    Run("dotnet", "tool restore"));

Target("build-binaries", 
    DependsOn("tool-restore"), () => 
    Run("dotnet", $@"publish ..\ -c Release --self-contained -o {publishDir} "));

Target("create-licenses-file",
    DependsOn("build-binaries"), () => {
        Run("dotnet", $"dotnet-project-licenses --log-level Verbose --input ../dotnet8-azure-devops.sln --projects-filter license-ignore-projects.json --unique --output --outfile {artifactsDir}\\licenses.txt");
        File.Copy($"{artifactsDir}\\licenses.txt", $"{publishDir}\\licenses.txt");});

Target("obfuscate-code",
    DependsOn("create-licenses-file"), () => {
        Run("dotnet", $@"tool run obfuscar.console .\obfuscar.config");
        CopyFiles($"{obfuscateDir}\\", $"{publishDir}\\");
    }); 

Target("zip-build-files",
    DependsOn("obfuscate-code"), () =>
    ZipFile.CreateFromDirectory(publishDir, zipFilePath));

Target("build-msi",
    DependsOn("zip-build-files"), () => 
    Run(MSBuildPath, $@"..\Installer\ /p:Configuration=Release /p:MsiFileName={msiFileName} /p:FileVersion={version} /noconsolelogger"));

Parser.Default.ParseArguments<Options>(Args).WithParsed<Options>(o =>
{
    WriteLine("Starting building EnifyEngine");

    var args = new List<string>(){o.Target};

    RunTargetsAndExit(args);
});