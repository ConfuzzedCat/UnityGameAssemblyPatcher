using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Serilog;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityGameAssemblyPatcher.PatchFramework;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class CodeCompiler
    {
        internal CodeCompiler()
        {
            logger = Logging.GetInstance();
        }
        private readonly ILogger logger;
        private readonly HashSet<PortableExecutableReference> References = new();


        internal Assembly? Compile(string sourceFilePath, string gamePath)
        {           
            string sourceAsText = File.ReadAllText(sourceFilePath);
            if (string.IsNullOrEmpty(sourceAsText))
            {
                throw new IOException("Patch file was found, but it was empty.");
            }
            AddNetCoreDefaultReferences();
            AddPatchReferences(sourceFilePath);
            AddGameReferences();
            AddAssembly(typeof(ACodeInjection));

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceAsText.Trim());
            CSharpCompilation compilation = CSharpCompilation.Create("TempPatchSource.cs")
                .WithOptions(new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary, 
                    optimizationLevel: OptimizationLevel.Release))
                .WithReferences(References)
                .AddSyntaxTrees(syntaxTree);

            string errorMessage = string.Empty;

            using (Stream codeStream = new MemoryStream())
            {
                EmitResult compilationResult = compilation.Emit(codeStream);

                if (!compilationResult.Success)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach(var diag in compilationResult.Diagnostics)
                    {
                        sb.AppendLine(diag.ToString());
                    }
                    errorMessage = sb.ToString();
                    logger.Error("There was an error compiling the file: \"{0}\".\nError message: {1}", sourceFilePath, errorMessage);
                    return null;
                }
                return Assembly.Load(((MemoryStream)codeStream).ToArray());
            }
        }

        private void AddAssemblies(params string[] assemblies)
        {
            for (int i = 0; i < assemblies.Length; i++)
            {
                AddAssembly(assemblies[i]);
            }
        }

        private bool AddAssembly(string assemblyDll)
        {
            if (string.IsNullOrEmpty(assemblyDll)) 
                return false;

            string file = Path.GetFullPath(assemblyDll);

            if (!File.Exists(file))
            {
                string? path = Path.GetDirectoryName(typeof(object).Assembly.Location);
                file = Path.Combine(path, assemblyDll);
                if(!File.Exists(file))
                    return false;
            }

            if(References.Any(r => r.FilePath == file))
                return true;

            try
            {
                PortableExecutableReference reference = MetadataReference.CreateFromFile(file);
                References.Add(reference);
            }
            catch
            {

                return false;
            }

            return true;
        }

        private bool AddAssembly(Type type)
        {
            try
            {
                if (References.Any(r => r.FilePath == type.Assembly.Location))
                    return true;

                PortableExecutableReference systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
                References.Add(systemReference);
            }
            catch
            {

                return false;
            }

            return true;
        }

        private void AddGameReferences(string gamePath)
        {
            string lastFolderPath = Path.GetDirectoryName(gamePath).Split(Path.DirectorySeparatorChar).Last();
            if (!lastFolderPath.Equals("Managed"))
                return;

            AddAssemblies(Directory.GetFiles(gamePath, "*.dll"));
            throw new NotImplementedException();
        }


        private void AddPatchReferences(string file)
        {
            string[] references = Utils.ParsePatchLibraryReferences(file);

            if(references.Length == 0) 
                return;
            AddAssemblies(references);
        }

        private void AddNetCoreDefaultReferences()
        {
            string rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                 Path.DirectorySeparatorChar;

            AddAssemblies(
                rtPath + "System.Private.CoreLib.dll",
                rtPath + "System.Runtime.dll",
                rtPath + "System.Console.dll",
                rtPath + "netstandard.dll",

                rtPath + "System.Text.RegularExpressions.dll",
                rtPath + "System.Linq.dll",
                rtPath + "System.Linq.Expressions.dll",

                rtPath + "System.IO.dll",
                rtPath + "System.Net.Primitives.dll",
                rtPath + "System.Net.Http.dll",
                rtPath + "System.Private.Uri.dll",
                rtPath + "System.Reflection.dll",
                rtPath + "System.ComponentModel.Primitives.dll",
                rtPath + "System.Globalization.dll",
                rtPath + "System.Collections.Concurrent.dll",
                rtPath + "System.Collections.NonGeneric.dll",
                rtPath + "Microsoft.CSharp.dll"
            );
        }
    }
}
