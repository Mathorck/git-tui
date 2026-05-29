using GitTui.Interfaces;

namespace GitTui.Utils;

public class Localizer : ILocalizer
{
    private const string LOCALIZED_DIR_PATH = "Ressources/Localization/";
    
    private string locale;
    private string[] langFiles;
    
    private HashSet<string> keys;
    
    public string Locale { get; set; }
    
    
    public async void Reload(string locale = "")
    {
        if (string.IsNullOrEmpty(locale))
            locale = this.locale;
        
        
        
        
    }

    private async void ReloadLangFiles()
    {
        
    }

    public string[] GetLocales()
    {
        throw new NotImplementedException();
    }

    public string this[string key] => throw new NotImplementedException();

    public string this[string key, params object[] arguments] => throw new NotImplementedException();

    public string Get(string key)
    {
        throw new NotImplementedException();
    }

    public string Get(string key, params object[] arguments)
    {
        throw new NotImplementedException();
    }

    public bool ContainsKey(string key)
    {
        throw new NotImplementedException();
    }
}