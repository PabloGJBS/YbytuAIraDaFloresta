using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Posiciona os 3 troncos interativos nos trechos de caminhada da Stage1.
/// Cada tronco: SpriteRenderer + BoxCollider2D solido na base (nao atravessavel) +
/// YSortRenderer + um filho "EduTrigger" com EducationalMarker (frase, modo interativo).
/// Idempotente. Menu: Tools/Ybytu/Place Troncos
/// </summary>
public static class PlaceTroncos
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";
    private const string Dir = "Assets/Sprites/Stage1/Tilesets/Props";

    [MenuItem("Tools/Ybytu/Place Troncos")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var old = GameObject.Find("Troncos");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Troncos");
        SceneManager.MoveGameObjectToScene(root, scene);

        Make(root, "TroncoInterativo1", new Vector3(17.5f, -3f, 0f),
            "Os povos originários são os maiores guardiões da floresta. Onde há terra indígena demarcada e respeitada, a mata continua de pé, porque para eles a terra não é mercadoria: é casa, é parente, é vida.");
        Make(root, "TroncoInterativo3", new Vector3(47.5f, -3f, 0f),
            "Uma única árvore pode abrigar centenas de espécies de insetos, aves, plantas e fungos que dependem só dela. Quando ela cai, não é uma árvore que se perde: é um mundo inteiro que se apaga.");
        Make(root, "TroncoInterativo2", new Vector3(76f, -3f, 0f),
            "A água que chega limpa à sua torneira começa muito longe, na floresta. São as árvores que capturam a chuva, alimentam os rios voadores e mantêm as nascentes vivas. Sem mata, as torneiras secam, mesmo nas grandes cidades.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PlaceTroncos] 3 troncos posicionados (colisor solido + marcador educativo).");
    }

    private static void Make(GameObject parent, string sprite, Vector3 pos, string phrase)
    {
        var go = new GameObject(sprite, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(YSortRenderer));
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{sprite}.png");
        // Ordem visivel em edit mode (= -Y*100, igual ao YSort em runtime)
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);

        // Brilho/contorno branco ao aproximar
        go.AddComponent<InteractableHighlight>();

        // Colisor solido na base do tronco (player nao atravessa; passa por cima/baixo na faixa)
        var col = go.GetComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = new Vector2(2.4f, 1.6f);
        col.offset = new Vector2(0f, -2.0f);

        // Trigger educativo (filho) - area maior pra disparar a frase ao chegar perto
        var trig = new GameObject("EduTrigger", typeof(BoxCollider2D), typeof(EducationalMarker));
        trig.transform.SetParent(go.transform, false);
        var tcol = trig.GetComponent<BoxCollider2D>();
        tcol.size = new Vector2(6f, 6f);
        tcol.offset = new Vector2(0f, -1.5f);
        var marker = trig.GetComponent<EducationalMarker>();
        marker.useRandomFromPool = true; // sorteia 1 das 7, sem repetir entre troncos
        marker.requireInteraction = true;
        marker.showOnce = true;
    }
}
