namespace GitTui.Interfaces;

public interface ILocalizer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="locale">If empty takes the last one</param>
    public void Reload(string locale = "");
    
    public string Locale { get; set; }
    public string[] GetLocales();
    
    public string this[string key] { get; }
    public string this[string key, params object[] arguments] { get; }
    
    public string Get(string key);
    public string Get(string key, params object[] arguments);
    
    public bool ContainsKey(string key);
    
}