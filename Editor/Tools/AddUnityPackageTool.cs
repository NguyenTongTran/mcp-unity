using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    /// <summary>
    /// Placeholder tool for adding Unity packages (WIP)
    /// </summary>
    public class AddUnityPackageTool : McpToolBase
    {
        public AddUnityPackageTool()
        {
            Name = "add_unity_package";
            Description = "Add custom Unity package (.unitypackage) to the project";
            IsAsync = false;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            tcs.SetResult(new JObject
            {
                ["success"] = false,
                ["type"] = "text",
                ["message"] = "AddUnityPackageTool is a placeholder and not yet implemented."
            });
        }

        public override JObject Execute(JObject parameters)
        {
            // Extract the package path from parameters
            string packagePath = parameters["packagePath"]?.ToObject<string>();
            if (string.IsNullOrEmpty(packagePath))
            {
                // Return error if required parameter is missing
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'packagePath' not provided", 
                    "validation_error"
                );
            }

            try
            {
                // Import the .unitypackage file non-interactively
                var before = new HashSet<string>(AssetDatabase.GetAllAssetPaths());

                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.Refresh();

                var after = new HashSet<string>(AssetDatabase.GetAllAssetPaths());
                var assets = after.Except(before).ToArray();
                
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Successfully imported package: {packagePath}",
                    ["assets"] = new JArray(assets)
                };
            }
            catch (System.Exception ex)
            {
                // Return error if import fails
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to import package: {ex.Message}",
                    "import_error"
                );
            }
        }
    }
}
