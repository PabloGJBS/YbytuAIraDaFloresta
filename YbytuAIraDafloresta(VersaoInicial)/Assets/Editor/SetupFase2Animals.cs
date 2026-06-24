using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupFase2Animals
{
    private const string SprDir = "Assets/Sprites_Temporarios/Sprites/";
    private const string AnimDir = "Assets/Animations/";
    private const string OutDir = "Assets/Prefabs/Animals/";

    private class Cfg
    {
        public string name, folder, controllerName, allyType;
        public string idle = "Idle", run = "Walk", attack = "Attack", hurt = "Hurt", death = "Death";
        public int hp; public int dmg; public float speed; public float scale = 1f;
        public Vector2 hurtSize = new Vector2(1.3f, 2.5f); public Vector2 hurtOff = new Vector2(0f, 1.2f);
        public string thanks, farewell;
    }

    private static readonly Cfg[] Animals =
    {
        new Cfg { name="CobraAlly",      folder="Cobra",      controllerName="Cobra",      allyType="SnakeAlly",
                  run="Walk", attack="Attack", hurt="Hurt", death="Death",
                  hp=35, dmg=5, speed=3.2f, scale=0.8f, hurtSize=new Vector2(1.1f,1.4f), hurtOff=new Vector2(0f,0.7f),
                  thanks="Ssssim! Livre! Conte comigo!", farewell="Obrigada! Vou pra um lugar seguro." },
        new Cfg { name="CobraAzulAlly",  folder="CobraAzul",  controllerName="CobraAzul",  allyType="SnakeAlly",
                  run="Walk", attack="Attack", hurt="Hurt", death="Death",
                  hp=35, dmg=5, speed=3.2f, scale=0.8f, hurtSize=new Vector2(1.1f,1.4f), hurtOff=new Vector2(0f,0.7f),
                  thanks="Obrigada por nos soltar!", farewell="Boa sorte!" },
        new Cfg { name="CobraVerdeAlly", folder="CobraVerde", controllerName="CobraVerde", allyType="SnakeAlly",
                  run="Walk", attack="Attack", hurt="Hurt", death="Death",
                  hp=35, dmg=5, speed=3.2f, scale=0.8f, hurtSize=new Vector2(1.1f,1.4f), hurtOff=new Vector2(0f,0.7f),
                  thanks="Vamos juntos!", farewell="Obrigada!" },
        new Cfg { name="OncaAlly",       folder="Onca",       controllerName="Onca",       allyType="OncaAlly",
                  run="Walk", attack="Atack1", hurt="Idle", death="Death",
                  hp=130, dmg=14, speed=2.8f, scale=1f, hurtSize=new Vector2(1.6f,2.2f), hurtOff=new Vector2(0f,1.1f),
                  thanks="Você me libertou! Vou lutar com você!", farewell="Obrigada, humano. Cuide da floresta." },
        new Cfg { name="JavaliAlly",     folder="Javali",     controllerName="Javali",     allyType="BoarAlly",
                  run="Run", attack="Attack", hurt="Hurt", death="Death",
                  hp=110, dmg=8, speed=2.4f, scale=0.9f, hurtSize=new Vector2(1.5f,2.0f), hurtOff=new Vector2(0f,1.0f),
                  thanks="Obrigado! Esses bandidos vão pagar!", farewell="Vou procurar minha familia!" },
    };

    [MenuItem("Tools/Setup/Setup Fase2 Animals")]
    public static void Run()
    {
        EnsureFolder(OutDir);
        foreach (var c in Animals) BuildAnimal(c);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Fase2Animals] OK. Animais (3 cobras + onça + javali) em " + OutDir);
    }

    private static void BuildAnimal(Cfg c)
    {
        var go = new GameObject(c.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(SprDir + c.folder + "/Idle/Idle-1.png");
        sr.sortingOrder = 50;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = Load<AnimatorController>(AnimDir + c.folder + "/" + c.controllerName + ".controller");

        go.AddComponent<HealthSystem>();
        go.AddComponent<YSortRenderer>();

        var type = System.Type.GetType(c.allyType + ", Assembly-CSharp");
        var ally = (AllyCreature)go.AddComponent(type);
        ally.maxHealth = c.hp;
        ally.attackDamage = c.dmg;
        ally.moveSpeed = c.speed;
        ally.idleState = c.idle; ally.runState = c.run; ally.attackState = c.attack;
        ally.hurtState = c.hurt; ally.deathState = c.death;
        ally.hurtboxSize = c.hurtSize; ally.hurtboxOffset = c.hurtOff;
        ally.spriteFacesRight = true;
        ally.shoutLine = c.thanks; ally.shoutLocalizationKey = "";
        ally.farewellLine = c.farewell; ally.farewellLocalizationKey = "";

        go.transform.localScale = new Vector3(c.scale, c.scale, 1f);

        PrefabUtility.SaveAsPrefabAsset(go, OutDir + c.name + ".prefab");
        Object.DestroyImmediate(go);
        Debug.Log($"[Fase2Animals] {c.name} (HP {c.hp}, dmg {c.dmg}).");
    }

    // ---- helpers ----
    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite sp) return sp;
        Debug.LogWarning("[Fase2Animals] sprite nao encontrado: " + path);
        return null;
    }

    private static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("[Fase2Animals] asset nao encontrado: " + path);
        return a;
    }

    private static void EnsureFolder(string folder)
    {
        var parts = folder.TrimEnd('/').Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
