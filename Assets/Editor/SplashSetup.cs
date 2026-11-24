using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoHeroes.Splash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GoHeroes.EditorTools
{
  /// <summary>
  /// Builds Assets/Scenes/Splash.unity from the sprites and layout in Assets/Splash (shared with
  /// Let Them In), and keeps the splash first in the build. Rebuilding backs the old
  /// scene up first. Timing, motion, composition, melody and levels live on StacktoneSplash, not here.
  /// </summary>
  public static class SplashSetup
  {
    public const string ScenePath = "Assets/Scenes/Splash.unity";
    public const string GamePath = "Assets/Scenes/SampleScene.unity";
    const string ArtFolder = "Assets/Splash";
    const string LayoutPath = ArtFolder + "/splash_layout.json";
    const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const string BackupFolder = "Backups/Scenes";
    const int Voices = 24; // 6 layers + 9 notes + up to 5 chord notes, with room to spare.
    const string PianoFolder = "Assets/Audio/Piano";

    static readonly string[] PianoNotes = { "D", "E", "F", "G", "A", "B" };

    [Serializable] sealed class Layout { public float width, height; public PieceInfo[] pieces; }
    [Serializable] sealed class PieceInfo { public string name, group; public float x, y; public int width, height; }

    [MenuItem("Go Heroes/Build Splash Scene")]
    public static void BuildInteractive()
    {
      if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Splash.unity?",
            $"The splash scene is rebuilt from {ArtFolder}. Changes made by hand are lost. A backup is saved to {BackupFolder}.",
            "Rebuild", "Cancel"))
        return;
      if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
      Build();
    }

    /// <summary>For batch mode: -executeMethod GoHeroes.EditorTools.SplashSetup.BuildBatch</summary>
    public static void BuildBatch() => Build();

    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

    static void Build()
    {
      if (Application.isPlaying) return;
      AssetDatabase.ImportAsset(ArtFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
      AssetDatabase.ImportAsset(PianoFolder, ImportAssetOptions.ImportRecursive);
      var json = Load<TextAsset>(LayoutPath);
      if (json == null)
      {
        Debug.LogError($"Splash: {LayoutPath} is missing.");
        return;
      }
      var layout = JsonUtility.FromJson<Layout>(json.text);
      var material = Load<Material>(UnlitMaterial);

      if (File.Exists(ScenePath))
      {
        Directory.CreateDirectory(BackupFolder);
        File.Copy(ScenePath, Path.Combine(BackupFolder, $"Splash-{DateTime.Now:yyyyMMdd-HHmmss}.unity"), true);
      }
      var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

      var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
      cameraObject.transform.position = new Vector3(0f, 0f, -10f);
      var view = cameraObject.AddComponent<Camera>();
      view.orthographic = true;
      view.orthographicSize = 1440f;
      view.clearFlags = CameraClearFlags.SolidColor;
      view.backgroundColor = Color.black; // The same black as the game's bars and the curtain.
      view.nearClipPlane = .3f;
      view.farClipPlane = 100f;
      cameraObject.AddComponent<AudioListener>();

      var root = new GameObject("Splash");
      var splash = root.AddComponent<StacktoneSplash>();
      var logo = new GameObject("Logo").transform;
      logo.SetParent(root.transform, false);

      SpriteRenderer whole = null;
      var layers = new List<SpriteRenderer>();
      var letters = new List<SpriteRenderer>();
      foreach (var info in layout.pieces)
      {
        var sprite = Load<Sprite>($"{ArtFolder}/{info.name}.png");
        if (sprite == null) { Debug.LogError($"Splash: sprite {info.name}.png is missing."); continue; }
        var renderer = new GameObject(info.name).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(logo, false);
        renderer.transform.localPosition = new Vector3(info.x, info.y, 0f);
        renderer.sprite = sprite;
        if (material != null) renderer.sharedMaterial = material;
        switch (info.group)
        {
          case "whole": whole = renderer; renderer.sortingOrder = 10; break;
          case "layer": layers.Add(renderer); renderer.sortingOrder = 10 + layers.Count; break;
          default: letters.Add(renderer); renderer.sortingOrder = 20; break;
        }
      }
      // The scene shows the finished logo. The layers only exist for the animation.
      foreach (var layer in layers) layer.enabled = false;

      var handoff = BuildHandoff();

      var so = new SerializedObject(splash);
      so.FindProperty("view").objectReferenceValue = view;
      so.FindProperty("logo").objectReferenceValue = logo;
      so.FindProperty("logoSize").vector2Value = new Vector2(layout.width, layout.height);
      so.FindProperty("iconWhole").objectReferenceValue = whole;
      SetArray(so.FindProperty("iconLayers"), layers);
      SetArray(so.FindProperty("letters"), letters);
      so.FindProperty("handoff").objectReferenceValue = handoff;
      so.FindProperty("effects.layer").objectReferenceValue = Load<AudioClip>("Assets/Audio/splash_layer.wav");
      so.FindProperty("effects.letterClick").objectReferenceValue = Load<AudioClip>("Assets/Audio/splash_land.wav");
      so.FindProperty("effects.chime").objectReferenceValue = Load<AudioClip>("Assets/Audio/splash_done.wav");
      foreach (string note in PianoNotes)
        so.FindProperty("piano." + note.ToLowerInvariant()).objectReferenceValue = Load<AudioClip>($"{PianoFolder}/{note}.wav");
      so.FindProperty("nextScene").stringValue = Path.GetFileNameWithoutExtension(GamePath);
      so.ApplyModifiedPropertiesWithoutUndo();

      EditorSceneManager.SaveScene(scene, ScenePath);
      OrderBuildScenes();
      AssetDatabase.SaveAssets();
      Debug.Log($"Splash: built {ScenePath} with {layers.Count} layers and {letters.Count} letters.");
    }

    static SplashHandoff BuildHandoff()
    {
      var go = new GameObject("Handoff");
      var handoff = go.AddComponent<SplashHandoff>();
      var voices = new List<AudioSource>();
      for (int i = 0; i < Voices; i++) voices.Add(Source(go, null, false));

      var so = new SerializedObject(handoff);
      var list = so.FindProperty("voices");
      list.arraySize = voices.Count;
      for (int i = 0; i < voices.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = voices[i];
      so.ApplyModifiedPropertiesWithoutUndo();
      return handoff;
    }

    static AudioSource Source(GameObject go, AudioClip clip, bool loop)
    {
      var source = go.AddComponent<AudioSource>();
      source.playOnAwake = false;
      source.spatialBlend = 0f;
      source.clip = clip;
      source.loop = loop;
      source.volume = loop ? 0f : 1f;
      return source;
    }

    static void SetArray(SerializedProperty property, IReadOnlyList<SpriteRenderer> items)
    {
      property.arraySize = items.Count;
      for (int i = 0; i < items.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    /// <summary>
    /// The splash first (when it exists), then the game, then anything else in its existing order.
    /// Both are enabled.
    /// </summary>
    public static void OrderBuildScenes()
    {
      var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GamePath);
      var ordered = new List<EditorBuildSettingsScene>();
      if (File.Exists(ScenePath)) ordered.Add(new EditorBuildSettingsScene(ScenePath, true));
      if (File.Exists(GamePath)) ordered.Add(new EditorBuildSettingsScene(GamePath, true));
      ordered.AddRange(others);
      var current = EditorBuildSettings.scenes;
      bool same = current.Length == ordered.Count &&
                  current.Zip(ordered, (a, b) => a.path == b.path && a.enabled == b.enabled).All(x => x);
      if (!same) EditorBuildSettings.scenes = ordered.ToArray();
    }

    /// <summary>Reports problems without changing anything. Empty when the splash is ready.</summary>
    public static List<string> Problems(StacktoneSplash splash)
    {
      var problems = new List<string>();
      var so = new SerializedObject(splash);
      foreach (string name in new[] { "view", "logo", "iconWhole", "handoff", "effects.layer", "effects.letterClick", "effects.chime",
                                      "piano.d", "piano.e", "piano.f", "piano.g", "piano.a", "piano.b" })
        if (so.FindProperty(name).objectReferenceValue == null) problems.Add(name);
      foreach (string name in new[] { "iconLayers", "letters" })
      {
        var list = so.FindProperty(name);
        if (list.arraySize == 0) problems.Add(name + " is empty");
        for (int i = 0; i < list.arraySize; i++)
          if (list.GetArrayElementAtIndex(i).objectReferenceValue == null) problems.Add($"{name}[{i}]");
      }
      var handoff = so.FindProperty("handoff").objectReferenceValue as SplashHandoff;
      if (handoff != null)
      {
        var hso = new SerializedObject(handoff);
        var voices = hso.FindProperty("voices");
        if (voices.arraySize == 0) problems.Add("handoff voices is empty");
        for (int i = 0; i < voices.arraySize; i++)
          if (voices.GetArrayElementAtIndex(i).objectReferenceValue == null) problems.Add($"handoff voices[{i}]");
      }
      return problems;
    }
  }
}
