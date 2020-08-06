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
        private ImmutableDictionary<VsTestCase, PuTestCase> _lazyTestMap;

        static PhpUnitContext()
        {
            Context.AddScriptReference(typeof(PuTestCase).Assembly);
        }

        public PhpUnitContext(IEnumerable<string> sources = null)
        {
            _sources = sources?.ToImmutableArray() ?? ImmutableArray<string>.Empty;

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

        private void EnsureTestMap()
        {
            if (_lazyTestMap == null)
            {
                var testLoader = new StandardTestSuiteLoader(_phpContext);
                var testRunner = new TestRunner(_phpContext, testLoader);

                var testSuite = testRunner.getTest("tests");
                var puTestSuites = testSuite.tests().Values.OfType<PuTestSuite>();

                var mapBuilder = ImmutableDictionary.CreateBuilder<VsTestCase, PuTestCase>();
                foreach (var puTestSuite in puTestSuites)
                {
                    var puTests = puTestSuite.tests().Values.OfType<PuTestCase>();
                    foreach (var puTest in puTests)
                    {
                        // TODO: Provide more detailed information

                        var vsTest = new VsTestCase(puTest.getName(), PhpUnitTestExecutor.ExecutorUri, _sources.FirstOrDefault() ?? "");
                        mapBuilder.Add(vsTest, puTest);
                    }
                }

                _lazyTestMap = mapBuilder.ToImmutable();
            }
        }

        public IEnumerable<VsTestCase> FindTestCases()
        {
            EnsureTestMap();
            return _lazyTestMap.Keys;
        }

        public void RunTests(IEnumerable<VsTestCase> vsTests, IFrameworkHandle frameworkHandle)
        {
            EnsureTestMap();

            foreach (var vsTest in vsTests)
            {
                var puTest = _lazyTestMap[vsTest];
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
