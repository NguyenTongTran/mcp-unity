using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using McpUnity.Tools;
using McpUnity.Unity;
using McpUnity.Utils;

namespace McpUnity.Tools
{
    public class AddAssetTool : McpToolBase
    {
        public AddAssetTool()
        {
            Name = "add_asset_to_project";
            Description = "Imports assets (images, etc.) into the Unity project";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            JToken sourcePathsToken = parameters["sourcePaths"];
            string destPath = parameters["destPath"]?.ToObject<string>();

            if (sourcePathsToken == null || string.IsNullOrEmpty(destPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameters 'sourcePaths' and 'destPath' must be provided",
                    "validation_error"
                );
            }

            List<string> sourcePaths;
            if (sourcePathsToken.Type == JTokenType.Array)
            {
                sourcePaths = sourcePathsToken.ToObject<List<string>>();
            }
            else
            {
                sourcePaths = new List<string> { sourcePathsToken.ToObject<string>() };
            }

            if (!sourcePaths.Any())
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "At least one source path must be provided",
                    "validation_error"
                );
            }

            destPath = AssetUtils.EnsureAssetPath(destPath);

            int results = 0;
            Dictionary<string, string> errors = new Dictionary<string, string>();

            foreach (string sourcePath in sourcePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    if (Directory.Exists(sourcePath))
                    {
                        errors.Add(sourcePath, "Path must be a file, not a directory");
                    } else
                    {
                        errors.Add(sourcePath, "Source file not found");
                    }
                    continue;
                }

                string fileName = Path.GetFileName(sourcePath);
                string finalDestPath = Path.Combine(destPath, fileName);
                string normalizedDestPath = AssetUtils.EnsureAssetPath(finalDestPath);

                if (AssetUtils.CheckAssetExists(normalizedDestPath))
                {
                    errors.Add(finalDestPath, "Asset already exists at destination");
                    continue;
                }

                try
                {
                    AssetUtils.EnsureDirectoryExists(Path.GetDirectoryName(normalizedDestPath));

                    File.Copy(sourcePath, normalizedDestPath, false);

                    AssetDatabase.ImportAsset(normalizedDestPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    ++results;

                    AssetDatabase.Refresh();
                }
                catch (System.Exception ex)
                {
                    if (File.Exists(normalizedDestPath))
                    {
                        File.Delete(normalizedDestPath);
                    }
                    errors.Add(sourcePath, ex.Message);
                }
            }

            return new JObject
            {
                ["success"] = !errors.Any(),
                ["type"] = "text",
                ["message"] = $"Imported assets: {results} successfully, {errors.Count} failed",
                ["errors"] = errors.Count > 0 ? JObject.FromObject(errors) : null
            };
        }
    }
}
