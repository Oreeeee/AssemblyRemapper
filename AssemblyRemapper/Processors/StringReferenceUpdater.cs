using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyRemapper.Processors;

/// <summary>
/// Replaces all string references to obfuscated types to their unobfuscated names
/// </summary>
/// <param name="symbolMap"></param>
/// <param name="module"></param>
public class StringReferenceUpdater(Dictionary<string, string> symbolMap, ModuleDefinition module): Processor(symbolMap, module)
{
    public override void Process()
    {
        Logger.Verbose("Replacing IL strings");
        ReplaceAllIlStrings();

        Logger.Verbose("Replacing attributes strings");
        ReplaceAllAttributesStrings();
    }

    /// <summary>
    /// Replaces all strings in IL defined with ldstr
    /// </summary>
    void ReplaceAllIlStrings()
    {
        foreach (TypeDefinition type in module.Types)
        {
            ReplaceAllIlStringsInType(type);

            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                ReplaceAllIlStringsInType(nestedType);
            }
        }
    }

    /// <summary>
    /// Replaces all IL strings in type
    /// </summary>
    /// <param name="type"></param>
    void ReplaceAllIlStringsInType(TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.HasBody)
            {
                for (int i = 0; i < method.Body.Instructions.Count; i++)
                {
                    var instruction = method.Body.Instructions[i];

                    if (instruction.OpCode == OpCodes.Ldstr)
                    {
                        string str = instruction.Operand as string;
                        if (!string.IsNullOrEmpty(str) && Regex.IsMatch(str, Options.Config.ObfuscatedRegex))
                        {
                            string replacedStr = Regex.Replace(str, Options.Config.ObfuscatedRegex, match => GetName(match.ToString()));
                            Logger.Verbose($"Replacing IL string \"{str}\" with \"{replacedStr}\"");
                            method.Body.Instructions[i] = Instruction.Create(OpCodes.Ldstr, replacedStr);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Replaces all strings in attributes
    /// </summary>
    void ReplaceAllAttributesStrings()
    {
        // Get strings from assembly attributes
        foreach (var attr in module.Assembly.CustomAttributes)
        {
            ReplaceAllStringsInAttribute(attr);
        }
        
        // Get strings from module attributes
        foreach (var attr in module.CustomAttributes)
        {
            ReplaceAllStringsInAttribute(attr);
        }
        
        // Get strings from type and member attributes
        foreach (var type in module.Types)
        {
            ReplaceAllAttributeStringsInType(type);

            foreach (var nestedType in type.NestedTypes)
            {
                ReplaceAllAttributeStringsInType(nestedType);
            }
        }
    }

    /// <summary>
    /// Replaces all strings in attributes in type
    /// </summary>
    /// <param name="type"></param>
    void ReplaceAllAttributeStringsInType(TypeDefinition type)
    {
        foreach (var attr in type.CustomAttributes)
        {
            ReplaceAllStringsInAttribute(attr);
        }

        foreach (var method in type.Methods)
        {
            foreach (var attr in method.CustomAttributes)
            {
                ReplaceAllStringsInAttribute(attr);
            }
        }

        foreach (var field in type.Fields)
        {
            foreach (var attr in field.CustomAttributes)
            {
                ReplaceAllStringsInAttribute(attr);
            }
        }

        foreach (var property in type.Properties)
        {
            foreach (var attr in property.CustomAttributes)
            {
                ReplaceAllStringsInAttribute(attr);
            }
        }
    }

    /// <summary>
    /// Replaces all strings in attributes
    /// </summary>
    /// <param name="attr"></param>
    void ReplaceAllStringsInAttribute(CustomAttribute attr)
    {
        // Constructor arguments
        for (int i = 0; i < attr.ConstructorArguments.Count; i++)
        {
            CustomAttributeArgument arg = attr.ConstructorArguments[i];
            
            if (arg.Value is string str && !string.IsNullOrEmpty(str) && Regex.IsMatch(str, Options.Config.ObfuscatedRegex))
            {
                string newVal = Regex.Replace(str, Options.Config.ObfuscatedRegex, match => GetName(match.ToString()));
                Logger.Verbose($"Replacing construction argument string \"{str}\" with \"{newVal}\"");
                CustomAttributeArgument newArg = new CustomAttributeArgument(arg.Type, newVal);
                attr.ConstructorArguments[i] = newArg;
            }
        }
        
        // Named arguments
        for (int i = 0; i < attr.Properties.Count; i++)
        {
            CustomAttributeNamedArgument namedArg = attr.Properties[i];
            
            if (namedArg.Argument.Value is string str && !string.IsNullOrEmpty(str) && Regex.IsMatch(str, Options.Config.ObfuscatedRegex))
            {
                string newVal = Regex.Replace(str, Options.Config.ObfuscatedRegex, match => GetName(match.ToString()));
                Logger.Verbose($"Replacing named argument string \"{str}\" with \"{newVal}\"");
                CustomAttributeArgument newArg = new CustomAttributeArgument(namedArg.Argument.Type, newVal);
                CustomAttributeNamedArgument newNamedArg = new CustomAttributeNamedArgument(namedArg.Name, newArg);
                attr.Properties[i] = newNamedArg;
            }
        }

        for (int i = 0; i < attr.Fields.Count; i++)
        {
            CustomAttributeNamedArgument namedArg = attr.Fields[i];
            
            if (namedArg.Argument.Value is string str && !string.IsNullOrEmpty(str) && Regex.IsMatch(str, Options.Config.ObfuscatedRegex))
            {
                string newVal = Regex.Replace(str, Options.Config.ObfuscatedRegex, match => GetName(match.ToString()));
                Logger.Verbose($"Replacing named argument string \"{str}\" with \"{newVal}\"");
                CustomAttributeArgument newArg = new CustomAttributeArgument(namedArg.Argument.Type, newVal);
                CustomAttributeNamedArgument newNamedArg = new CustomAttributeNamedArgument(namedArg.Name, newArg);
                attr.Properties[i] = newNamedArg;
            }
        }
    }
}
