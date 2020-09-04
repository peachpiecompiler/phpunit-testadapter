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
                PhpUnitHelper.Launch(projectDir, source, new[] { "--list-tests-xml", tempTestsXml },
                    finishCallback: ctx => ProcessTestsXml(ctx, source, tempTestsXml, discoverySink)
                );
            }
            finally
            {
                if (File.Exists(tempTestsXml))
                {
                    File.Delete(tempTestsXml);
                }
            }
        }

        private static void ProcessTestsXml(Pchp.Core.Context ctx, string source, string path, ITestCaseDiscoverySink discoverySink)
        {
            var testsEl = XElement.Load(path);
            foreach (var testCaseClassEl in testsEl.Descendants("testCaseClass"))
            {
                var phpClassName = testCaseClassEl.Attribute("name").Value;
                var classInfo = ctx.GetDeclaredType(phpClassName, autoload: false);
                var filePath = Path.GetFullPath(Path.Combine(ctx.RootPath, classInfo.RelativePath));

                string[] fileContent = null;

                foreach (var testCaseMethodEl in testCaseClassEl.Descendants("testCaseMethod"))
                {
                    var methodName = testCaseMethodEl.Attribute("name").Value;

                    var testName = PhpUnitHelper.GetTestNameFromPhp(classInfo, methodName);
                    var testCase = new TestCase(testName, PhpUnitTestExecutor.ExecutorUri, source)
                    {
                        DisplayName = $"function {classInfo.Name}::{methodName}()",
                        CodeFilePath = filePath,
                        LineNumber = GetLineNumber(filePath, phpClassName, methodName, ref fileContent),
                    };

                    ProcessTraits(testCase, testCaseMethodEl.Attribute("groups")?.Value);

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

        private static int GetLineNumber(string filePath, string className, string methodName, ref string[] fileContent)
        {
            // trim off namespace part of class name
            var sep = className.IndexOf('\\');
            if (sep >= 0) className = className.Substring(sep + 1);

            // read the file
            if (fileContent == null)
            {
                fileContent = File.ReadAllLines(filePath);
            }

            // find the class:
            int classLine = 0;

            for (int i = 0; i < fileContent.Length; i++)
            {
                if (fileContent[i].IndexOf("class", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    fileContent[i].IndexOf(className, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    classLine = i;
                    break;
                }
            }

            // guess the method line
            for (int i = classLine; i < fileContent.Length; i++)
            {
                var line = fileContent[i];
                if (line.IndexOf("function", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var idx = line.IndexOf(methodName, StringComparison.InvariantCultureIgnoreCase);
                if (idx < 0)
                {
                    continue;
             
                }

                if (idx == 0 || !char.IsLetterOrDigit(line[idx - 1]))
                {
                    var end = idx + methodName.Length;
                    if (end >= line.Length || !char.IsLetterOrDigit(line[end]))
                    {
                        return i + 1;
                    }
                }
            }

            return 1;
        }
    }
}
