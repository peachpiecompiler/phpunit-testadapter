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
    [ExtensionUri(PhpUnitTestExecutor.ExecutorUriString)]
    public sealed class PhpUnitTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://PhpUnitTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                RunSourceTest(source, frameworkHandle);
            }
        }

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
                var args = new[] { "--teamcity", "--extensions", TestReporterExtension.PhpName };

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
                        ctx.DeclareType<TestReporterExtension>();

                        var testRunCtx = new TestRunContext(source, frameworkHandle);
                        ctx.SetProperty(testRunCtx);
                    });
            }
            catch (Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, e.Message + "\n" + e.StackTrace);
            }
        }

        public void Cancel()
        {
            // TODO
        }
    }
}
