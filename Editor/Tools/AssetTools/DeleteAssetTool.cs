using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using McpUnity.Tools;
using McpUnity.Unity;
using McpUnity.Utils;

namespace McpUnity.Tools
{
    public class DeleteAssetTool : McpToolBase
    {
        public DeleteAssetTool()
        {
            Name = "delete_asset";
            Description = "Deletes an asset (file or folder) from the project";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            string path = parameters["path"]?.ToObject<string>();

            if (string.IsNullOrEmpty(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'path' must be provided",
                    "validation_error"
                );
            }

            path = AssetUtils.EnsureAssetPath(path);

            if (!AssetUtils.CheckAssetExists(path))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Asset not found at path: {path}",
                    "asset_not_found"
                );
            }

            McpLogger.LogInfo($"Deleting asset at {path}");

            try
            {
                bool success = AssetDatabase.DeleteAsset(path);
                
                if (success)
                {
                    AssetDatabase.Refresh();
                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Successfully deleted asset at {path}"
                    };
                }
                else
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Failed to delete asset at '{path}'. Check logs or if the file is locked.",
                        "delete_failed"
                    );
                }
            }
            catch (System.Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Exception deleting asset: {ex.Message}",
                    "asset_operation_error"
                );
            }
        }
    }
}
