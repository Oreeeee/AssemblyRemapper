using Mono.Cecil;

namespace AssemblyRemapper;

public class ReferenceUpdater(Dictionary<string, string> symbolMap, ModuleDefinition module)
{
    private Dictionary<string, string> SymbolMap = symbolMap;
    private ModuleDefinition Module = module;

    public void Process()
    {
        foreach (TypeDefinition type in Module.Types)
        {
            Logger.Verbose($"Fixing references in type {type.FullName}");
            FixType(type);
        }
    }

    void FixType(TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            Logger.Verbose($"Fixing references in method {method.FullName}");
            FixMethod(method);
        }

        foreach (TypeDefinition nestedType in type.NestedTypes)
        {
            Logger.Verbose($"Fixing references in nested type {nestedType.FullName}");
            FixType(nestedType);
        }
    }

    void FixMethod(MethodDefinition method)
    {
        foreach (MethodReference methodOverride in method.Overrides)
        {
            if (!Utils.IsObfuscated(methodOverride.Name)) continue;
            methodOverride.Name = GetName(methodOverride.Name);
        }
        
        if (!method.HasBody)
            return;
        
        foreach (var instruction in method.Body.Instructions)
        {
            try
            {
                if (instruction.Operand == null) continue;

                switch (instruction.Operand)
                {
                    case GenericInstanceMethod generic:
                        FixMethodReference(generic.ElementMethod);
                        break;
                    case MethodReference methodRef:
                        FixMethodReference(methodRef);
                        break;
                    case FieldReference fieldRef:
                        if (!Utils.IsObfuscated(fieldRef.Name)) break;
                        fieldRef.Name = GetName(fieldRef.Name);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to fix reference (ignore this usually): {e}");
            }
        }
    }

    void FixMethodReference(MethodReference methodRef)
    {
        if (Utils.IsObfuscated(methodRef.Name))
            methodRef.Name = GetName(methodRef.Name);
        
        FixTypeReference(methodRef.DeclaringType);
    }

    void FixTypeReference(TypeReference typeRef)
    {
        if (Utils.IsObfuscated(typeRef.FullName))
        {
            string originalName = typeRef.FullName;
            string cleanName = GetName(typeRef.FullName);
            if (originalName != cleanName)
            {
                // We need to rename type (last index) and namespace (everything before last index) separately
                var lastDotIndex = cleanName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    typeRef.Namespace = cleanName.Substring(0, lastDotIndex);
                    typeRef.Name = cleanName.Substring(lastDotIndex + 1);
                }
                else
                {
                    typeRef.Namespace = string.Empty;
                    typeRef.Name = cleanName;
                }
                Logger.Verbose($"Renamed typeRef {typeRef.FullName}");
            }
        }
        
        if (typeRef.DeclaringType != null)
            FixTypeReference(typeRef.DeclaringType);
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
