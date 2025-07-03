using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace AssetPack.Bridge.Editor
{
  public class AssetPackWindow : EditorWindow
  {
    public const string WindowName = "Asset Pack";
    private string _errorMessage = string.Empty;
    private PackListOutput.Pack[] _allPacks = new PackListOutput.Pack[0];

    private CallbackController _callback = new();
    private EditorCoroutine _packListRoutine = null;
    private EditorCoroutine _downloadRoutine = null;
    private EditorCoroutine _packModelsRoutine = null;

    private PackListOutput.Pack _activePack = null;
    private PackDownloadOutput.Model[] _packModels = new PackDownloadOutput.Model[0];

    private Label errorElement = null;
    private Label errorNoAuthElement = null;
    private ScrollView scrollView = null;
    private VisualElement authElement = null;
    private VisualElement noAuthElement = null;
    private Dictionary<string, Texture2D> conceptImages = new();
    private VisualElement packView = null;
    private Label packLabel = null;
    private Label packDescription = null;
    private Button packDownload = null;
    private Label packDownloading = null;
    private Image packImage = null;
    private ScrollView packScrollView = null;

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

    private IEnumerator StartDownloadPackModelRefs(string packId)
    {
      packDownload.SetEnabled(false);
      _packModels = new PackDownloadOutput.Model[0];
      PackDownloadOutput output = null;
      yield return BridgeAPI.GetDownloadablePack(
        new PackDownloadInput() { packId = packId },
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
        packDownload.SetEnabled(true);
        yield break;
      }

      _packModels = output.models.OrderBy(m => m.name).ToArray();
      packDownload.SetEnabled(true);
      RefreshView();
    }

    private IEnumerator StartDownloadPack()
    {
      Utility.Log("Starting pack download...");
      packDownload.SetEnabled(false);
      packDownloading.style.display = DisplayStyle.Flex;

      // make sure we download the model info first
      yield return _packListRoutine;

      for (int i = 0; i < _packModels.Length; i++)
      {
        var model = _packModels[i];
        Utility.Log($"Model {i + 1}/{_packModels.Length}: {model.name}");

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
      packDownload.SetEnabled(true);
      packDownloading.style.display = DisplayStyle.None;
      ClearError();
      RefreshView();
    }

    private IEnumerator StartListPacks()
    {
      Utility.Log($"Starting listing packs...");

      PackListOutput output = null;
      yield return BridgeAPI.GetListPacks(
        new RequestArgs<PackListOutput>
        {
          onSuccess = (result) =>
          {
            output = result;
            Utility.Log($"Packs listed successfully: {output.packs.Length} packs found.");
          },
          onError = (error) =>
          {
            Utility.LogError($"Pack list failed: {error}");
            _errorMessage = error;
          }
        });

      if (output == null)
      {
        Reset();
        RefreshView();
        yield break;
      }

      _allPacks = output.packs.OrderBy(p => p.name).ToArray();

      for (int i = 0; i < _allPacks.Length; i++)
      {
        PackListOutput.Pack pack = _allPacks[i];
        string url = pack.conceptImageUrl;
        EditorCoroutineUtility.StartCoroutine(DownloadPackImage(url), this);
      }

      RefreshView();
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

      // load pack list on login
      if (_packListRoutine != null)
      {
        EditorCoroutineUtility.StopCoroutine(_packListRoutine);
      }
      _packListRoutine = EditorCoroutineUtility.StartCoroutine(StartListPacks(), this);
    }

    private void OnCallbackErrored(string error)
    {
      Utility.LogError($"Login failed: {error}");
      _errorMessage = error;
      Repaint();
    }

    private void RefreshView()
    {
      if (_activePack != null && _activePack.id != "")
      {
        RefreshPackView();
      }
      else
      {
        RefreshScrollView();
      }
    }

    private void RefreshPackView()
    {
      if (_activePack == null) return;
      packLabel.text = _activePack.name;
      packDescription.text = _activePack.description;
      if (conceptImages.ContainsKey(_activePack.conceptImageUrl))
      {
        packImage.image = conceptImages[_activePack.conceptImageUrl];
      }

      if (packScrollView == null) return;

      packScrollView.Clear();
      for (int i = 0; i < _packModels.Length; i++)
      {
        var model = _packModels[i];
        Label label = new() { text = model.name };
        packScrollView.Add(label);
      }
    }

    private void RefreshScrollView()
    {
      if (scrollView == null) return;

      scrollView.Clear();
      for (int i = 0; i < _allPacks.Length; i++)
      {
        PackListOutput.Pack pack = _allPacks[i];

        VisualElement outerContainer = new();
        outerContainer.AddToClassList("packContainer");
        VisualElement container = new();
        container.AddToClassList("internalContainer");
        Label label = new() { text = pack.name };
        label.AddToClassList("packLabel");
        Texture2D texture;
        if (conceptImages.ContainsKey(pack.conceptImageUrl))
        {
          texture = conceptImages[pack.conceptImageUrl];
        }
        else
        {
          texture = new Texture2D(256, 256);
          Color solidColor = new Color(1f, 1f, 1f, 0f);
          Color[] pixels = new Color[256 * 256];
          for (int j = 0; j < pixels.Length; j++)
          {
            pixels[j] = solidColor;
          }
          texture.SetPixels(pixels);
          texture.Apply();
          conceptImages[pack.conceptImageUrl] = texture;
        }
        Image image = new() { image = texture };
        image.AddToClassList("packContainerImage");

        container.Add(image);
        container.Add(label);
        outerContainer.Add(container);
        scrollView.Add(outerContainer);

        container.RegisterCallback<ClickEvent>((evt) =>
        {
          _activePack = pack;

          Utility.Log($"Active pack: {pack.name}");
          if (_packModelsRoutine != null)
          {
            EditorCoroutineUtility.StopCoroutine(_packModelsRoutine);
          }
          _packModelsRoutine = EditorCoroutineUtility.StartCoroutine(StartDownloadPackModelRefs(pack.id), this);
          RefreshView();
        });
      }
    }

    private IEnumerator DownloadPackImage(string url)
    {
      UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
      yield return www.SendWebRequest();

      if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
      {
        Utility.Log(www.error);
        _errorMessage = www.error;
      }
      else
      {
        Texture2D myTexture = DownloadHandlerTexture.GetContent(www);
        if (myTexture != null)
        {
          conceptImages[url] = myTexture;
        }
      }
      RefreshView();
    }

    public void CreateGUI()
    {
      // Each editor window contains a root VisualElement object.
      VisualElement root = rootVisualElement;

      // Import UXML.
      var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Bridge/Editor/AssetPackWindow.uxml");
      VisualElement ScrollViewExample = visualTree.Instantiate();
      root.Add(ScrollViewExample);

      // Find the relevant views by name.
      errorElement = root.Query<Label>("error");
      errorNoAuthElement = root.Query<Label>("errorNoAuth");
      scrollView = root.Query<ScrollView>("scrollView");
      authElement = root.Query<VisualElement>("auth");
      noAuthElement = root.Query<VisualElement>("noAuth");
      packView = root.Query<VisualElement>("pack");
      packLabel = root.Query<Label>("packLabel");
      packDescription = root.Query<Label>("packDescription");
      packImage = root.Query<Image>("packImage");
      packDownloading = root.Query<Label>("packDownloading");
      packDownloading.style.display = DisplayStyle.None;
      packScrollView = root.Query<ScrollView>("packScrollView");

      // Setup static button callbacks.
      Button logoutButton = root.Query<Button>("logout");
      logoutButton.clicked += () =>
      {
        Utility.Log("Logging out...");
        Utility.ClearTokens();
        Reset();
      };
      Button loginButton = root.Query<Button>("login");
      loginButton.clicked += () =>
      {
        Utility.Log("Starting login process...");
        _callback.Start();
        ClearError();
      };
      Button refreshButton = root.Query<Button>("refresh");
      refreshButton.clicked += () =>
      {
        Utility.Log("Starting pack list refresh...");
        if (_packListRoutine != null)
        {
          EditorCoroutineUtility.StopCoroutine(_packListRoutine);
        }
        _packListRoutine = EditorCoroutineUtility.StartCoroutine(StartListPacks(), this);
      };
      Button packCloseButton = root.Query<Button>("packClose");
      packCloseButton.clicked += () =>
      {
        _activePack = null;
        _packModels = new PackDownloadOutput.Model[0];
        RefreshView();
      };
      packDownload = root.Query<Button>("packDownload");
      packDownload.clicked += () =>
      {
        Utility.Log("Starting pack download...");
        if (_downloadRoutine != null)
        {
          EditorCoroutineUtility.StopCoroutine(_downloadRoutine);
        }
        _downloadRoutine = EditorCoroutineUtility.StartCoroutine(StartDownloadPack(), this);
      };

      RefreshView();
    }

    void OnGUI()
    {
      if (errorElement == null) return;
      if (errorNoAuthElement == null) return;
      if (packView == null) return;
      if (authElement == null) return;
      if (noAuthElement == null) return;

      errorElement.text = null; // null here
      errorNoAuthElement.text = null;
      packView.style.display = DisplayStyle.None;
      authElement.style.display = DisplayStyle.None;
      noAuthElement.style.display = DisplayStyle.None;

      if (Utility.IsLoggedIn())
      {
        errorElement.text = _errorMessage;
        if (_activePack != null && _activePack.id != "") packView.style.display = DisplayStyle.Flex;
        else authElement.style.display = DisplayStyle.Flex;
      }
      else
      {
        errorNoAuthElement.text = _errorMessage;
        noAuthElement.style.display = DisplayStyle.Flex;
      }
    }

    void Reset()
    {
      _activePack = null;
      _packModels = new PackDownloadOutput.Model[0];
      _allPacks = new PackListOutput.Pack[0];
    }
  }
}
