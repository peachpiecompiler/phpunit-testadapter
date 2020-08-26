using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Peachpied.PhpUnit.TestAdapter
{
    [ExtensionUri(PhpUnitTestExecutor.ExecutorUriString)]
    public sealed class PhpUnitTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://PhpUnitTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var ctx = new PhpUnitContext(sources);
            var tests = ctx.FindTestCases();
            
            ctx.RunTests(tests, frameworkHandle);
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
