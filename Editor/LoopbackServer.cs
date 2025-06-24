using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetPack.Bridge.Editor
{
  public class LoopbackServer
  {
    private HttpListener listener = null;

    public event System.Action LoggedIn = null;

    public bool IsRunning
    {
      get { return listener != null; }
    }

    public string CallbackUrl { get; private set; }

    public string CallbackUrlBase64
    {
      get
      {
        if (string.IsNullOrEmpty(CallbackUrl))
        {
          return null;
        }
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(CallbackUrl));
      }
    }

    private string GetAvailablePort()
    {
      return "5005";
    }

    public void Start()
    {
      if (listener != null)
      {
        Utility.LogWarning("Loopback server is already running.");
        return;
      }

      CallbackUrl = $"http://{IPAddress.Loopback}:{GetAvailablePort()}/callback";
      listener = new HttpListener();
      listener.Prefixes.Add(CallbackUrl + "/");
      listener.Start();

      Utility.Log("Loopback server started...");

      Task.Run(() =>
      {
        while (listener.IsListening)
        {
          Utility.Log("Waiting for connection...");
          var context = listener.GetContext();
          var response = context.Response;

          string idToken = context.Request.QueryString["id_token"];
          string refreshToken = context.Request.QueryString["refresh_token"];
          Utility.Log("Received idToken: " + idToken);
          Utility.Log("Received refreshToken: " + refreshToken);

          Utility.SetIdToken(idToken);
          Utility.SetRefreshToken(refreshToken);

          response.Redirect($"{Utility.GetWebsiteUrl()}/connected");
          response.Close();

          LoggedIn?.Invoke();
        }
      });
    }

    [System.Serializable]
    private class TokenData
    {
      public string idToken;
      public string refreshToken;
    }

    public void Stop()
    {
      if (listener == null)
      {
        return;
      }

      CallbackUrl = null;
      listener.Stop();
      listener.Close();
      listener = null;
    }
  }
}
