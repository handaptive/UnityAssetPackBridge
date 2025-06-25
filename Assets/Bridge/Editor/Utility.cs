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

    private static RefreshTokenPayload GetRefreshTokenPayload()
    {
      var base64Token = GetRefreshToken();
      if (string.IsNullOrEmpty(base64Token))
      {
        return null;
      }

      var tokenParts = base64Token.Split('.');
      if (tokenParts.Length < 2)
      {
        LogError("Invalid refresh token format.");
        return null;
      }

      var payloadPart = tokenParts[1];
      if (string.IsNullOrEmpty(payloadPart))
      {
        LogError("Refresh token payload is empty.");
        return null;
      }

      // Replace URL-safe characters with standard Base64 characters
      string base64 = payloadPart.Replace('-', '+').Replace('_', '/');
      switch (base64.Length % 4)
      {
        case 2: base64 += "=="; break;
        case 3: base64 += "="; break;
      }

      try
      {
        var payloadJson = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));
        return JsonUtility.FromJson<RefreshTokenPayload>(payloadJson);
      }
      catch (System.Exception ex)
      {
        LogError($"Error parsing refresh token: {ex.Message}");
        return null;
      }
    }

    public static bool IsAuthorized()
    {
      var payload = GetRefreshTokenPayload();
      if (payload == null)
      {
        ClearTokens();
        return false;
      }

      var refreshExp = payload?.exp ?? 0;
      var now = Now();
      if (refreshExp < now)
      {
        ClearTokens();
        return false;
      }

      return true;
    }

    [System.Serializable]
    private class RefreshTokenPayload
    {
      public int exp;
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
