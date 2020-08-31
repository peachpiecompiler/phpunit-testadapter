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
    [DefaultExecutorUri(PhpUnitTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    public sealed class PhpUnitTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
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
                string projectDir = EnvironmentHelper.TryFindProjectDirectory(Path.GetDirectoryName(source));
                tempTestsXml = Path.GetTempFileName();
                PhpUnitHelper.Launch(projectDir, source, "--teamcity", "--list-tests-xml", tempTestsXml);     // TODO: Remove --teamcity switch when it no longer causes crash

                ProcessTestsXml(source, tempTestsXml, discoverySink);

                File.Delete(tempTestsXml);
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
                    string methodName = testCaseMethodEl.Attribute("name").Value;

                    string testName = PhpUnitHelper.GetTestNameFromPhp(className, methodName);
                    var testCase = new TestCase(testName, PhpUnitTestExecutor.ExecutorUri, source);

                    discoverySink.SendTestCase(testCase);
                }
            }
        }
    }
}
