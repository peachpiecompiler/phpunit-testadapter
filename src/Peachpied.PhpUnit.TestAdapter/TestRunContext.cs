using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpied.PhpUnit.TestAdapter
{
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
