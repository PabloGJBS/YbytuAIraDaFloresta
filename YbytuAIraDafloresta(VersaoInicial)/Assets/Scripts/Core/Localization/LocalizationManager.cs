using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gerenciador de localizacao (multi-idioma).
/// Carrega um arquivo JSON por idioma e fornece textos por chave.
///
/// Uso:
///   LocalizationManager.Instance.GetText("ui.main_menu.play") → "Jogar" ou "Play"
///   LocalizationManager.Instance.GetText("story.intro.scene_01") → texto da intro
///   LocalizationManager.Instance.GetText("stages.stage_01.name") → nome da fase
///
/// Para adicionar um novo idioma:
///   1. Copiar pt-BR.json e renomear (ex: es-ES.json)
///   2. Traduzir os valores (manter as mesmas chaves)
///   3. Adicionar o TextAsset no array availableLanguages no Inspector
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager instance;
    public static LocalizationManager Instance => instance;

    [Header("Configuracao")]
    [SerializeField] private string defaultLanguage = "pt-BR";
    [SerializeField] private TextAsset[] availableLanguages;

    private Dictionary<string, string> currentTexts = new Dictionary<string, string>();
    private string currentLanguageCode;
    private Dictionary<string, TextAsset> languageAssets = new Dictionary<string, TextAsset>();

    public string CurrentLanguage => currentLanguageCode;
    public event Action<string> OnLanguageChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Destroi apenas ESTE componente, nao o GameObject: o LocalizationManager
            // compartilha o GameObject com o controller da cutscene (IntroCutscene/FinalizacaoFase1Cutscene).
            // Destruir o GO inteiro mataria a cutscene -> tela preta no 2o play.
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        IndexLanguages();
        LoadLanguage(GetSavedLanguage());
    }

    private void IndexLanguages()
    {
        languageAssets.Clear();
        if (availableLanguages == null) return;

        foreach (var asset in availableLanguages)
        {
            if (asset == null) continue;
            // O nome do arquivo eh o codigo do idioma (pt-BR, en-US, etc)
            languageAssets[asset.name] = asset;
        }
    }

    /// <summary>
    /// Trocar o idioma ativo.
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        LoadLanguage(languageCode);
        PlayerPrefs.SetString("game_language", languageCode);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(languageCode);
    }

    /// <summary>
    /// Buscar texto por chave com notacao de ponto.
    /// Ex: "ui.main_menu.play", "story.intro.scene_01", "stages.stage_01.name"
    /// </summary>
    public string GetText(string key)
    {
        if (currentTexts.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[Localization] Chave nao encontrada: {key}");
        return $"[{key}]";
    }

    /// <summary>
    /// Buscar texto com formatacao (substitui {0}, {1}, etc).
    /// Ex: GetTextFormatted("ui.hud.wave", 2, 5) → "Wave 2/5"
    /// </summary>
    public string GetTextFormatted(string key, params object[] args)
    {
        string text = GetText(key);
        try
        {
            return string.Format(text, args);
        }
        catch
        {
            return text;
        }
    }

    /// <summary>
    /// Retorna todos os textos de uma secao como array ordenado.
    /// Ex: GetSection("story.intro") → ["scene_01 text", "scene_02 text", ...]
    /// Util para cutscenes que mostram textos em sequencia.
    /// </summary>
    public string[] GetSection(string prefix)
    {
        var results = new List<string>();
        var sorted = new SortedDictionary<string, string>();

        foreach (var kvp in currentTexts)
        {
            if (kvp.Key.StartsWith(prefix + "."))
            {
                string subKey = kvp.Key.Substring(prefix.Length + 1);
                // Ignorar sub-secoes (apenas pegar chaves diretas)
                if (!subKey.Contains("."))
                    sorted[subKey] = kvp.Value;
            }
        }

        foreach (var kvp in sorted)
            results.Add(kvp.Value);

        return results.ToArray();
    }

    /// <summary>
    /// Lista os codigos de idiomas disponiveis.
    /// </summary>
    public string[] GetAvailableLanguages()
    {
        var codes = new string[languageAssets.Count];
        languageAssets.Keys.CopyTo(codes, 0);
        return codes;
    }

    /// <summary>
    /// Retorna o nome legivel do idioma (do campo _meta.language).
    /// </summary>
    public string GetLanguageName(string code)
    {
        return GetText("_meta.language");
    }

    private void LoadLanguage(string languageCode)
    {
        currentTexts.Clear();

        if (!languageAssets.TryGetValue(languageCode, out TextAsset asset))
        {
            // Fallback para idioma padrao
            if (!languageAssets.TryGetValue(defaultLanguage, out asset))
            {
                Debug.LogError($"[Localization] Idioma nao encontrado: {languageCode} nem fallback {defaultLanguage}");
                return;
            }
            languageCode = defaultLanguage;
        }

        currentLanguageCode = languageCode;
        ParseJsonToFlatDictionary(asset.text, "");
    }

    private void ParseJsonToFlatDictionary(string json, string prefix)
    {
        // Parser simples de JSON para dicionario flat com chaves de ponto
        // Usa JsonUtility indiretamente via parsing manual para suportar nested objects
        var parsed = ParseJsonObject(json);
        FlattenDictionary(parsed, prefix);
    }

    private void FlattenDictionary(Dictionary<string, object> dict, string prefix)
    {
        foreach (var kvp in dict)
        {
            string fullKey = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

            if (kvp.Value is Dictionary<string, object> nested)
            {
                FlattenDictionary(nested, fullKey);
            }
            else if (kvp.Value is string strValue)
            {
                currentTexts[fullKey] = strValue;
            }
        }
    }

    // --- JSON Parser simples (sem dependencias externas) ---

    private Dictionary<string, object> ParseJsonObject(string json)
    {
        var result = new Dictionary<string, object>();
        int index = 0;
        SkipWhitespace(json, ref index);

        if (index >= json.Length || json[index] != '{') return result;
        index++; // skip {

        while (index < json.Length)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] == '}') break;
            if (json[index] == ',') { index++; continue; }

            string key = ParseString(json, ref index);
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ':') index++;
            SkipWhitespace(json, ref index);

            if (index < json.Length && json[index] == '{')
            {
                // Encontrar o objeto interno completo
                int start = index;
                int depth = 0;
                do
                {
                    if (json[index] == '{') depth++;
                    else if (json[index] == '}') depth--;
                    index++;
                } while (depth > 0 && index < json.Length);

                string subJson = json.Substring(start, index - start);
                result[key] = ParseJsonObject(subJson);
            }
            else if (index < json.Length && json[index] == '"')
            {
                result[key] = ParseString(json, ref index);
            }
            else
            {
                // Skip outros tipos (numeros, booleans, etc)
                while (index < json.Length && json[index] != ',' && json[index] != '}')
                    index++;
            }
        }

        return result;
    }

    private string ParseString(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (index >= json.Length || json[index] != '"') return "";
        index++; // skip opening "

        var sb = new System.Text.StringBuilder();
        while (index < json.Length && json[index] != '"')
        {
            if (json[index] == '\\' && index + 1 < json.Length)
            {
                index++;
                switch (json[index])
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    default: sb.Append(json[index]); break;
                }
            }
            else
            {
                sb.Append(json[index]);
            }
            index++;
        }

        if (index < json.Length) index++; // skip closing "
        return sb.ToString();
    }

    private void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
            index++;
    }

    private string GetSavedLanguage()
    {
        return PlayerPrefs.GetString("game_language", defaultLanguage);
    }
}
