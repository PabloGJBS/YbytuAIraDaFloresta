using UnityEngine;
using System;
using System.Collections.Generic;

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
            languageAssets[asset.name] = asset;
        }
    }

    public void SetLanguage(string languageCode)
    {
        LoadLanguage(languageCode);
        PlayerPrefs.SetString("game_language", languageCode);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(languageCode);
    }

    public string GetText(string key)
    {
        if (currentTexts.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[Localization] Chave nao encontrada: {key}");
        return $"[{key}]";
    }

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

    public string[] GetSection(string prefix)
    {
        var results = new List<string>();
        var sorted = new SortedDictionary<string, string>();

        foreach (var kvp in currentTexts)
        {
            if (kvp.Key.StartsWith(prefix + "."))
            {
                string subKey = kvp.Key.Substring(prefix.Length + 1);
                if (!subKey.Contains("."))
                    sorted[subKey] = kvp.Value;
            }
        }

        foreach (var kvp in sorted)
            results.Add(kvp.Value);

        return results.ToArray();
    }

    public string[] GetAvailableLanguages()
    {
        var codes = new string[languageAssets.Count];
        languageAssets.Keys.CopyTo(codes, 0);
        return codes;
    }

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
