using CommandLine;

namespace AssemblyRemapper;

public class Options
{
    [Option("file", Required = true, HelpText = "Path to module to deobfuscate")]
    public string File { get; set; }
    
    [Option("map-file", Required = true, HelpText = "JSON map file")]
    public string MapFile { get; set; }
    
    [Option("output", Required = true, HelpText = "Path to deobfuscated output")]
    public string Output { get; set; }

    [Option("verbose", Required = false, HelpText = "Print verbose logs", Default = false)]
    public bool Verbose { get; set; }
    
    [Option("hide-ref-fix-exceptions", Required = false, HelpText = "Hide reference fixer exceptions. They might be spammy but they shouldn't occur", Default = false)]
    public bool HideRefFixExceptions { get; set; }
    
    [Option("obfuscated-regex", Required = false, HelpText = "Regex for obfuscated symbol names. By default it matches all names (slow)", Default = "")]
    public string ObfuscatedRegex { get; set; }

    public static Options Config = new Options();
}
