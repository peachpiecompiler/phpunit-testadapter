using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Peachpied.PhpUnit.TestAdapter
{
    /// <summary>
    /// Implementation of the <see cref="ITestExecutor"/> interface used to run tests.
    /// </summary>
    [ExtensionUri(PhpUnitTestExecutor.ExecutorUriString)]
    public sealed class PhpUnitTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://PhpUnitTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        /// <summary>
        /// Run all the tests in the assemblies, used e.g. from <c>dotnet test</c>.
        /// </summary>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                RunSourceTest(source, frameworkHandle);
            }
        }

        /// <summary>
        /// Run selected tests, used e.g. from Test Explorer in Microsoft Visual Studio.
        /// </summary>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var sourceGroup in tests.GroupBy(t => t.Source))
            {
                RunSourceTest(sourceGroup.Key, frameworkHandle, sourceGroup);
            }
        }

        private void RunSourceTest(string source, IFrameworkHandle frameworkHandle, IEnumerable<TestCase> testCases = null)
        {
            try
            {
                // Inject our custom extension to report the test results
                var args = new[] { "--extensions", TestReporterExtension.PhpName };

                // Optionally filter the test cases by their names
                if (testCases != null)
                {
                    var filterItems =
                        from testCase in testCases
                        let testName = PhpUnitHelper.GetPhpTestName(testCase.FullyQualifiedName)
                        select $"^{Regex.Escape(testName)}(\\s|$)";

                    string filter = $"({string.Join("|", filterItems)})";

                    args = args.Concat(new[] { "--filter", filter }).ToArray();
                }

                string projectDir = EnvironmentHelper.TryFindProjectDirectory(Path.GetDirectoryName(source));

                PhpUnitHelper.Launch(projectDir, source, args,
                    ctx =>
                    {
                        // Enable Peachpie to create an instance of our custom extension
                        ctx.DeclareType<TestReporterExtension>();

                        // Pass data to the extension via Context
                        var testRunCtx = new TestRunContext(source, frameworkHandle);
                        ctx.SetProperty(testRunCtx);
                    });
            }
            catch (Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Cancel test execution, currently ignored.
        /// </summary>
        public void Cancel()
        {
            // TODO
        }
    }
}
