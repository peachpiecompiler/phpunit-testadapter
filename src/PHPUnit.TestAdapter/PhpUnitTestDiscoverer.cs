using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PHPUnit.TestAdapter
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
                catch (System.Exception e)
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

                // The file might not exist as PHPUnit may mistakenly select classes and methods in its own PHAR,
                // namely PHPUnit\Framework\WarningTestCase::Warning (happened with PHPUnit 7.5.9)
                var unit =
                    File.Exists(filePath)
                    ? CodeSourceUnit.ParseCode(File.ReadAllText(filePath), filePath)
                    : null;

                foreach (var testCaseMethodEl in testCaseClassEl.Descendants("testCaseMethod"))
                {
                    var methodName = testCaseMethodEl.Attribute("name").Value;

                    var testName = PhpUnitHelper.GetTestNameFromPhp(classInfo, methodName);
                    var testCase = new TestCase(testName, PhpUnitTestExecutor.ExecutorUri, source);

                    if (unit != null)
                    {
                        testCase.DisplayName = $"{classInfo.Name}::{methodName}";
                        testCase.CodeFilePath = filePath;
                        testCase.LineNumber = GetLineNumber(unit, phpClassName, methodName);
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

        #region MethodVisitor

        class MethodVisitor : TreeVisitor
        {
            public QualifiedName ClassName { get; private set; }

            public Name MethodName { get; private set; }

            public MethodDecl Method { get; private set; }

            public MethodVisitor(string phpClassName, string methodName)
            {
                ClassName = QualifiedName.Parse(phpClassName, true);
                MethodName = new Name(methodName);
            }

            public override void VisitMethodDecl(MethodDecl x)
            {
                if (x.Name == MethodName && x.ContainingType.QualifiedName == ClassName)
                {
                    Method = x;
                }
            }

            public override void VisitFunctionDecl(FunctionDecl x)
            {
                // nope
            }
        }

        #endregion

        private static int GetLineNumber(SourceUnit unit, string className, string methodName)
        {
            if (unit?.Ast != null)
            {
                var visitor = new MethodVisitor(className, methodName);
                visitor.VisitGlobalCode(unit.Ast);

                if (visitor.Method != null)
                {
                    return unit.GetLineFromPosition(visitor.Method.HeadingSpan.Start) + 1;
                }
            }

            //
            return 1;
        }
    }
}
