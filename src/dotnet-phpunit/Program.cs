using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Library;
using PHPUnit.TextUI;

namespace DotnetPhpUnit
{
    class Program
    {
        private const string PharName = "phpunit.phar";

        static int Main(string[] args)
        {
            Console.WriteLine("Runner of PHPUnit (© Sebastian Bergmann) on PHP assemblies compiled by Peachpie");
            Console.WriteLine();

            if (args.Length == 0 || Path.GetExtension(args[0]) != ".dll")
            {
                Console.WriteLine("Usage: dotnet phpunit [pathToDll] [PHPUnit arguments...]");
                return 1;
            }

            string assemblyFullPath = Path.GetFullPath(args[0]);
            if (!File.Exists(assemblyFullPath))
            {
                Console.WriteLine($"Assembly \"{assemblyFullPath}\" does not exist");
                return 1;
            }

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

            // Strip the loaded assembly from arguments passed to PHPUnit
            args = args.Skip(1).ToArray();

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
