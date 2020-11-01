using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// Implementation of the <see cref="ITestExecutor"/> interface used to run tests.
    /// </summary>
    [ExtensionUri(PhpUnitTestExecutor.ExecutorUriString)]
    public sealed class PhpUnitTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://PhpUnitTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        /// <summary>
        /// Run all the tests in the assemblies, used e.g. from <c>dotnet test</c>.
        /// </summary>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var source in sources)
            {
                RunSourceTest(source, frameworkHandle);
            }
        }

        /// <summary>
        /// Run selected tests, used e.g. from Test Explorer in Microsoft Visual Studio.
        /// </summary>
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
                var args = Array.Empty<string>();

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

                // Inject our custom extension to report the test results
                RunPhpUnitWithExtension(projectDir, source, args,
                    ctx =>
                    {
                        // Enable Peachpie to create an instance of our custom extension
                        ctx.DeclareType<TestReporterExtension>();

                        // Pass data to the extension via Context
                        var testRunCtx = new TestRunContext(source, frameworkHandle);
                        ctx.SetProperty(testRunCtx);
                    });
            }
            catch (System.Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, e.Message + "\n" + e.StackTrace);
            }
        }

        private void RunPhpUnitWithExtension(string projectDir, string source, string[] args, Action<Context> initCallback)
        {
            if (PhpUnitHelper.Version >= new Version(9, 1))
            {
                // The --extensions CLI argument is available from PHPUnit 9.1 (although it's documented only from 9.3)
                args = new[] { "--extensions", TestReporterExtension.PhpName }.Concat(args).ToArray();
                PhpUnitHelper.Launch(projectDir, source, args, initCallback);
            }
            else
            {
                // Older PHPUnit versions can receive extensions only in the configuration file,
                // so we need to create a modified version of the existing one and pass it to PHPUnit.
                // (or create one from scratch it if it doesn't exist at all)

                // Create PHPUnit configuration with the extension added
                string origConfigFile = PhpUnitHelper.TryFindConfigFile(projectDir);
                var configXml = (origConfigFile != null) ? XElement.Load(origConfigFile) : new XElement("phpunit");
                var extensionsEl = configXml.GetOrCreateElement("extensions");
                extensionsEl.Add(new XElement("extension", new XAttribute("class", TestReporterExtension.PhpName)));

                // Store the configuration in a temporary file to pass it to PHPUnit
                string tempConfigFile = null;
                try
                {
                    // The configuration file must be in the same folder as the project in order to work (to load PHP classes properly etc.)
                    tempConfigFile = EnvironmentHelper.GetNonexistingFilePath(projectDir, "phpunit.xml.");

                    // Save the config file without the BOM
                    using (var xmlWriter = new XmlTextWriter(tempConfigFile, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                    {
                        configXml.Save(xmlWriter);
                    }

                    args = new[] { "--configuration", tempConfigFile }.Concat(args).ToArray();
                    PhpUnitHelper.Launch(projectDir, source, args, initCallback);
                }
                finally
                {
                    if (File.Exists(tempConfigFile))
                    {
                        File.Delete(tempConfigFile);
                    }
                }
            }
        }

        /// <summary>
        /// Cancel test execution, currently ignored.
        /// </summary>
        public void Cancel()
        {
            // TODO
        }
    }
}
