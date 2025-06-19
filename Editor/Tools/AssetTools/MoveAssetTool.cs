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
    public class MoveAssetTool : McpToolBase
    {
        public MoveAssetTool()
        {
            Name = "move_asset";
            Description = "Moves assets (files or folders) from source locations to a destination in the project";
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
                string normalizedSourcePath = AssetUtils.EnsureAssetPath(sourcePath);
                
                if (!AssetUtils.CheckAssetExists(normalizedSourcePath))
                {
                    errors.Add(normalizedSourcePath, "Source asset not found");
                    continue;
                }

                string finalDestPath = Path.Combine(destPath, Path.GetFileName(normalizedSourcePath));

                if (AssetUtils.CheckAssetExists(finalDestPath))
                {
                    errors.Add(finalDestPath, "Asset already exists at destination");
                    continue;
                }

                try 
                {
                    AssetUtils.EnsureDirectoryExists(Path.GetDirectoryName(finalDestPath));

                    string validationError = AssetDatabase.ValidateMoveAsset(normalizedSourcePath, finalDestPath);
                    if (!string.IsNullOrEmpty(validationError))
                    {
                        errors.Add(normalizedSourcePath, validationError);
                        continue;
                    }

                    string error = AssetDatabase.MoveAsset(normalizedSourcePath, finalDestPath);
                    
                    if (string.IsNullOrEmpty(error))
                    {
                        ++results;
                    }
                    else
                    {
                        errors.Add(normalizedSourcePath, error);
                    }
                }
                catch (System.Exception ex)
                {
                    errors.Add(normalizedSourcePath, ex.Message);
                }
            }

            return new JObject
            {
                ["type"] = "text",
                ["message"] = $"Moved assets: {results} successfully, {errors.Count} failed",
                ["errors"] = errors.Count > 0 ? JObject.FromObject(errors) : null
            };
        }
    }
}
