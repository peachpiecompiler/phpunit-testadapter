using Pchp.Core;
using Pchp.Core.Reflection;
using PHPUnit.Framework;
using PHPUnit.TextUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// Helper for running PHPUnit and translating test names.
    /// </summary>
    internal static class PhpUnitHelper
    {
        /// <summary>
        /// Current PHPUnit version.
        /// </summary>
        public static Version Version { get; } = typeof(TestCase).Assembly.GetName().Version;

        private const string PharName = "phpunit.phar";

        static PhpUnitHelper()
        {
            // Add PHPUnit assembly to Peachpie
            Context.AddScriptReference(typeof(TestCase).Assembly);
        }

        /// <summary>
        /// Find a PHPUnit configuration file in the given directory, return <c>null</c> if not present.
        /// </summary>
        public static string TryFindConfigFile(string dir)
        {
            string primaryConfig = Path.Combine(dir, "phpunit.xml");
            if (File.Exists(primaryConfig))
            {
                return primaryConfig;
            }

            string secondaryConfig = Path.Combine(dir, "phpunit.xml.dist");
            if (File.Exists(secondaryConfig))
            {
                return secondaryConfig;
            }

            return null;
        }

        /// <summary>
        /// Run PHPUnit on the given assembly and command line arguments.
        /// </summary>
        public static void Launch(string cwd, string testedAssembly, string[] args, Action<Context> initCallback = null, Action<Context> finishCallback = null)
        {
            // Load assembly with tests (if not loaded yet)
            Context.AddScriptReference(Assembly.LoadFrom(testedAssembly));

            using (var ctx = Context.CreateConsole(PharName, args))
            {
                ctx.WorkingDirectory = ctx.RootPath = cwd;

                // Set $_SERVER['SCRIPT_NAME'] to be different from __FILE__ so that it NEVER executes
                // (there is the condition for execution __FILE__ === realpath($_SERVER['SCRIPT_NAME']) in PHPUnit PHAR entry file)
                ctx.Server[CommonPhpArrayKeys.SCRIPT_NAME] = "__DUMMY_INVALID_FILE";

                // Perform any custom operations on the context
                initCallback?.Invoke(ctx);

                // Run the PHAR entry point so that all the classes are included
                var pharLoader = Context.TryGetDeclaredScript(PharName);
                if (pharLoader.IsValid)
                {
                    // only in case we are running from phar, otherwise classes are autoloaded by runtime
                    RunScript(ctx, () => pharLoader.Evaluate(ctx, PhpArray.NewEmpty(), null));
                }

                // Run the tests themselves
                RunScript(ctx, () => new Command(ctx).run(new PhpArray(args), exit: true));

                //
                finishCallback?.Invoke(ctx);
            }
        }

        private static void RunScript<TResult>(Context ctx, Func<TResult> callback)
        {
            try
            {
                callback();
            }
            catch (ScriptDiedException e)
            {
                e.ProcessStatus(ctx);
            }
        }

        /// <summary>
        /// E.g. <c>My\NS\TestClass::test</c> to <c>My.NS.TestClass.test</c>.
        /// </summary>
        public static string GetTestNameFromPhp(string fullTestName) =>
            fullTestName.Replace('\\', '.').Replace("::", ".");

        /// <summary>
        /// E.g. <c>My\NS\TestClass</c>, <c>test</c> to <c>My.NS.TestClass.test</c>.
        /// </summary>
        public static string GetTestNameFromPhp(string className, string methodName) =>
            className.Replace('\\', '.') + "." + methodName;

        /// <summary>
        /// E.g. <c>My\NS\TestClass</c>, <c>test</c> to <c>My.NS.TestClass.test</c>.
        /// </summary>
        public static string GetTestNameFromPhp(PhpTypeInfo classInfo, string methodName) =>
            classInfo.Type.FullName + "." + methodName;

        /// <summary>
        /// E.g. <c>My.NS.TestClass.test</c> to <c>My\NS\TestClass::test</c>.
        /// </summary>
        public static string GetPhpTestName(string fullTestName)
        {
            int methodSepPos = fullTestName.LastIndexOf('.');
            if (methodSepPos == -1)
            {
                return fullTestName;
            }

            string dotnetNamespacedClassName = fullTestName.Substring(0, methodSepPos);
            string methodName = fullTestName.Substring(methodSepPos + 1);

            return dotnetNamespacedClassName.Replace('.', '\\') + "::" + methodName;
        }
    }
}
