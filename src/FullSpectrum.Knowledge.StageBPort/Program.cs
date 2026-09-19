namespace FullSpectrum.Knowledge.StageBPort;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args is not ["--database", var databasePath] || string.IsNullOrWhiteSpace(databasePath)) return 64;
        try
        {
            using var session = new FdeStageBPortSession(databasePath);
            string? line;
            while ((line = Console.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) return 65;
                Console.WriteLine(session.ProcessLine(line));
            }
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
