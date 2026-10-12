using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/CSharpSyntaxCheck -- <repository-root>");
    return 2;
}

string repositoryRoot = Path.GetFullPath(args[0]);
string sourceRoot = Path.Combine(repositoryRoot, "patches", "Assets", "NBA2K12Showcase");
if (!Directory.Exists(sourceRoot))
{
    Console.Error.WriteLine($"Patch source directory not found: {sourceRoot}");
    return 2;
}

string[] sourceFiles = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
    .Where(path => !Path.GetFileName(path).EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (sourceFiles.Length == 0)
{
    Console.Error.WriteLine($"No C# source files found under {sourceRoot}");
    return 2;
}

var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Parse, SourceCodeKind.Regular);
int errors = 0;

foreach (string file in sourceFiles)
{
    string source = File.ReadAllText(file);
    SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, file);
    Diagnostic[] syntaxErrors = tree.GetDiagnostics()
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        .ToArray();

    if (syntaxErrors.Length == 0)
    {
        Console.WriteLine($"PASS {Path.GetRelativePath(repositoryRoot, file)}");
        continue;
    }

    errors += syntaxErrors.Length;
    foreach (Diagnostic diagnostic in syntaxErrors)
        Console.Error.WriteLine(diagnostic.ToString());
}

Console.WriteLine();
Console.WriteLine($"Checked {sourceFiles.Length} C# files; syntax errors: {errors}.");
Console.WriteLine("This validates C# syntax only. It does not compile against Unity assemblies or test a Unity scene.");

return errors == 0 ? 0 : 1;
