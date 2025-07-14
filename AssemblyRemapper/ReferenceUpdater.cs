using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
        //foreach (TypeReference reference in type.GetTypeRe)
        
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
                        //FixGenericInstanceMethod(instruction, generic);
                        //generic.ElementMethod.Name = GetName(generic.ElementMethod.Name);
                        FixMethodReference(generic.ElementMethod);
                        break;
                    case MethodReference methodRef:
                        //methodRef.Name = GetName(methodRef.Name);
                        FixMethodReference(methodRef);
                        break;
                    case FieldReference fieldRef:
                        fieldRef.Name = GetName(fieldRef.Name);
                        break;
                    // case TypeReference typeRef:
                    //     typeRef.Name = GetName(typeRef.Name);
                    //     break;
                    // case MemberReference memberRef:
                    //     memberRef.DeclaringType.Name = GetName(memberRef.Name);
                    //     memberRef.Name = GetName(memberRef.Name);
                    //     break;
                }
                
                // if (instruction.Operand is MemberReference member)
                // {
                //     member.Name = GetName(member.Name);
                // }
                // if (instruction.Operand is MethodReference methodRef)
                // {
                //     methodRef.Name = GetName(methodRef.Name);
                // }
                // else if (instruction.Operand is TypeReference typeRef)
                // {
                //     typeRef.Name = GetName(typeRef.Name);
                // }
                // else if (instruction.Operand is FieldReference fieldRef)
                // {
                //     fieldRef.Name = GetName(fieldRef.Name);
                // }
                // else if (instruction.Operand is PropertyReference propertyRef)
                // {
                //     propertyRef.Name = GetName(propertyRef.Name);
                // }
                // else if (instruction.Operand is EventReference eventRef)
                // {
                //     eventRef.Name = GetName(eventRef.Name);
                // }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to fix reference (ignore this usually): {e}");
            }
        }
    }

    void FixMethodReference(MethodReference methodRef)
    {
        methodRef.Name = GetName(methodRef.Name);
        FixTypeReference(methodRef.DeclaringType);
    }

    void FixTypeReference(TypeReference typeRef)
    {
        //typeRef.Name = GetName(typeRef.Name);
        //typeRef.Namespace = GetName(typeRef.Name);
        //typeRef.FullName = GetName(typeRef.FullName);
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
