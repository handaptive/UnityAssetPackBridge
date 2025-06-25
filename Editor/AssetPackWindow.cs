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

    private IEnumerator DownloadFbxToAsset(string name, string downloadUrl)
    {
      Utility.Log($"Downloading FBX: {name} from {downloadUrl}");
      using UnityWebRequest request = UnityWebRequest.Get(downloadUrl);
      string filePath = System.IO.Path.Combine(Application.persistentDataPath, name + ".fbx");
      request.downloadHandler = new DownloadHandlerFile(filePath);

      yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
      if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
      {
        Utility.Log($"Failed to download FBX: {request.error}");
      }
      else
      {
        Utility.Log($"FBX downloaded to: {filePath}");
      }
    }

    private IEnumerator StartDownloadPack()
    {
      Utility.Log("Starting pack download...");

      PackDownloadOutput output = null;
      yield return BridgeAPI.DownloadPack(new RequestArgs<PackDownloadOutput>
      {
        onSuccess = (result) =>
        {
          Utility.Log($"Pack downloaded successfully: {output.models.Length} models found.");
          output = result;
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
        yield return DownloadFbxToAsset(model.name, model.downloadUrl);
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