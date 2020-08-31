using Peachpie.Library.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Peachpied.PhpUnit.TestAdapter
{
    internal static class EnvironmentHelper
    {
        public static string ProjectDirectory { get; } = TryFindProjectDirectory(Environment.CurrentDirectory);

        public static string TryFindProjectDirectory(string assemblyDir)
        {
            // TODO: Obtain the information about the project root more reliably

            // project/path/bin/Debug/netcoreapp3.0 -> project/path
            var tokens = assemblyDir.Split(Path.DirectorySeparatorChar);
            if (tokens.Length >= 3
                && tokens[tokens.Length - 3] == "bin"
                && (tokens[tokens.Length - 2] == "Debug" || tokens[tokens.Length - 2] == "Release")
                && tokens[tokens.Length - 1].StartsWith("net"))
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), tokens.Take(tokens.Length - 3));
            }
            else
            {
                return assemblyDir;
            }
        }
    }
}
