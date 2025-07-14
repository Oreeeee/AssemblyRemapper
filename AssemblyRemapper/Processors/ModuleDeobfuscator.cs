using Mono.Cecil;

namespace AssemblyRemapper.Processors;

public class ModuleDeobfuscator(Dictionary<string, string> symbolMap, ModuleDefinition module) : Processor(symbolMap, module)
{
    /// <summary>
    /// Deobfuscates types, nested types, members, parameters in specified module based on a module map
    /// </summary>
    public override void Process()
    {
        foreach (TypeDefinition type in module.Types)
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
        if (!isNested && IsObfuscated(type.FullName))
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
        else if (IsObfuscated(type.Name))
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

        // Rename generic params in class
        foreach (GenericParameter genericParam in type.GenericParameters)
        {
            if (!IsObfuscated(genericParam.Name)) continue;
            
            Logger.Verbose($"Renaming generic parameter {genericParam.Name}");
            genericParam.Name = GetName(genericParam.Name);
        }
    }
    
    /// <summary>
    /// Renames a type member and parameters if member is a MethodDefinition
    /// </summary>
    /// <param name="member">Member to rename</param>
    void RenameMember(IMemberDefinition member)
    {
        if (IsObfuscated(member.Name))
        {
            member.Name = GetName(member.Name);
            Logger.Verbose($"Renamed member {member.Name}");
        }
        
        if (member is MethodDefinition method)
        {
            // Also rename parameters
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                if (!IsObfuscated(parameter.Name)) continue;
                
                Logger.Verbose($"Renaming parameter {parameter.Name}");
                parameter.Name = GetName(parameter.Name);
                //Logger.Verbose($"Renamed parameter {parameter.Name}");
            }

            // Rename generic params in method
            foreach (GenericParameter genericParam in method.GenericParameters)
            {
                if (!IsObfuscated(genericParam.Name)) continue;
                
                Logger.Verbose($"Renaming generic parameter {genericParam.Name}");
                genericParam.Name = GetName(genericParam.Name);
            }
        }
    }
}
