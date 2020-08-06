using System;
using System.Collections.Generic;
using System.Linq;
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
            var ctx = new PhpUnitContext(sources);
            foreach (var test in ctx.FindTestCases())
            {
                discoverySink.SendTestCase(test);
            }
        }
    }
}
