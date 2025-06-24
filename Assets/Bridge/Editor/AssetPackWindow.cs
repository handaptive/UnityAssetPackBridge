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

    void OnGUI()
    {
      GUILayout.Label(WindowName, EditorStyles.boldLabel);
      GUILayout.Space(10);

      if (GUILayout.Button("Open Website"))
      {
        Debug.Log("Opening Asset Pack website...");
        Application.OpenURL($"{Utility.GetWebsiteUrl()}/login");
      }
    }
  }
}