using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace McpUnity.Utils
{
    public static class AssetUtils
    {
        /// <summary>
        /// Ensures the asset path starts with "Assets/".
        /// </summary>
        public static string EnsureAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) && path != "Assets")
            {
                return "Assets/" + path.TrimStart('/');
            }
            return path;
        }

        /// <summary>
        /// Checks if an asset (file or folder) exists at the given path.
        /// </summary>
        public static bool CheckAssetExists(string path, bool autoEnsurePath = false)
        {
            if (autoEnsurePath)
            {
                path = EnsureAssetPath(path);
            }

            // Check if it's a known asset GUID.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                return true;
            }

            // Fallback check for newly created folders not yet refreshed.
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            {
                return AssetDatabase.IsValidFolder(path);
            }

            // Check file existence for non-folder assets.
            return File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        /// <summary>
        /// Ensures the directory for a given asset path exists, creating it if necessary.
        /// </summary>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
                AssetDatabase.Refresh();
            }
        }
    }
}
