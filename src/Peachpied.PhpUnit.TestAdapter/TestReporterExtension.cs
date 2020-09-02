using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Pchp.Core;
using Pchp.Core.Reflection;
using PHPUnit.Runner;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpied.PhpUnit.TestAdapter
{
    internal class TestReporterExtension : BeforeTestHook, AfterSuccessfulTestHook, AfterTestErrorHook, AfterTestFailureHook, AfterSkippedTestHook
    {
        public readonly static string PhpName = typeof(TestReporterExtension).GetPhpTypeInfo().Name;

        private TestRunContext _testRunContext;

        public TestReporterExtension(Context ctx)
        {
            _testRunContext = ctx.TryGetProperty<TestRunContext>() ?? throw new InvalidOperationException();
        }

        public void executeBeforeTest([NotNull] string test) =>
            _testRunContext.FrameworkHandle.RecordStart(GetTestCase(test));

        public void executeAfterSuccessfulTest([NotNull] string test, double time) =>
            ReportOutcome(test, TestOutcome.Passed);

        public void executeAfterTestError([NotNull] string test, [NotNull] string message, double time) =>
            ReportOutcome(test, TestOutcome.Failed);

        public void executeAfterTestFailure([NotNull] string test, [NotNull] string message, double time) =>
            ReportOutcome(test, TestOutcome.Failed);

        public void executeAfterSkippedTest([NotNull] string test, [NotNull] string message, double time) =>
            ReportOutcome(test, TestOutcome.Skipped);

        private void ReportOutcome(string phpTestName, TestOutcome outcome)
        {
            var testCase = GetTestCase(phpTestName);
            var testResult = new TestResult(testCase)
            {
                Outcome = outcome
            };

            _testRunContext.FrameworkHandle.RecordResult(testResult);
        }

        private TestCase GetTestCase(string phpTestName)
        {
            string vsTestName = PhpUnitHelper.GetTestNameFromPhp(phpTestName);
            return new TestCase(vsTestName, PhpUnitTestExecutor.ExecutorUri, _testRunContext.Source);
        }
    }
}
