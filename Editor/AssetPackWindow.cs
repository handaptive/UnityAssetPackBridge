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
      yield return BridgeAPI.GetDownloadablePack(
        new PackDownloadInput() { packId = "zSGhLkez9usUvT98VuGI" },
        new RequestArgs<PackDownloadOutput>
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
        Utility.Log($"Model {i + 1}/{output.models.Length}: {model.name}");

        var meshPath = Utility.GetModelFilePath("myPack", model.name, "mesh.fbx");
        yield return BridgeAPI.DownloadFile(meshPath, model.fbxUrl);

        var diffusePath = Utility.GetModelFilePath("myPack", model.name, "diffuse.png");
        yield return BridgeAPI.DownloadFile(diffusePath, model.diffuseUrl);

        // Refresh asset database to ensure texture is imported
        AssetDatabase.ImportAsset(Utility.AssetRelativePath(diffusePath));
        AssetDatabase.Refresh();

        var materialPath = Utility.GetModelFilePath("myPack", model.name, "material.mat");
        Utility.Log($"Creating material at {materialPath}");
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
          mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Utility.AssetRelativePath(diffusePath))
        };
        // remove smoothness and metallic properties
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, Utility.AssetRelativePath(materialPath));

        var prefabPath = Utility.GetModelFilePath("myPack", model.name, "prefab.prefab");
        Utility.Log($"Creating prefab at {prefabPath}");
        var prefab = new GameObject(model.name);

        // Load and instantiate the mesh as a child
        var meshAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Utility.AssetRelativePath(meshPath));
        if (meshAsset != null)
        {
          var meshInstance = Instantiate(meshAsset, prefab.transform);
          meshInstance.name = model.name + "_Mesh";
        }

        // // Assign the material to the mesh
        // var meshRenderer = prefab.GetComponentInChildren<Renderer>();
        // if (meshRenderer != null)
        // {
        //   Utility.Log($"Assigning material to mesh renderer: {meshRenderer.name}");
        //   meshRenderer.material = material;
        //   meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        //   meshRenderer.receiveShadows = true;
        // }

        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefab, Utility.AssetRelativePath(prefabPath));
        Utility.Log($"Prefab created at {prefabPath}");

        // Clean up the temporary prefab GameObject
        DestroyImmediate(prefab);
        Utility.Log($"Temporary prefab GameObject destroyed.");
      }

      Utility.Log($"All models downloaded successfully.");
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      _downloadRoutine = null;
      ClearError();
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