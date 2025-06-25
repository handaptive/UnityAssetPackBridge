using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using UnityEngine.Networking;

namespace AssetPack.Bridge.Editor
{
  public class AssetPackWindow : EditorWindow
  {
    public const string WindowName = "Asset Pack";
    private string _errorMessage = string.Empty;

    private CallbackController _callback = new();
    private EditorCoroutine _downloadRoutine = null;

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

    private IEnumerator StartDownloadPack()
    {
      Utility.Log("Starting pack download...");

      PackDownloadOutput output = null;
      yield return BridgeAPI.GetDownloadablePack(new RequestArgs<PackDownloadOutput>
      {
        onSuccess = (result) =>
        {
          output = result;
          Utility.Log($"Pack downloaded successfully: {output.models.Length} models found.");
        },
        onError = (error) =>
        {
          Utility.LogError($"Pack download failed: {error}");
          _errorMessage = error;
        }
      });

      if (output == null)
      {
        Utility.LogError("Pack download output is null.");
        yield break;
      }

      for (int i = 0; i < output.models.Length; i++)
      {
        var model = output.models[i];
        Utility.Log($"Model {i + 1}/{output.models.Length}: {model.name} - Download URL: {model.downloadUrl}");
        var path = Utility.GetModelFilePath("myPack", model.name, "mesh.fbx");
        yield return BridgeAPI.DownloadFile(path, model.downloadUrl);
      }
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

      if (Utility.IsLoggedIn())
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

      if (GUILayout.Button("Download Asset Pack"))
      {
        Utility.Log("Starting asset pack download...");
        if (_downloadRoutine != null)
        {
          EditorCoroutineUtility.StopCoroutine(_downloadRoutine);
        }
        _downloadRoutine = EditorCoroutineUtility.StartCoroutine(StartDownloadPack(), this);
      }
    }
  }
}