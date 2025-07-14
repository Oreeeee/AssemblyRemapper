using Mono.Cecil;

namespace AssemblyRemapper.Processors;

/// <summary>
/// Updates references in module
/// </summary>
/// <param name="symbolMap"></param>
/// <param name="module"></param>
public class ReferenceUpdater(Dictionary<string, string> symbolMap, ModuleDefinition module) : Processor(symbolMap, module)
{
    private readonly ModuleDefinition _module = module;

    public override void Process()
    {
        foreach (TypeDefinition type in _module.Types)
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
            if (!IsObfuscated(methodOverride.Name)) continue;
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
                        if (!IsObfuscated(fieldRef.Name)) break;
                        fieldRef.Name = GetName(fieldRef.Name);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.RefFixException(e);
            }
        }
    }

    void FixMethodReference(MethodReference methodRef)
    {
        if (IsObfuscated(methodRef.Name))
            methodRef.Name = GetName(methodRef.Name);
        
        FixTypeReference(methodRef.DeclaringType);
    }

    void FixTypeReference(TypeReference typeRef)
    {
        if (IsObfuscated(typeRef.FullName))
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
}
