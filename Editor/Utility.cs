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

    public static string GetApiUrl()
    {
      return GetConfig()?.apiUrl ?? "https://api.assetpack.ai";
    }

    public static bool IsDebugMode()
    {
      return GetConfig()?.debugMode ?? false;
    }

    public static void Log(string message)
    {
      if (IsDebugMode())
      {
        Debug.Log(message);
      }
    }

    public static void LogWarning(string message)
    {
      if (IsDebugMode())
      {
        Debug.LogWarning(message);
      }
    }

    public static void LogError(string message)
    {
      if (IsDebugMode())
      {
        Debug.LogError(message);
      }
    }

    public static string GetFlavor()
    {
      return GetConfig()?.flavor ?? "prod";
    }

    private static string GetIdTokenKey()
    {
      return $"assetpack_id_token_{GetFlavor()}";
    }

    public static void SetIdToken(string token)
    {
      EditorPrefs.SetString(GetIdTokenKey(), token);
      Log("ID token set: " + token);
    }

    public static string GetIdToken()
    {
      return EditorPrefs.GetString(GetIdTokenKey(), string.Empty);
    }

    private static string GetRefreshTokenKey()
    {
      return $"assetpack_refresh_token_{GetFlavor()}";
    }

    public static void SetRefreshToken(string token)
    {
      EditorPrefs.SetString(GetRefreshTokenKey(), token);
      Log("Refresh token set: " + token);
    }

    public static string GetRefreshToken()
    {
      return EditorPrefs.GetString(GetRefreshTokenKey(), string.Empty);
    }

    public static void ClearTokens()
    {
      EditorPrefs.DeleteKey(GetIdTokenKey());
      EditorPrefs.DeleteKey(GetRefreshTokenKey());
    }

    public static bool IsAuthorized()
    {
      return false;
    }

    [System.Serializable]
    private class Config
    {
      public string websiteUrl;
      public bool debugMode;
      public string flavor;
      public string apiUrl;
    }
  }
}
