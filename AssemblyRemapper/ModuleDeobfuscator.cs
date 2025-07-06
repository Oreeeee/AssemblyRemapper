using Mono.Cecil;

namespace AssemblyRemapper;

public class ModuleDeobfuscator
{
    private Dictionary<string, string> SymbolMap;
    private ModuleDefinition Module;

    public ModuleDeobfuscator(Dictionary<string, string> symbolMap, ModuleDefinition module)
    {
        SymbolMap = symbolMap;
        Module = module;
    }

    /// <summary>
    /// Deobfuscates types, nested types, members, parameters in specified module based on a module map
    /// </summary>
    public void Deobfuscate()
    {
        foreach (TypeDefinition type in Module.Types)
        {
            Logger.Verbose($"Renaming type {type.FullName}");
            RenameType(type);
        }
    }
    
    /// <summary>
    /// Renames type, all members, and nested types
    /// </summary>
    /// <param name="type">Type to rename</param>
    /// <param name="isNested">Whether the type is nested</param>
    void RenameType(TypeDefinition type, bool isNested = false)
    {
        if (!isNested)
        {
            // Non-nested types need to get full name (including namespace)
            string originalName = type.FullName;
            string cleanName = GetName(type.FullName);
            Logger.Verbose($"Renaming type {originalName} to {cleanName}");
            if (originalName != cleanName)
            {
                // We need to rename type (last index) and namespace (everything before last index) separately
                var lastDotIndex = cleanName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    type.Namespace = cleanName.Substring(0, lastDotIndex);
                    type.Name = cleanName.Substring(lastDotIndex + 1);
                }
                else
                {
                    type.Namespace = string.Empty;
                    type.Name = cleanName;
                }
                Logger.Verbose($"Renamed type {type.FullName}");
            }
        }
        else
        {
            // Nested types only need short name
            type.Name = GetName(type.Name);
            Logger.Verbose($"Renamed nested type {type.FullName}");
        }
        
        foreach (IMemberDefinition member in GetMembers(type))
        {
            Logger.Verbose($"Renaming member {member.Name}");
            RenameMember(member);
        }
    
        foreach (TypeDefinition nestedType in type.NestedTypes)
        {
            Logger.Verbose($"Renaming nested type {nestedType.Name}");
            RenameType(nestedType, true);
        }
    }
    
    /// <summary>
    /// Renames a type member and parameters if member is a MethodDefinition
    /// </summary>
    /// <param name="member">Member to rename</param>
    void RenameMember(IMemberDefinition member)
    {
        member.Name = GetName(member.Name);
        Logger.Verbose($"Renamed member {member.Name}");
    
        if (member is MethodDefinition method)
        {
            // Also rename parameters
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                Logger.Verbose($"Renaming parameter {parameter.Name}");
                parameter.Name = GetName(parameter.Name);
                Logger.Verbose($"Renamed parameter {parameter.Name}");
            }
        }
    }
    
    /// <summary>
    /// Yields all methods, fields, events, and properties in a type
    /// </summary>
    /// <param name="type">Target type</param>
    /// <returns></returns>
    IEnumerable<IMemberDefinition> GetMembers(TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            yield return method;
        }
    
        foreach (var field in type.Fields)
        {
            yield return field;
        }
    
        foreach (var _event in type.Events)
        {
            yield return _event;
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
    string GetName(string obfuscatedName)
    {
        SymbolMap.TryGetValue(obfuscatedName, out var cleanName);
        if (cleanName == null)
        {
            Logger.Verbose($"No name found for {obfuscatedName}");
            return obfuscatedName;
        }
        Logger.Verbose($"Name {cleanName} would be renamed to {cleanName}");
        return cleanName;
    }
}
