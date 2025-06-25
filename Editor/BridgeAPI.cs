using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetPack.Bridge.Editor
{
  [System.Serializable]
  public class AuthRefreshTokenInput
  {
    public string refreshToken;
  }

  [System.Serializable]
  public class AuthRefreshTokenOutput
  {
    public string idToken;
    public string refreshToken;
  }

  [System.Serializable]
  public class CallbackCreateOutput
  {
    public string id;
  }

  [System.Serializable]
  public class CallbackPollInput
  {
    public string id;
  }

  [System.Serializable]
  public class CallbackPollOutput
  {
    public string status;
    public string error;
    public string idToken;
    public string refreshToken;
  }

  [System.Serializable]
  public class PackDownloadInput
  {
    public string packId;
  }

  [System.Serializable]
  public class PackDownloadOutput
  {
    [System.Serializable]
    public class Model
    {
      public string name;
      public string fbxUrl;
      public string diffuseUrl;
    }

    public Model[] models;
  }

  public struct RequestArgs<O>
  {
    public System.Action<O> onSuccess;
    public System.Action<string> onError;
  }

  public static class BridgeAPI
  {
    private static UnityWebRequest BuildRequest(string name, string data, string token = null)
    {
      var wrappedData = "{\"data\": {\"name\": \"" + name + "\", \"args\": " + data + "}}";

      UnityWebRequest request = new(Utility.GetBridgeEndpoint(), "POST");
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(wrappedData);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");

      if (token != null)
      {
        request.SetRequestHeader("Authorization", "Bearer " + token);
      }

      return request;
    }

    private static IEnumerator SendRequest<O>(string name, RequestArgs<O> args, string data, string token = null)
    {
      var request = BuildRequest(name, data, token);
      Utility.Log($"Sending request to {request.url} with data: {data}");
      yield return request.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
      if (request.result != UnityWebRequest.Result.Success)
#else
      if (request.isNetworkError || request.isHttpError)
#endif
      {
        Utility.LogError($"Request failed: {request.error}");
        args.onError?.Invoke(request.error);
      }
      else
      {
        string json = request.downloadHandler.text;
        int startIndex = json.IndexOf('{', json.IndexOf("result"));
        int endIndex = json.LastIndexOf('}');

        string innerJson = json[startIndex..endIndex];
        Utility.Log($"Received response: {innerJson}");
        O output = JsonUtility.FromJson<O>(innerJson);
        args.onSuccess?.Invoke(output);
      }
    }

    public static IEnumerator CreateCallback(RequestArgs<CallbackCreateOutput> args)
    {
      yield return SendRequest("callback/create", args, "{}");
    }

    public static IEnumerator RefreshToken(RequestArgs<AuthRefreshTokenOutput> args, AuthRefreshTokenInput input)
    {
      string data = JsonUtility.ToJson(input);
      yield return SendRequest("auth/refreshToken", args, data);
    }

    public static IEnumerator PollCallback(RequestArgs<CallbackPollOutput> args, CallbackPollInput input)
    {
      string data = JsonUtility.ToJson(input);
      yield return SendRequest("callback/poll", args, data);
    }

    private static IEnumerator SendAuthorizedRequest<O>(string name, RequestArgs<O> args, string data)
    {
      if (!Utility.IsLoggedIn())
      {
        Utility.LogError("User is not logged in. Cannot send authorized request.");
        args.onError?.Invoke("User is not logged in.");
        yield break;
      }

      if (!Utility.IsIdTokenValid())
      {
        bool errored = false;
        Utility.Log("ID token is expired. Refreshing token...");
        yield return RefreshToken(new RequestArgs<AuthRefreshTokenOutput>
        {
          onSuccess = (output) =>
          {
            Utility.SetIdToken(output.idToken);
            Utility.SetRefreshToken(output.refreshToken);
            Utility.MarkIdTokenExpiration();
          },
          onError = (error) =>
          {
            Utility.LogError($"Failed to refresh token: {error}");
            args.onError?.Invoke(error);
            errored = true;
          }
        }, new AuthRefreshTokenInput { refreshToken = Utility.GetRefreshToken() });

        if (errored)
        {
          yield break;
        }
      }

      yield return SendRequest(name, args, data, Utility.GetIdToken());
    }

    public static IEnumerator GetDownloadablePack(PackDownloadInput input, RequestArgs<PackDownloadOutput> args)
    {
      yield return SendAuthorizedRequest("pack/download", args, JsonUtility.ToJson(input));
    }

    public static IEnumerator DownloadFile(string path, string downloadUrl)
    {
      Utility.Log($"Downloading file from {downloadUrl} to {path}");
      using UnityWebRequest request = UnityWebRequest.Get(downloadUrl);
      request.downloadHandler = new DownloadHandlerFile(path);

      yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
      if (request.result != UnityWebRequest.Result.Success)
#else
      if (request.isNetworkError || request.isHttpError)
#endif
      {
        Utility.Log($"Failed to download file: {request.error}");
      }
      else
      {
        Utility.Log($"File downloaded to: {path}");
      }
    }
  }
}
