using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

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
                try
                {
                    string projectDir = EnvironmentHelper.TryFindProjectDirectory(Path.GetDirectoryName(source));

                    PhpUnitHelper.Launch(projectDir, source, new[] { "--teamcity", "--extensions", TestReporterExtension.PhpName },
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
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var sources = tests.Select(s => s.Source).Distinct().ToImmutableArray();

            var ctx = new PhpUnitContext(sources);

            ctx.RunTests(tests, frameworkHandle);
        }

        public void Cancel()
        {
            // TODO
        }
    }
}
