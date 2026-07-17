using System;
using System.IO;

namespace Stratton.Core
{
    public static class PathUtils
	{
		private static readonly string _directorySeparator = Path.DirectorySeparatorChar.ToString();

		public static string MakeProjectRelative(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;
			var fullPath = Path.GetFullPath(Environment.CurrentDirectory).Replace("\\", "/");
			path = Path.GetFullPath(path).Replace("\\", "/");

			if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.DirectorySeparatorChar)
				path = path.Substring(0, path.Length - 1);
			if (fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar || fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar)
				fullPath = fullPath.Substring(0, fullPath.Length - 1);

			if (path == fullPath)
				path = "." + _directorySeparator;
			else if (path.StartsWith(fullPath, StringComparison.Ordinal))
				path = path.Substring(fullPath.Length + 1).Replace("/", _directorySeparator);
			else
				path = null;

			return path != null ? Normalize(path) : null;
		}

		public static string MakeProjectAbsolute(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));

			return Normalize(Path.GetFullPath(path));
		}

		public static string Normalize(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));

			return path.Replace("\\", _directorySeparator).Replace("/", _directorySeparator);
		}

		public static string NormalizeForUrl(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));

			return path.Replace("\\", "/");
		}

		public static string CombineUrl(string baseUrl, string partUrl)
		{
			if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));
			if (partUrl == null) throw new ArgumentNullException(nameof(partUrl));

			if (baseUrl.EndsWith("/"))
            {
				return baseUrl + partUrl;
            }
			else
            {
				return baseUrl + "/" + partUrl;
			}
		}
	}
}