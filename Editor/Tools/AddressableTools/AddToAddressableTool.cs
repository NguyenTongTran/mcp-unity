using UnityEditor;
using Newtonsoft.Json.Linq;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.Linq;

namespace McpUnity.Tools
{
    public class AddToAddressableTool : McpToolBase
    {
        public AddToAddressableTool()
        {
            Name = "add_to_addressable";
            Description = "Adds multiple assets to the Addressable system";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            var assets = parameters["assets"]?.ToObject<Dictionary<string, string>>();
            string groupName = parameters["groupName"]?.ToObject<string>();

            if (assets == null || !assets.Any())
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'assets' must be provided as a non-empty object with address:path pairs",
                    "validation_error"
                );
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Addressable Asset Settings not found. Please initialize Addressables first.",
                    "addressable_not_initialized"
                );
            }

            var group = string.IsNullOrEmpty(groupName) ? settings.DefaultGroup : settings.FindGroup(groupName);
            if (group == null && !string.IsNullOrEmpty(groupName))
            {
                group = settings.CreateGroup(groupName, false, false, true, null);
            }

            var results = new List<string>();
            var errors = new List<string>();

            foreach (var asset in assets)
            {
                string address = asset.Key;
                string path = AssetUtils.EnsureAssetPath(asset.Value);

                if (!AssetUtils.CheckAssetExists(path))
                {
                    errors.Add($"Asset not found at path: {path}");
                    continue;
                }

                try
                {
                    McpLogger.LogInfo($"Adding asset at {path} to Addressables with address {address}");
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    var entry = settings.CreateOrMoveEntry(guid, group);
                    entry.address = address;
                    results.Add($"Successfully added asset at {path} with address: {address}");
                }
                catch (System.Exception ex)
                {
                    errors.Add($"Failed to add asset {path}: {ex.Message}");
                }
            }

            if (results.Any())
            {
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, null, true);
                AssetDatabase.SaveAssets();
            }

            return new JObject
            {
                ["success"] = !errors.Any(),
                ["type"] = "text",
                ["message"] = $"Added assets to Addressables: {results.Count} successfully, {errors.Count} failed",
                ["errors"] = errors.Any() ? JArray.FromObject(errors) : null
            };
        }
    }
}
