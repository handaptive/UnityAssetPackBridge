using UnityEngine;
using UnityEditor;

namespace AssetPack.Bridge.Editor
{
  public class AssetPackWindow : EditorWindow
  {
    public const string WindowName = "Asset Pack";

    private LoopbackServer _loopback = new();

    [MenuItem("Window/Asset Pack")]
    public static void ShowWindow()
    {
      GetWindow<AssetPackWindow>(WindowName);
    }

    private void OnEnable()
    {
      titleContent = new GUIContent(WindowName);
      _loopback.Start();
      _loopback.LoggedIn += OnLoggedIn;
    }

    private void OnDisable()
    {
      _loopback.Stop();
      _loopback.LoggedIn -= OnLoggedIn;
    }

    private void OnLoggedIn()
    {
      Utility.Log("User logged in successfully.");
      Repaint();
    }

    void OnGUI()
    {
      GUILayout.Label(WindowName, EditorStyles.boldLabel);
      GUILayout.Space(10);

      if (GUILayout.Button("Restart server"))
      {
        Utility.Log("Restarting loopback server...");
        _loopback.Stop();
        _loopback.Start();
        Repaint();
      }

      if (Utility.IsAuthorized())
      {
        AuthGUI();
      }
      else
      {
        NoAuthGUI();
      }
    }

    void NoAuthGUI()
    {
      GUILayout.Label("You are not logged in.", EditorStyles.boldLabel);
      GUILayout.Space(10);

      if (GUILayout.Button("Login"))
      {
        Utility.Log("Starting login process...");
        Application.OpenURL($"{Utility.GetWebsiteUrl()}/login?callback={_loopback.CallbackUrlBase64}");
      }

      GUILayout.Space(10);
      GUILayout.Label("After logging in, return to this window to see your status.", EditorStyles.wordWrappedLabel);
    }

    void AuthGUI()
    {
      GUILayout.Label("You are logged in.", EditorStyles.boldLabel);
      GUILayout.Space(10);

      if (GUILayout.Button("Logout"))
      {
        Utility.Log("Logging out...");
        Utility.ClearTokens();
        NoAuthGUI();
      }
    }
  }
}