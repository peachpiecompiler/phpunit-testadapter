﻿using Pchp.Core;
using Pchp.Core.Reflection;
using PHPUnit.Framework;
using PHPUnit.TextUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Peachpied.PhpUnit.TestAdapter
{
    /// <summary>
    /// Helper for running PHPUnit and translating test names.
    /// </summary>
    internal static class PhpUnitHelper
    {
        private const string PharName = "phpunit.phar";

        static PhpUnitHelper()
        {
            // Add PHPUnit assembly to Peachpie
            Context.AddScriptReference(typeof(TestCase).Assembly);
        }

        /// <summary>
        /// Run PHPUnit on the given assembly and command line arguments.
        /// </summary>
        public static void Launch(string cwd, string testedAssembly, string[] args, Action<Context> initCalblack = null, Action<Context> finishCallback = null)
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
                initCalblack?.Invoke(ctx);

                // Run the PHAR entry point so that all the classes are included
                var pharLoader = Context.TryGetDeclaredScript(PharName);
                RunScript(ctx, () => pharLoader.Evaluate(ctx, PhpArray.NewEmpty(), null).ToInt());

                // Run the tests themselves
                RunScript(ctx, () => (int)Command.main(ctx, PhpTypeInfoExtension.GetPhpTypeInfo<Command>()));

                //
                finishCallback?.Invoke(ctx);
            }
        }

        private static void RunScript(Context ctx, Func<int> callback)
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
