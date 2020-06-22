using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Devsense.PHP.Syntax;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Library;
using PHPUnit.TextUI;

#nullable enable

namespace DotnetPhpUnit
{
    class Program
    {
        private const string PharName = "phpunit.phar";

        static int Main(string[] args)
        {
            // Find the current MSBuild instance and use its assemblies, as they are not included in the application
            InitMSBuildAssemblies();

            Console.WriteLine("Runner of PHPUnit (© Sebastian Bergmann) on Peachpie projects");
            Console.WriteLine();

            ExtractDotNetArgs(ref args, out bool buildProject);

            string? projectFullPath = FindProject();
            if (projectFullPath == null)
            {
                Console.WriteLine("No Peachpie project found in the current directory");
                return 1;
            }

            if (buildProject)
            {
                Console.WriteLine($"Building \"{projectFullPath}\"...");
                if (!BuildProject(projectFullPath))
                {
                    Console.WriteLine("Error in building the project");
                    return 1;
                }
            }

            string assemblyFullPath = GetBuiltAssemblyPath(projectFullPath);
            if (!File.Exists(assemblyFullPath))
            {
                Console.WriteLine($"Assembly \"{assemblyFullPath}\" does not exist");
                return 1;
            }

            return RunTests(assemblyFullPath, args);
        }

        private static void InitMSBuildAssemblies()
        {
            // Locate the current instance of MSBuild
            var vsInstance = MSBuildLocator.RegisterDefaults();

            // Explicitly load the JSON library in order for Microsoft.Build.NuGetSdkResolver to work
            string jsonLib = Path.Combine(vsInstance.MSBuildPath, "Newtonsoft.Json.dll");
            Assembly.LoadFrom(jsonLib);
        }

        private static void ExtractDotNetArgs(ref string[] args, out bool buildProject)
        {
            // Defaults
            buildProject = true;

            // Parse arguments specific for this tool and remove them from the array
            int noBuildIndex = args.IndexOf("--no-build", StringComparer.Ordinal);
            if (noBuildIndex != -1)
            {
                buildProject = false;
                args = args.Where((_, i) => i != noBuildIndex).ToArray();
            }
        }

        private static string? FindProject()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Directory.GetFiles(cwd, "*.msbuildproj").FirstOrDefault();
        }

        private static bool BuildProject(string projectFullPath)
        {
            using var process = new Process()
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    Arguments = $"build \"{projectFullPath}\""
                },
            };
            try
            {
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetBuiltAssemblyPath(string projectFullPath)
        {
            // Read the final assembly destination using MSBuild
            var project = new Project(projectFullPath);
            string path = project.GetPropertyValue("OutDir") + project.GetPropertyValue("TargetName") + project.GetPropertyValue("TargetExt");
            return Path.GetFullPath(path);
        }

        private static int RunTests(string assemblyFullPath, string[] args)
        {
            Console.WriteLine($"Opening assembly \"{assemblyFullPath}\"...");
            Assembly pchpAssembly;
            try
            {
                pchpAssembly = Assembly.LoadFrom(assemblyFullPath);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error in opening the assembly:");
                Console.WriteLine(e);
                return 1;
            }

            Context.AddScriptReference(pchpAssembly);
            Console.WriteLine("Assembly loaded");
            Console.WriteLine();

            // Initialize PHP context with the current root directory and console output
            using var ctx = Context.CreateConsole(PharName, args);

            // TODO: Clean up the following hack if possible:

            // Try to run PHPUnit from the main entry point in PHAR. Due to the following condition in the PHAR loader
            //
            // if (__FILE__ === realpath($_SERVER['SCRIPT_NAME'])) { ... }
            //
            // it doesn't call Command.main unless a file named "phpunit.phar" is present in the current folder,
            // but it will at least include all the files containing class definitions.
            var pharLoader = Context.TryGetDeclaredScript(PharName);
            int exitCode = RunScript(ctx, () => pharLoader.Evaluate(ctx, PhpArray.NewEmpty(), null).ToInt());
            if (PhpPath.realpath(ctx, PharName) != null)
            {
                // It has already run, just return the exit code
                return exitCode;
            }
            else
            {
                // It hasn't properly run yet, ignore the previous exit code and re-run directly
                return RunScript(ctx, () => (int)Command.main(ctx, PhpTypeInfoExtension.GetPhpTypeInfo<Command>()));
            }
        }

        private static int RunScript(Context ctx, Func<int> callback)
        {
            try
            {
                return callback();
            }
            catch (ScriptDiedException e)
            {
                return e.ProcessStatus(ctx);
            }
        }
    }
}
