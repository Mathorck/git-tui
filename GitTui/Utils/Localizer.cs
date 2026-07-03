using System.Xml.Linq;
using GitTui.Interfaces;

namespace GitTui.Utils;

public class Localizer : ILocalizer
{
    private const string LOCALIZED_DIR_PATH = "Ressources/Localization/";
    private const string FILE_PREFIX = "lang.";
    private const string FILE_EXTENSION = ".xml";
    private const string DEFAULT_LOCALE = "en";

    private readonly Dictionary<string, string> _translations = new();
    private readonly Dictionary<string, string> _defaultTranslations = new();
    private string _locale = string.Empty;
    private bool _defaultLoaded;

    public string Locale
    {
        get => _locale;
        set => Reload(value);
    }

    public void Reload(string locale = "")
    {
        if (string.IsNullOrEmpty(locale))
            locale = string.IsNullOrEmpty(_locale) ? DEFAULT_LOCALE : _locale;

        string path = GetLangFilePath(locale);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Localization file not found for locale '{locale}'.", path);

        LoadTranslationsFromFile(path, _translations);
        _locale = locale;

        if (locale == DEFAULT_LOCALE)
        {
            _defaultTranslations.Clear();
            foreach (KeyValuePair<string, string> kvp in _translations)
                _defaultTranslations[kvp.Key] = kvp.Value;
            _defaultLoaded = true;
        }
        else
        {
            EnsureDefaultLoaded();
        }
    }

    private void EnsureDefaultLoaded()
    {
        if (_defaultLoaded)
            return;

        string path = GetLangFilePath(DEFAULT_LOCALE);
        if (File.Exists(path))
            LoadTranslationsFromFile(path, _defaultTranslations);

        _defaultLoaded = true;
    }

    private static void LoadTranslationsFromFile(string path, Dictionary<string, string> target)
    {
        target.Clear();

        XDocument document = XDocument.Load(path);
        foreach (XElement trad in document.Root?.Elements("trad") ?? Enumerable.Empty<XElement>())
        {
            string? name = trad.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name))
                continue;

            target[name] = trad.Attribute("content")?.Value ?? string.Empty;
        }
    }

    public string[] GetLocales()
    {
        string dir = GetLocalizationDirectory();
        if (!Directory.Exists(dir))
            return [];

        return Directory.GetFiles(dir, $"{FILE_PREFIX}*{FILE_EXTENSION}")
            .Select(path => Path.GetFileNameWithoutExtension(path)[FILE_PREFIX.Length..])
            .ToArray();
    }

    public string this[string key] => Get(key);

    public string this[string key, params object[] arguments] => Get(key, arguments);

    public string Get(string key)
    {
        if (_translations.TryGetValue(key, out string? value))
            return value;

        if (_defaultTranslations.TryGetValue(key, out string? fallback))
            return fallback;

        return key;
    }

    public string Get(string key, params object[] arguments)
    {
        return string.Format(Get(key), arguments);
    }

    public bool ContainsKey(string key)
    {
        return _translations.ContainsKey(key) || _defaultTranslations.ContainsKey(key);
    }

    private static string GetLocalizationDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, LOCALIZED_DIR_PATH);
    }

    private static string GetLangFilePath(string locale)
    {
        return Path.Combine(GetLocalizationDirectory(), $"{FILE_PREFIX}{locale}{FILE_EXTENSION}");
    }
}
