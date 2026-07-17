using System;
using System.IO;

namespace Stratton.Core
{
    public static class FileUtils
    {
        public static string GetLocalOrWindowsFile(string executableName, string[] pathsToSearch)
        {
            var fullPath = SearchInWindowsPath(executableName);
            if (fullPath != null)
            {
                return fullPath;
            }
            foreach (var path in pathsToSearch)
            {
                fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return fullPath;
        }

        public static string SearchInWindowsPath(string executableName)
        {
            if (File.Exists(executableName))
                return Path.GetFullPath(executableName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(';'))
            {
                var fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
