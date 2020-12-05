using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Pchp.Core;
using Pchp.Core.Reflection;
using PHPUnit.Runner;
using System;
using System.Collections.Generic;
using System.Text;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// Custom PHPUnit extension to report the test results.
    /// Expects <see cref="TestRunContext"/> in the properties of <see cref="Context"/>.
    /// </summary>
    internal class TestReporterExtension : BeforeTestHook, AfterSuccessfulTestHook, AfterTestErrorHook, AfterTestFailureHook, AfterSkippedTestHook
    {
        public static string PhpName => PhpTypeInfoExtension.GetPhpTypeInfo<TestReporterExtension>().Name;

        readonly TestRunContext _testRunContext;

        public TestReporterExtension(Context ctx)
        {
            // Restore the test run information from the Context
            _testRunContext = ctx.TryGetProperty<TestRunContext>() ?? throw new InvalidOperationException();
        }

        public void executeBeforeTest(string test) =>
            _testRunContext.FrameworkHandle.RecordStart(GetTestCase(test));

        public void executeAfterSuccessfulTest(string test, double time) =>
            ReportOutcome(test, TestOutcome.Passed, time: time);

        public void executeAfterTestError(string test, string message, double time) =>
            ReportOutcome(test, TestOutcome.Failed, message, time);

        public void executeAfterTestFailure(string test, string message, double time) =>
            ReportOutcome(test, TestOutcome.Failed, message, time);

        public void executeAfterSkippedTest(string test, string message, double time) =>
            ReportOutcome(test, TestOutcome.Skipped, message, time);

        private void ReportOutcome(string phpTestName, TestOutcome outcome, string message = null, double time = 0.0)
        {
            var testCase = GetTestCase(phpTestName);
            var testResult = new TestResult(testCase)
            {
                Outcome = outcome,
                ErrorMessage = message,
                Duration = TimeSpan.FromSeconds(time),
            };

            _testRunContext.FrameworkHandle.RecordResult(testResult);
        }

        private TestCase GetTestCase(string fullPhpTestName)
        {
            // "class::method with data set #0 ('foo', ...)" -> "class::method"
            int dataSetStrPos = fullPhpTestName.IndexOf(" with data set ");
            string phpTestName =
                (dataSetStrPos != -1)
                ? fullPhpTestName.Substring(0, dataSetStrPos)
                : fullPhpTestName;

            string vsTestName = PhpUnitHelper.GetTestNameFromPhp(phpTestName);
            return new TestCase(vsTestName, PhpUnitTestExecutor.ExecutorUri, _testRunContext.Source)
            {
                DisplayName = fullPhpTestName   // Preserve the original data set information if present
            };
        }
    }
}
