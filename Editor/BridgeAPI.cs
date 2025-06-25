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
      if (request.result != UnityWebRequest.Result.Success)
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
  }
}
