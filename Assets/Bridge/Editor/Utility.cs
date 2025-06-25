using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

namespace AssetPack.Bridge.Editor
{
  public static class Utility
  {
    private static Config GetConfig()
    {
      var assetsFolderPath = Application.dataPath;
      var configFilePath = Path.Combine(assetsFolderPath, "config.json");
      if (!File.Exists(configFilePath))
      {
        return null;
      }

      var jsonContent = File.ReadAllText(configFilePath);
      return JsonUtility.FromJson<Config>(jsonContent);
    }

    public static IEnumerator WaitForSeconds(float seconds)
    {
      float startTime = Time.realtimeSinceStartup;
      while (Time.realtimeSinceStartup - startTime < seconds)
      {
        yield return null;
      }
    }

    public static string GetWebsiteUrl()
    {
      return GetConfig()?.websiteUrl ?? "https://assetpack.ai";
    }

    public static string GetBridgeEndpoint()
    {
      return GetConfig()?.bridgeEndpoint ?? "https://us-central1-assetpack-prod.cloudfunctions.net/bridge";
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

    public static int Now()
    {
      return (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static bool IsAuthorized()
    {
      var refreshToken = GetRefreshToken();
      return !string.IsNullOrEmpty(refreshToken);
    }

    [System.Serializable]
    private class Config
    {
      public string websiteUrl;
      public bool debugMode;
      public string flavor;
      public string bridgeEndpoint;
    }
  }
}
