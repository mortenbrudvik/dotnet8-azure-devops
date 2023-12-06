using System;
using System.IO;

public string MSBuildPath = MSBuildResolver.GetMSBuildPath();

public static class MSBuildResolver
{
    private static readonly List<string> _editions = new() {"professional", "enterprise", "community", "BuildTools"};
    private static readonly List<string> _years = new() {"2022"};
    private static readonly List<string> _platforms = new() {"x86", "x64"};

    public static string GetMSBuildPath()
    {
        Console.Out.WriteLine("Trying to locate msbuild.exe");

        foreach(var platform in  _platforms)
        {
            foreach(var year in _years)
            {
                foreach (var edition in _editions)
                {
                    var path = GetMSBuildPath(year, edition, platform);
                    Console.Out.WriteLine(edition +": " + path);
                    if (File.Exists(path))
                    {
                        Console.Out.WriteLine("Hooray! Msbuild path located: " + path );
                        return path;
                    }
                }
            }
        }

        throw new Exception("Failed to locate msbuild file path");
    }

    private static string GetMSBuildPath(string year, string edition, string platform)
    {
        var programFilesPath = platform == "x64" ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.ProgramFilesX86;
        return Path.Join(Environment.GetFolderPath(programFilesPath), $@"\Microsoft Visual Studio\{year}\{edition}\MSBuild\Current\Bin\msbuild.exe");
    }
}