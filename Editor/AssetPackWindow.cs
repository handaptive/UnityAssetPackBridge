using UnityEngine;
using UnityEditor;

namespace AssetPack.Bridge.Editor
{
  public class AssetPackWindow : EditorWindow
  {
    public const string WindowName = "Asset Pack";
    private string _errorMessage = string.Empty;

    private CallbackController _callback = new();

    [MenuItem("Window/Asset Pack")]
    public static void ShowWindow()
    {
      GetWindow<AssetPackWindow>(WindowName);
    }

    private void OnEnable()
    {
      _callback.Finished += OnCallbackFinished;
      _callback.Errored += OnCallbackErrored;
    }

    private void OnDisable()
    {
      _callback.Finished -= OnCallbackFinished;
      _callback.Errored -= OnCallbackErrored;
    }

    private void ClearError()
    {
      _errorMessage = string.Empty;
    }

    private void OnCallbackFinished()
    {
      Utility.Log("User logged in successfully.");
      ClearError();
      Repaint();
    }

    private void OnCallbackErrored(string error)
    {
      Utility.LogError($"Login failed: {error}");
      _errorMessage = error;
      Repaint();
    }

    void OnGUI()
    {
      GUILayout.Label(WindowName, EditorStyles.boldLabel);
      GUILayout.Space(10);

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

      if (!string.IsNullOrEmpty(_errorMessage))
      {
        GUILayout.Label($"Error: {_errorMessage}", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
      }

      if (GUILayout.Button("Login"))
      {
        Utility.Log("Starting login process...");
        _callback.Start();
        ClearError();
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