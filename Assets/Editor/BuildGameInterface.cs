using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Pipeline.Commands;

public static class BuildGameInterface
{
    [CliCommand("goheroes", "build-ui", "Build the Go Heroes Canvas scene.")]
    public static string BuildUi()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Canvas" || root.name == "EventSystem" || root.name == "Go Heroes UI") Object.DestroyImmediate(root);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 600);

        Image("Background", canvas.transform, new Color(.055f,.067f,.09f), Vector2.zero, Vector2.one);
        var screen = Image("Game Screen", canvas.transform, new Color(.075f,.095f,.13f), new Vector2(.18f,.13f), new Vector2(.82f,.87f));
        Text("Title", screen.transform, "GO HEROES", font, 30, new Vector2(0,.86f), new Vector2(1,.98f));
        Text("Subtitle", screen.transform, "TURN-BASED BATTLE", font, 12, new Vector2(0,.80f), new Vector2(1,.86f));
        Bar("Enemy Health Bar", screen.transform, "ENEMY", new Color(.91f,.2f,.25f), new Vector2(.06f,.70f), new Vector2(.47f,.75f), font);
        Bar("Player Health Bar", screen.transform, "YOU", new Color(.04f,.59f,.39f), new Vector2(.53f,.70f), new Vector2(.94f,.75f), font);
        Image("Enemy Hero", screen.transform, new Color(.25f,.35f,.55f), new Vector2(.12f,.32f), new Vector2(.32f,.62f));
        Image("Player Hero", screen.transform, new Color(.25f,.35f,.55f), new Vector2(.68f,.32f), new Vector2(.88f,.62f));
        var panel = Image("Action Panel", screen.transform, new Color(.055f,.07f,.1f), new Vector2(.04f,.04f), new Vector2(.96f,.25f));
        Button("Play Button", panel.transform, "PLAY", font, new Color(.19f,.43f,.94f), new Vector2(.06f,.15f), new Vector2(.94f,.75f));
        Button("Attack Button", panel.transform, "ATTACK", font, new Color(.91f,.2f,.25f), new Vector2(.06f,.15f), new Vector2(.31f,.75f));
        Button("Defend Button", panel.transform, "DEFEND", font, new Color(.18f,.28f,.48f), new Vector2(.37f,.15f), new Vector2(.63f,.75f));
        Button("Heal Button", panel.transform, "HEAL", font, new Color(.04f,.59f,.39f), new Vector2(.69f,.15f), new Vector2(.94f,.75f));
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity", true);
        return "Built Canvas hierarchy in SampleScene.";
    }
    static Image Image(string n, Transform p, Color c, Vector2 min, Vector2 max) { var g=new GameObject(n,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var i=g.GetComponent<Image>();i.color=c;return i; }
    static void Text(string n,Transform p,string value,Font f,int size,Vector2 min,Vector2 max){var g=new GameObject(n,typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=g.GetComponent<Text>();t.font=f;t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;}
    static void Bar(string n,Transform p,string label,Color c,Vector2 min,Vector2 max,Font f){var b=Image(n,p,Color.black,min,max);Image("Fill",b.transform,c,new Vector2(.03f,.2f),new Vector2(.78f,.8f));Text("Label",b.transform,label,f,10,new Vector2(.03f,.1f),new Vector2(.97f,.9f));}
    static void Button(string n,Transform p,string label,Font f,Color c,Vector2 min,Vector2 max){var b=Image(n,p,c,min,max);b.gameObject.AddComponent<Button>();Text("Label",b.transform,label,f,18,Vector2.zero,Vector2.one);}
}
