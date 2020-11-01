using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// Class to store information about the current test run to be used from <see cref="TestReporterExtension"/>.
    /// </summary>
    internal class TestRunContext
    {
        public string Source { get; }

        public IFrameworkHandle FrameworkHandle { get; }

        public TestRunContext(string source, IFrameworkHandle frameworkHandle)
        {
            this.Source = source;
            this.FrameworkHandle = frameworkHandle;
        }
    }
}
