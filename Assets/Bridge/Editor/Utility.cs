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

    private static string GetIdtokenExpirationKey()
    {
      return $"assetpack_id_token_expiration_{GetFlavor()}";
    }

    public static void SetIdTokenExpiration(int expiration)
    {
      EditorPrefs.SetInt(GetIdtokenExpirationKey(), expiration);
      Log("ID token expiration set: " + expiration);
    }

    public static int GetIdTokenExpiration()
    {
      return EditorPrefs.GetInt(GetIdtokenExpirationKey(), 0);
    }

    public static void ClearTokens()
    {
      EditorPrefs.DeleteKey(GetIdTokenKey());
      EditorPrefs.DeleteKey(GetRefreshTokenKey());
      EditorPrefs.DeleteKey(GetIdtokenExpirationKey());
    }

    public static int NowSeconds()
    {
      return (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static bool IsLoggedIn()
    {
      var refreshToken = GetRefreshToken();
      return !string.IsNullOrEmpty(refreshToken);
    }

    public static bool IsIdTokenValid()
    {
      var expiration = GetIdTokenExpiration();
      Log($"Checking ID token expiration: {expiration} (now: {NowSeconds()})");
      return expiration > NowSeconds();
    }

    public static void MarkIdTokenExpiration()
    {
      SetIdTokenExpiration(NowSeconds() + 60 * 50);
      Log("ID token expiration marked for 50 minutes from now.");
    }

    private static string FormatName(string name)
    {
      return System.Text.RegularExpressions.Regex.Replace(name.ToLower(), @"[^a-z0-9]", "_");
    }

    public static string GetFolder(string folderName)
    {
      var assetsFolderPath = Application.dataPath;
      var targetFolderPath = Path.Combine(assetsFolderPath, folderName);

      if (!Directory.Exists(targetFolderPath))
      {
        Directory.CreateDirectory(targetFolderPath);
        Log($"Created folder: {targetFolderPath}");
      }

      return targetFolderPath;
    }

    public static string GetPacksFolder()
    {
      return GetFolder(Path.Combine(Application.dataPath, "AssetPacks"));
    }

    public static string GetPackFolder(string packName)
    {
      return GetFolder(Path.Combine(GetPacksFolder(), FormatName(packName)));
    }

    public static string GetModelFolder(string packName, string modelName)
    {
      return GetFolder(Path.Combine(GetPackFolder(packName), FormatName(modelName)));
    }

    public static string GetModelFilePath(string packName, string modelName, string fileName)
    {
      return Path.Combine(GetModelFolder(packName, modelName), fileName);
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
