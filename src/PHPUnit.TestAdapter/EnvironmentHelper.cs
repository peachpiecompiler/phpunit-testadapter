using Peachpie.Library.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PHPUnit.TestAdapter
{
    /// <summary>
    /// Helper to obtain the information about the environment where the tests are being run.
    /// </summary>
    internal static class EnvironmentHelper
    {
        /// <summary>
        /// Try to deduce the project directory from the location of the compiled assembly.
        /// Returns <paramref name="assemblyDir"/> if it fails.
        /// </summary>
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

        /// <summary>
        /// Generate a file name in the given directory with the given prefix which
        /// does not exists in it yet.
        /// </summary>
        public static string GetNonexistingFilePath(string dir, string prefix = "")
        {
            string filePath;
            do
            {
                filePath = Path.Combine(dir, prefix + Path.GetRandomFileName());
            } while (File.Exists(filePath));

            return filePath;
        }
    }
}
