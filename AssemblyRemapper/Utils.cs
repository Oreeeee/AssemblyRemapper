using System.Text.RegularExpressions;

namespace AssemblyRemapper;

public static class Utils
{
    private static Regex? obfuscatedRegex;

    /// <summary>
    /// Checks whether a symbol name matches the obfuscated regex provided
    /// If no obfuscated regex is provided, then always returns true
    /// </summary>
    /// <param name="name">Symbol name to check</param>
    /// <returns>If name matches obfuscated regex</returns>
    public static bool IsObfuscated(string name)
    {
        if (Options.Config.ObfuscatedRegex == "") return true;
        
        if (obfuscatedRegex == null)
            obfuscatedRegex = new Regex(Options.Config.ObfuscatedRegex, RegexOptions.Compiled);
        
        bool match = obfuscatedRegex.IsMatch(name);
        Logger.Verbose(match ? $"{name} matches with obfuscated regex" : $"{name} doesn't match with obfuscated regex");
        
        return match;
    }
}
