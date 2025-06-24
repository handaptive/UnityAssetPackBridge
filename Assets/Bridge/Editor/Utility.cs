using UnityEngine;
using UnityEditor;
using System.IO;

namespace AssetPack.Bridge.Editor
{
  public static class Utility
  {
    private static Config GetConfig()
    {
      var assetsFolderPath = Application.dataPath;
      var configFilePath = Path.Combine(assetsFolderPath, "config.jsonc");
      if (!File.Exists(configFilePath))
      {
        return null;
      }

      var jsonContent = File.ReadAllText(configFilePath);
      return JsonUtility.FromJson<Config>(jsonContent);
    }

    public static string GetWebsiteUrl()
    {
      return GetConfig()?.websiteUrl ?? "https://assetpack.ai";
    }

    [System.Serializable]
    private class Config
    {
      public string websiteUrl;
    }
  }
}
