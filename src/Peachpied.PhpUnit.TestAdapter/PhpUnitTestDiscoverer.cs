using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Peachpied.PhpUnit.TestAdapter
{
    /// <summary>
    /// Implementation of the <see cref="ITestDiscoverer"/> interface used to discover tests prior to running
    /// them in interactive scenarios, e.g. Test Explorer in Microsoft Visual Studio.
    /// </summary>
    [DefaultExecutorUri(PhpUnitTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    public sealed class PhpUnitTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            // Run each assembly (project) separately
            foreach (var source in sources)
            {
                try
                {
                    ProcessSource(discoverySink, source);
                }
                catch (Exception e)
                {
                    logger.SendMessage(TestMessageLevel.Error, e.Message + "\n" + e.StackTrace);
                }
            }
        }

        private static void ProcessSource(ITestCaseDiscoverySink discoverySink, string source)
        {
            string tempTestsXml = null;

            try
            {
                // Use XML output of PHPUnit to gather all the tests and report them
                string projectDir = EnvironmentHelper.TryFindProjectDirectory(Path.GetDirectoryName(source));
                tempTestsXml = Path.GetTempFileName();
                PhpUnitHelper.Launch(projectDir, source, new[] { "--list-tests-xml", tempTestsXml });

                ProcessTestsXml(source, tempTestsXml, discoverySink);
            }
            finally
            {
                if (File.Exists(tempTestsXml))
                {
                    File.Delete(tempTestsXml);
                }
            }
        }

        private static void ProcessTestsXml(string source, string path, ITestCaseDiscoverySink discoverySink)
        {
            var testsEl = XElement.Load(path);
            foreach (var testCaseClassEl in testsEl.Descendants("testCaseClass"))
            {
                string className = testCaseClassEl.Attribute("name").Value;

                foreach (var testCaseMethodEl in testCaseClassEl.Descendants("testCaseMethod"))
                {
                    var methodName = testCaseMethodEl.Attribute("name").Value;

                    var testName = PhpUnitHelper.GetTestNameFromPhp(className, methodName);
                    var testCase = new TestCase(testName, PhpUnitTestExecutor.ExecutorUri, source);

                    ProcessTraits(testCase, testCaseMethodEl.Attribute("groups")?.Value);

                    //testCase.CodeFilePath
                    discoverySink.SendTestCase(testCase);
                }
            }
        }

        private static void ProcessTraits(TestCase testCase, string groups)
        {
            if (!string.IsNullOrEmpty(groups))
            {
                foreach (var group in groups.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    testCase.Traits.Add(group, null);
                }
            }
        }
    }
}
