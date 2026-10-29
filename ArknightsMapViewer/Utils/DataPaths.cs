using System;
using System.Collections.Generic;
using System.IO;

namespace ArknightsMapViewer
{
    internal static class DataPaths
    {
        public static string DefaultConfigDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string UserConfigDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "Config");
        public static string DefaultGameDataDirectory => Path.Combine(DefaultConfigDirectory, "gamedata");
        public static string UserGameDataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "gamedata");

        public static string GetConfigReadPath(string relativePath)
        {
            string userPath = GetUserConfigPath(relativePath);
            return File.Exists(userPath) ? userPath : ResolveRelativePath(DefaultConfigDirectory, relativePath);
        }

        public static string GetUserConfigPath(string relativePath)
        {
            return ResolveRelativePath(UserConfigDirectory, relativePath);
        }

        public static string GetGameDataReadPath(string relativePath)
        {
            string userPath = GetUserGameDataPath(relativePath);
            return File.Exists(userPath) ? userPath : ResolveRelativePath(DefaultGameDataDirectory, relativePath);
        }

        public static string GetUserGameDataPath(string relativePath)
        {
            return ResolveRelativePath(UserGameDataDirectory, relativePath);
        }

        public static List<string> GetGameDataUpdateFiles()
        {
            var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            AddExcelFiles(DefaultGameDataDirectory, files);
            AddExcelFiles(UserGameDataDirectory, files);
            files.Add("levels/enemydata/enemy_database.json");
            return new List<string>(files);
        }

        private static void AddExcelFiles(string gameDataDirectory, ISet<string> files)
        {
            string excelDirectory = ResolveRelativePath(gameDataDirectory, "excel");
            if (!Directory.Exists(excelDirectory))
            {
                return;
            }

            string root = Path.GetFullPath(gameDataDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string[] paths = Directory.GetFiles(excelDirectory, "*.json", SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);
            foreach (string path in paths)
            {
                if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path.Substring(root.Length).Replace('\\', '/'));
                }
            }
        }

        private static string ResolveRelativePath(string directory, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.IndexOf(':') >= 0)
            {
                throw new ArgumentException("A relative file path is required.", nameof(relativePath));
            }

            string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The relative path must stay inside its data directory.", nameof(relativePath));
            }
            return path;
        }
    }
}
