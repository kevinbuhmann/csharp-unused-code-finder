using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vstack.Common.Extensions;

namespace UnusedCodeFinder
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            args.ValidateNotNull();

            foreach (string solutionPath in args)
            {
                if (solutionPath.EndsWith(".sln") && File.Exists(solutionPath))
                {
                    Console.WriteLine($"Processing solution {solutionPath}... ");

                    FindUnusedCode(solutionPath).Wait();
                }
                else
                {
                    Console.WriteLine($"Error: Either {solutionPath} does not exit or it is not an solution file.");
                    Console.WriteLine();
                }
            }

            Console.Write("Press enter to exit...");
            Console.ReadLine();
        }

        private static async Task FindUnusedCode(string solutionPath)
        {
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = await workspace.OpenSolutionAsync(solutionPath);

                IEnumerable<KeyValuePair<string, MemberDeclarationSyntax>> allUnusedDeclarations = await solution.Projects
                    .SelectMany(project => project.Documents)
                    .Where(document => document.FilePath.EndsWith(".cs"))
                    .SelectManyAsync(document => FindUnusedDeclarations(solution, document));

                IEnumerable<IGrouping<string, MemberDeclarationSyntax>> unusedDeclarationByFile = allUnusedDeclarations
                    .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                    .OrderBy(group => group.Key)
                    .AsEnumerable();

                foreach (IGrouping<string, MemberDeclarationSyntax> unusedDeclarations in unusedDeclarationByFile)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Unused declarations in {unusedDeclarations.Key}:");

                    foreach (MemberDeclarationSyntax unusedDeclaration in unusedDeclarations)
                    {
                        Console.WriteLine($"{unusedDeclaration.GetType().Name}: {unusedDeclaration.GetIdentifer()}");
                    }
                }
            }
        }

        private static async Task<IEnumerable<KeyValuePair<string, MemberDeclarationSyntax>>> FindUnusedDeclarations(Solution solution, Document document)
        {
            SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
            SemanticModel semanticModel = await document.GetSemanticModelAsync();

            IEnumerable<MemberDeclarationSyntax> declarations = new MemberDeclarationSyntax[] { }
                .Concat(syntaxRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().Cast<MemberDeclarationSyntax>())
                .Concat(syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Cast<MemberDeclarationSyntax>());

            List<MemberDeclarationSyntax> unusedDeclarations = new List<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax declaration in declarations)
            {
                ISymbol symbol = semanticModel.GetDeclaredSymbol(declaration);
                IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(symbol, solution);

                if (references.Where(reference => reference.Locations.Any()).Any() == false)
                {
                    unusedDeclarations.Add(declaration);
                }
            }

            return unusedDeclarations
                .Select(declaration => new KeyValuePair<string, MemberDeclarationSyntax>(document.FilePath, declaration));
        }
    }
}
