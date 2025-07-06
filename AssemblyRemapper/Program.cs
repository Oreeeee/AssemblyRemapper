using System.Text.Json;
using AssemblyRemapper;
using CommandLine;
using Mono.Cecil;

// Parse config
Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o => Options.Config = o);

// Read map file
Logger.Log("Reading map file");
string jsonMapData = File.ReadAllText(Options.Config.MapFile);
Dictionary<string, string>? map = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMapData);
if (map == null)
{
    throw new Exception("Couldn't parse JSON");
}

// Create resolver that includes module path for saving the output 
Logger.Verbose("Creating resolver");
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Options.Config.File));
var readerParameters = new ReaderParameters { AssemblyResolver = resolver };

Logger.Verbose("Loading assembly");
ModuleDefinition module = ModuleDefinition.ReadModule(Options.Config.File, readerParameters); // Read module

// Deobfuscate the module
Logger.Log("Deobfuscating");
ModuleDeobfuscator md = new ModuleDeobfuscator(map, module);
md.Deobfuscate();

// Write the deobfuscated module to disk
Logger.Log("Writing deobfuscated assembly");
module.Write(Options.Config.Output);

Logger.Log("Done");
