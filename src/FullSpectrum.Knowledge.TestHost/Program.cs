using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.TestHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return args.Length == 0 ? VerifyRepository() : Dispatch(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[FAIL] {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static int Dispatch(string[] args) => args[0] switch
    {
        "verify" when args.Length == 1 => VerifyRepository(),
        "digest" when args.Length == 2 => PrintDigest(args[1]),
        "validate" when args.Length == 3 => Validate(args[1], args[2]),
        _ => Usage()
    };

    private static int VerifyRepository()
    {
        var root = FindRepositoryRoot();
        var schemaFiles = Directory.GetFiles(
            Path.Combine(root, "schemas", "knowledge", "v1.0"),
            "*.schema.json",
            SearchOption.TopDirectoryOnly);
        var errors = new List<string>();
        foreach (var file in schemaFiles)
        {
            errors.AddRange(SchemaSubsetValidator.AuditSchemaDocument(File.ReadAllText(file))
                .Select(error => $"{Path.GetFileName(file)}: {error}"));
        }

        var fixture = Path.Combine(root, "examples", "k0-01", "knowledge-pack.synthetic.json");
        var packSchema = Path.Combine(root, "schemas", "knowledge", "v1.0", "knowledge-pack.schema.json");
        errors.AddRange(SchemaSubsetValidator.Validate(
                File.ReadAllText(fixture),
                File.ReadAllText(packSchema))
            .Select(error => $"fixture: {error}"));

        var digest = DeterministicJson.ComputeSha256(File.ReadAllText(fixture));
        var output = new
        {
            status = errors.Count == 0 ? "PASS" : "FAIL",
            schema_dialect = "https://json-schema.org/draft/2020-12/schema",
            schema_documents = schemaFiles.Length,
            fixture = "examples/k0-01/knowledge-pack.synthetic.json",
            fixture_sha256 = digest.Value,
            errors
        };
        Console.WriteLine(JsonSerializer.Serialize(output, KnowledgeJson.Options));
        return errors.Count == 0 ? 0 : 1;
    }

    private static int PrintDigest(string path)
    {
        Console.WriteLine(DeterministicJson.ComputeSha256(File.ReadAllText(path)).Value);
        return 0;
    }

    private static int Validate(string instancePath, string schemaPath)
    {
        var errors = SchemaSubsetValidator.Validate(
            File.ReadAllText(instancePath),
            File.ReadAllText(schemaPath));
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }
        Console.WriteLine(errors.Count == 0 ? "PASS" : "FAIL");
        return errors.Count == 0 ? 0 : 1;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FullSpectrum.Knowledge.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: verify | digest <json> | validate <instance> <schema>");
        return 2;
    }
}
