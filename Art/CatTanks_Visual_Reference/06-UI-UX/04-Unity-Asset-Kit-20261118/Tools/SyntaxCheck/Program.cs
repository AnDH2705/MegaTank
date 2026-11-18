using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var root = args.Length > 0 ? args[0] : throw new ArgumentException("Pass the kit's Unity/Assets folder.");
int errors = 0, count = 0;
foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
{
    count++;
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9), path);
    foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    { Console.WriteLine(diagnostic); errors++; }
}
Console.WriteLine("C# syntax files=" + count + ", errors=" + errors + ". This is not Unity API compilation or a Play Mode test.");
return errors == 0 ? 0 : 1;
