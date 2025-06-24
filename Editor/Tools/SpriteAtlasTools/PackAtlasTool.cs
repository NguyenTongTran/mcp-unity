using UnityEditor;
using UnityEditor.U2D;
using Newtonsoft.Json.Linq;
using McpUnity.Unity;
using McpUnity.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.U2D;
using UnityEngine;

namespace McpUnity.Tools
{
    public class PackAtlasTool : McpToolBase
    {
        public PackAtlasTool()
        {
            Name = "pack_atlas";
            Description = "Packs sprite atlases - supports packing specific atlases or all atlases";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            var pathsParam = parameters["paths"];
            if (pathsParam == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'paths' must be provided as either a string 'all' or an array of atlas paths",
                    "validation_error"
                );
            }

            var success = 0;
            var errors = new List<string>();

            if (pathsParam.Type == JTokenType.String && pathsParam.Value<string>().ToLower() == "all")
            {
                success = PackAllAtlases(errors);
            }
            else if (pathsParam.Type == JTokenType.Array)
            {
                var paths = pathsParam.ToObject<string[]>();
                if (paths == null || !paths.Any())
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "When providing paths as array, it must be non-empty",
                        "validation_error"
                    );
                }
                success = PackSpecificAtlases(paths, errors);
            }
            else 
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Parameter 'paths' must be either a string 'all' or an array of atlas paths",
                    "validation_error"
                );
            }

            return new JObject
            {
                ["success"] = !errors.Any(),
                ["type"] = "text",
                ["message"] = $"Packed atlases: {success} successfully, {errors.Count} failed",
                ["errors"] = errors.Any() ? JArray.FromObject(errors) : null
            };
        }

        private int PackAllAtlases(List<string> errors)
        {
            try
            {
                McpLogger.LogInfo($"Packing all atlas");
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
                return AssetDatabase.FindAssets("t:SpriteAtlas").Count();
            }
            catch (System.Exception ex)
            {
                errors.Add($"Failed to pack all atlases: {ex.Message}");
            }
            return 0;
        }

        private int PackSpecificAtlases(string[] paths, List<string> errors)
        {
            int success = 0;
            foreach (string path in paths)
            {
                string assetPath = AssetUtils.EnsureAssetPath(path);
                if (!AssetUtils.CheckAssetExists(assetPath))
                {
                    errors.Add($"Atlas not found at path: {assetPath}");
                    continue;
                }

                if (PackAtlas(assetPath, errors))
                {
                    success++;
                }
            }
            return success;
        }

        private bool PackAtlas(string path, List<string> errors)
        {
            try
            {
                McpLogger.LogInfo($"Packing atlas at {path}");
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                {
                    errors.Add($"Failed to load atlas at {path}: Not a valid Sprite Atlas");
                    return false;
                }

                SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
                return true;
            }
            catch (System.Exception ex)
            {
                errors.Add($"Failed to pack atlas {path}: {ex.Message}");
            }
            return false;
        }
    }
}
