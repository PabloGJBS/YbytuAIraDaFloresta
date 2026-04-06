using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Setup do sistema de audio: cria SoundLibrary.asset apontando para SFX
/// existentes e gera AudioManager.prefab pronto pra arrastar nas cenas.
/// Roda uma vez via menu "Tools/Setup/Audio System".
/// </summary>
public static class AudioSetup
{
    private const string LibraryPath = "Assets/Audio/SoundLibrary.asset";
    private const string PrefabPath = "Assets/Prefabs/AudioManager.prefab";

    [MenuItem("Tools/Setup/Audio System")]
    public static void Run()
    {
        EnsureDir("Assets/Audio");
        EnsureDir("Assets/Prefabs");

        // 1) Cria ou carrega a SoundLibrary
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        // 2) Assign clips a partir dos MP3 existentes em Assets/SoundEffects
        library.playerPunch     = Load("Assets/SoundEffects/SoundEffectPunch.mp3");
        library.playerKick      = Load("Assets/SoundEffects/SoundEffectKick.mp3");
        library.playerJab       = Load("Assets/SoundEffects/SoundEffectPunch.mp3"); // reusa
        library.playerJumpKick  = Load("Assets/SoundEffects/SoundEffectKick.mp3");
        library.playerDiveKick  = Load("Assets/SoundEffects/SoundEffectKick.mp3");
        library.playerJump      = Load("Assets/SoundEffects/SoundEffectJump.mp3");
        library.playerHurt      = Load("Assets/SoundEffects/SoundEffectHurt.mp3");
        library.enemyPunch      = Load("Assets/SoundEffects/SoundEffectPunch.mp3");
        library.enemyHurt       = Load("Assets/SoundEffects/SoundEffectHurt.mp3");
        library.uiSelect        = Load("Assets/SoundEffects/SoundEffectSelectButton.mp3");
        library.uiConfirm       = Load("Assets/SoundEffects/SoundEffectSelectButton.mp3");

        EditorUtility.SetDirty(library);

        // 3) Gera AudioManager prefab
        var go = new GameObject("AudioManager");
        var manager = go.AddComponent<SoundManager>();
        var soField = typeof(SoundManager).GetField("library",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        soField?.SetValue(manager, library);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AudioSetup] OK: SoundLibrary + AudioManager.prefab criados. " +
                  "AudioMixer ainda precisa ser criado manualmente: Assets > Create > Audio Mixer.");
    }

    private static AudioClip Load(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning("[AudioSetup] Faltando: " + path);
        return clip;
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
