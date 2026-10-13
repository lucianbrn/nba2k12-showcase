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

// Lightweight source-contract checks cover showcase integrations that syntax parsing alone
// cannot verify. These are regression guards, not substitutes for Unity compilation or play tests.
var requiredSourceContracts = new[]
{
    (
        File: Path.Combine(sourceRoot, "ShowcaseBroadcastHud.cs"),
        Description: "HUD resolves references when enabled",
        Required: new[]
        {
            "private void OnEnable()",
            "ResolveReferences();",
            "scoreTracker.Scored -= HandleScore;",
            "scoreTracker.Scored += HandleScore;",
            "keyboard.enterKey.wasPressedThisFrame",
            "keyboard.numpadEnterKey.wasPressedThisFrame",
            "if (final) RestartGame();",
            "else if (periodEnded) NextPeriod();"
        }),
    (
        File: Path.Combine(sourceRoot, "ShowcaseGamepadSupport.cs"),
        Description: "Controller mode can switch back to keyboard/mouse",
        Required: new[]
        {
            "Mouse mouse = Mouse.current;",
            "bool mouseActive = mouse != null",
            "else if (keyboardActive || mouseActive)",
            "int next = current < 0",
            "match.SelectPlayer(eligible[next].PlayerIndex);"
        })
};

foreach (var contract in requiredSourceContracts)
{
    if (!File.Exists(contract.File))
    {
        Console.Error.WriteLine($"FAIL {contract.Description}: missing {Path.GetFileName(contract.File)}");
        errors++;
        continue;
    }

    string source = File.ReadAllText(contract.File);
    string[] missing = contract.Required.Where(fragment => !source.Contains(fragment, StringComparison.Ordinal)).ToArray();
    if (missing.Length == 0)
    {
        Console.WriteLine($"PASS contract: {contract.Description}");
        continue;
    }

    foreach (string fragment in missing)
        Console.Error.WriteLine($"FAIL {contract.Description}: expected source fragment not found: {fragment}");
    errors++;
}

Console.WriteLine();
Console.WriteLine($"Checked {sourceFiles.Length} C# files; syntax/contract errors: {errors}.");
Console.WriteLine("This validates C# syntax and selected source contracts only. It does not compile against Unity assemblies or test a Unity scene.");

return errors == 0 ? 0 : 1;
