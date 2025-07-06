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

    public static Options Config = new Options();
}
