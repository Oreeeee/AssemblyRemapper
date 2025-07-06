namespace AssemblyRemapper;

public static class Logger
{
    public static void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
    
    public static void Verbose(string message)
    {
        if (!Options.Config.Verbose) return;
        Console.WriteLine($"[VERBOSE] {message}");
    }
}
