using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using PHPUnit.TextUI;
using PHPUnit.Runner;

using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using PuTestSuite = PHPUnit.Framework.TestSuite;
using PuTestCase = PHPUnit.Framework.TestCase;

namespace Peachpied.PhpUnit.TestAdapter
{
    internal sealed class PhpUnitContext
    {
        private readonly Context _phpContext;

        private readonly ImmutableArray<string> _sources;
        private ImmutableArray<VsTestCase> _lazyVsTestCases;
        private ImmutableDictionary<string, PuTestCase> _lazyTestMap;

        static PhpUnitContext()
        {
            Context.AddScriptReference(typeof(PuTestCase).Assembly);
        }

        public PhpUnitContext(IEnumerable<string> sources)
        {
            _sources = sources.ToImmutableArray();

            _phpContext = Context.CreateEmpty();
            _phpContext.WorkingDirectory = _phpContext.RootPath = EnvironmentHelper.ProjectDirectory;

            // Load source assemblies
            foreach (string source in _sources)
            {
                Context.AddScriptReference(Assembly.LoadFrom(source));
            }

            // Include all the PhpUnit classes in the context
            var pharLoader = Context.TryGetDeclaredScript("phpunit.phar");
            try
            {
                pharLoader.Evaluate(_phpContext, PhpArray.NewEmpty(), null);
            }
            catch (ScriptDiedException)
            {
            }
        }

        private void EnsureTests()
        {
            if (_lazyVsTestCases.IsDefault || _lazyTestMap == null)
            {
                var testLoader = new StandardTestSuiteLoader(_phpContext);
                var testRunner = new TestRunner(_phpContext, testLoader);

                var testSuite = testRunner.getTest("tests");
                var puTestSuites = testSuite.tests().Values.OfType<PuTestSuite>();

                var vsTestCaseBuilder = ImmutableArray.CreateBuilder<VsTestCase>();
                var mapBuilder = ImmutableDictionary.CreateBuilder<string, PuTestCase>();
                foreach (var puTestSuite in puTestSuites)
                {
                    var puTests = puTestSuite.tests().Values.OfType<PuTestCase>();
                    foreach (var puTest in puTests)
                    {
                        // TODO: Provide more detailed information

                        string name = puTestSuite.getName() + "." + puTest.getName();
                        var vsTest = new VsTestCase(name, PhpUnitTestExecutor.ExecutorUri, _sources.FirstOrDefault() ?? "");
                        vsTestCaseBuilder.Add(vsTest);
                        mapBuilder.Add(name, puTest);
                    }
                }

                _lazyVsTestCases = vsTestCaseBuilder.ToImmutable();
                _lazyTestMap = mapBuilder.ToImmutable();
            }
        }

        public IEnumerable<VsTestCase> FindTestCases()
        {
            EnsureTests();
            return _lazyVsTestCases;
        }

        public void RunTests(IEnumerable<VsTestCase> vsTests, IFrameworkHandle frameworkHandle)
        {
            EnsureTests();

            foreach (var vsTest in vsTests)
            {
                frameworkHandle.RecordStart(vsTest);

                var puTest = _lazyTestMap[vsTest.FullyQualifiedName];
                var puResult = puTest.run();

                var vsResult = new VsTestResult(vsTest);
                if (puResult.skippedCount() > 0)
                {
                    vsResult.Outcome = TestOutcome.Skipped;
                }
                else if (puResult.errorCount() > 0 || puResult.failureCount() > 0 || puResult.notImplementedCount() > 0)
                {
                    vsResult.Outcome = TestOutcome.Failed;
                }
                else
                {
                    vsResult.Outcome = TestOutcome.Passed;
                }

                frameworkHandle.RecordResult(vsResult);
            }
        }
    }
}
