using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menu: Tools > Criaturas > ...
public static class CreatureAnimationBuilder
{
    const string SpriteRoot = "Assets/Sprites_Temporarios/Sprites";
    const string AnimRoot = "Assets/Animations";

    static readonly string[] Creatures = {
        "Onca", "Coelho", "Arara", "Javali", "Cobra", "Passaro1", "Passaro2", "Sapo", "Inseto",
        "AraraVermelha", "CobraVerde", "CobraAzul",
        "InsetoVerde", "InsetoAzul", "InsetoRoxo",
        "Passaro1Verde", "Passaro1Azul",
        "SapoLaranja", "SapoAzul", "SapoMarrom",
    };

    static readonly string[] LoopActions = { "idle", "walk", "run", "move", "fly", "swim" };

    [MenuItem("Tools/Criaturas/Build All")]
    public static void BuildAll()
    {
        foreach (var c in Creatures) BuildCreature(c);
    }

    [MenuItem("Tools/Criaturas/Build Onca")]
    public static void BuildOnca() => BuildCreature("Onca");

    [MenuItem("Tools/Criaturas/Build Coelho")]
    public static void BuildCoelho() => BuildCreature("Coelho");

    [MenuItem("Tools/Criaturas/Build Arara")]
    public static void BuildArara() => BuildCreature("Arara");

    public static void BuildCreature(string creature)
    {
        string spriteDir = $"{SpriteRoot}/{creature}";
        if (!AssetDatabase.IsValidFolder(spriteDir))
        {
            Debug.LogError($"[Criaturas] Pasta nao encontrada: {spriteDir}");
            return;
        }

        string animDir = $"{AnimRoot}/{creature}";
        if (!AssetDatabase.IsValidFolder(AnimRoot))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(animDir))
            AssetDatabase.CreateFolder(AnimRoot, creature);

        ConfigureImporters(spriteDir);

        var actions = AssetDatabase.GetSubFolders(spriteDir)
            .Select(p => p.Substring(p.LastIndexOf('/') + 1))
            .OrderBy(n => n)
            .ToArray();

        var clips = new Dictionary<string, AnimationClip>();
        foreach (var action in actions)
        {
            var clip = BuildClip(creature, spriteDir, animDir, action);
            if (clip != null) clips[action] = clip;
        }

        BuildController(creature, animDir, clips, actions);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Criaturas] {creature}: {clips.Count} clips ({string.Join(", ", clips.Keys)}) + controller em {animDir}");
    }

    static void ConfigureImporters(string spriteDir)
    {
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { spriteDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);

            ti.SaveAndReimport();
        }
    }

    static AnimationClip BuildClip(string creature, string spriteDir, string animDir, string action)
    {
        string dir = $"{spriteDir}/{action}";
        var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { dir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(sp => sp != null)
            .OrderBy(sp => FrameIndex(sp.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[Criaturas] Sem sprites em {dir}");
            return null;
        }

        float fps = FrameRateFor(action);
        var clip = new AnimationClip { frameRate = fps };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = IsLoop(action);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string outPath = $"{animDir}/{creature}_{action}.anim";
        AssetDatabase.DeleteAsset(outPath);
        AssetDatabase.CreateAsset(clip, outPath);
        return clip;
    }

    static void BuildController(string creature, string animDir, Dictionary<string, AnimationClip> clips, string[] actions)
    {
        string path = $"{animDir}/{creature}.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = controller.layers[0].stateMachine;

        AnimatorState defaultState = null;
        float y = 60f;
        foreach (var action in actions)
        {
            if (!clips.TryGetValue(action, out var clip)) continue;
            var st = sm.AddState(action, new Vector3(260f, y, 0f));
            st.motion = clip;
            y += 60f;
            if (defaultState == null || action.ToLower() == "idle") defaultState = st;
        }

        if (defaultState != null) sm.defaultState = defaultState;
        EditorUtility.SetDirty(controller);
    }

    static int FrameIndex(string spriteName)
    {
        int dash = spriteName.LastIndexOf('-');
        if (dash >= 0 && int.TryParse(spriteName.Substring(dash + 1), out int n)) return n;
        return 0;
    }

    static bool IsLoop(string action)
    {
        string a = action.ToLower();
        return LoopActions.Any(k => a.Contains(k));
    }

    static float FrameRateFor(string action)
    {
        string a = action.ToLower();
        if (a.Contains("idle")) return 6f;
        if (a.Contains("run")) return 12f;
        if (a.Contains("fly")) return 12f;
        if (a.Contains("walk") || a.Contains("move")) return 8f;
        if (a.Contains("atack") || a.Contains("attack")) return 12f;
        if (a.Contains("death") || a.Contains("die") || a.Contains("hurt")) return 8f;
        return 10f;
    }
}
