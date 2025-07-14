using System.Text.RegularExpressions;
using Mono.Cecil;

namespace AssemblyRemapper.Processors;

public abstract class Processor(Dictionary<string, string> symbolMap, ModuleDefinition module)
{
    public abstract void Process();
    
    protected Regex? ObfuscatedRegex;

    /// <summary>
    /// Checks whether a symbol name matches the obfuscated regex provided
    /// If no obfuscated regex is provided, then always returns true
    /// </summary>
    /// <param name="name">Symbol name to check</param>
    /// <returns>If name matches obfuscated regex</returns>
    protected bool IsObfuscated(string name)
    {
        if (Options.Config.ObfuscatedRegex == "") return true;
        
        ObfuscatedRegex ??= new Regex(Options.Config.ObfuscatedRegex, RegexOptions.Compiled);
        
        bool match = ObfuscatedRegex.IsMatch(name);
        Logger.Verbose(match ? $"{name} matches with obfuscated regex" : $"{name} doesn't match with obfuscated regex");
        
        return match;
    }
    
    /// <summary>
    /// Yields all methods, fields, events, and properties in a type
    /// </summary>
    /// <param name="type">Target type</param>
    /// <returns></returns>
    protected IEnumerable<IMemberDefinition> GetMembers(TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            yield return method;
        }
    
        foreach (var field in type.Fields)
        {
            yield return field;
        }
    
        foreach (var evnt in type.Events)
        {
            yield return evnt;
        }
    
        foreach (var property in type.Properties)
        {
            yield return property;
        }
    }
    
    /// <summary>
    /// Gets name of an obfuscated symbol from the map
    /// </summary>
    /// <param name="obfuscatedName">Name to get</param>
    /// <returns>Deobfuscated name if present, obfuscated name if not</returns>
    protected string GetName(string obfuscatedName)
    {
        symbolMap.TryGetValue(obfuscatedName, out var cleanName);
        if (cleanName == null)
        {
            Logger.Verbose($"No name found for {obfuscatedName}");
            return obfuscatedName;
        }
        Logger.Verbose($"Name {cleanName} would be renamed to {cleanName}");
        return cleanName;
    }
}
