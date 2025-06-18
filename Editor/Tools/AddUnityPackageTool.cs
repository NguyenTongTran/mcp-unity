using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpUnity.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    /// <summary>
    /// Placeholder tool for adding Unity packages (WIP)
    /// </summary>
    public class AddUnityPackageTool : McpToolBase
    {
        private class AddPackageOperation
        {
            public string PackageName { get; set; }
            public TaskCompletionSource<JObject> CompletionSource { get; set; }

            private HashSet<string> _beforeAssets;

            public AddPackageOperation(string packagePath, TaskCompletionSource<JObject> tcs)
            {
                PackageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
                CompletionSource = tcs;
                _beforeAssets = new HashSet<string>(AssetDatabase.GetAllAssetPaths());
            }

            public void OnComplete()
            {
                var after = new HashSet<string>(AssetDatabase.GetAllAssetPaths());
                var assets = after.Except(_beforeAssets).ToArray();

                CompletionSource.SetResult(new JObject { 
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Successfully imported package: {PackageName}",
                    ["assets"] = new JArray(assets)
                });
            }
        }

        private readonly List<AddPackageOperation> _activeOperations = new();
        private bool _updateCallbackRegistered = false;

        public AddUnityPackageTool()
        {
            Name = "add_unity_package";
            Description = "Add custom Unity package (.unitypackage) to the project";
            IsAsync = true;
        }


        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            // Extract the package path from parameters
            string packagePath = parameters["packagePath"]?.ToObject<string>();
            if (string.IsNullOrEmpty(packagePath))
            {
                // Return error if required parameter is missing
                tcs.SetResult(
                    McpUnitySocketHandler.CreateErrorResponse(
                        "Required parameter 'packagePath' not provided", 
                        "validation_error"
                    )
                );
                return;
            }

            try
            {
                // Get all assets at the package path
                string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
                var before = new HashSet<string>(guids.Select(guid => 
                    System.IO.Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid))));
                UnityEngine.Debug.Log($"Assets at path before import: {JsonConvert.SerializeObject(before)}");

                lock (_activeOperations)
                {
                    _activeOperations.Add(new AddPackageOperation(packagePath, tcs));
                    if (!_updateCallbackRegistered)
                    {
                        AssetDatabase.importPackageCompleted += OnPackageImported;
                        _updateCallbackRegistered = true;
                    }
                }

                UnityEngine.Debug.Log($"Importing package: {packagePath}");
                AssetDatabase.ImportPackage(packagePath, false);
            }
            catch (Exception ex)
            {
                // Return error if import fails
                tcs.SetResult(
                    McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to import package: {ex.Message}",
                    "import_error"
                    )
                );
            }
        }

        private void OnPackageImported(string packageName)
        {
            UnityEngine.Debug.Log($"OnPackageImported: {packageName}");
            AssetDatabase.Refresh();
            lock (_activeOperations)
            {
                var operation = _activeOperations.FirstOrDefault(o => o.PackageName == packageName);
                if (operation == null) return;

                _activeOperations.Remove(operation);
                operation.OnComplete();

                if (_activeOperations.Count == 0 && _updateCallbackRegistered)
                {
                    AssetDatabase.importPackageCompleted -= OnPackageImported;
                    _updateCallbackRegistered = false;
                }
            }
        }
    }
}
