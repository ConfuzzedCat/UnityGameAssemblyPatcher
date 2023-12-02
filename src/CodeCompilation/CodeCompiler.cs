using HarmonyLib.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Serilog;
using System.Reflection;
using System.Text;
using UnityGameAssemblyPatcher.PatchFramework;
using UnityGameAssemblyPatcher.src.Exceptions;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class CodeCompiler
    {
        internal CodeCompiler()
        {
            logger = Logging.GetLogger<CodeCompiler>();
        }

        private const string ErrorMessageTemplate = "There was an error compiling the file: {0}.{1}";
        private const string CompileMessageTemplate = "Compiled patch: {0} - checksum:{1}";
        private readonly ILogger logger;
        private readonly HashSet<PortableExecutableReference> References = new();


        internal PatchInfo Compile(string sourceFilePath, string gamePath, string gameTargetVersion)
        {
            string checksum = Utils.CalculateMD5Checksum(sourceFilePath);

            var info = Utils.ParsePatchComments(sourceFilePath, checksum);
            string compiledPatchFile = Path.Combine(Directory.GetCurrentDirectory(), info.name + ".dll");

            string patchName = info.name;
            string patchDesc = string.Empty;
            if (!string.IsNullOrEmpty(gamePath))
            {
                string compiledPatchPath = Path.Combine(gamePath, "CompiledPatches");
                if (!Directory.Exists(compiledPatchPath))
                    throw new IOException("CompiledPatches folder should exist, but it does not. Was it deleted externally?");

                compiledPatchFile = Path.Combine(compiledPatchPath, info.name + ".dll");

                if (File.Exists(compiledPatchFile+".md5"))
                {
                    string compiledChecksum = File.ReadAllText(compiledPatchFile + ".md5");
                    if(compiledChecksum.Equals(checksum, StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.Information("patch isn't modified, loaded already compiled patch.");
                        Assembly _assembly = Assembly.LoadFrom(compiledPatchFile);
                        return new PatchInfo(patchName, patchDesc, checksum, _assembly);
                    }
                }
            }

            Console.WriteLine("getting sources code");
            string sourceAsText = Utils.GetSourceCode(sourceFilePath, info.targetFramework);
            if (string.IsNullOrEmpty(sourceAsText))
            {
                throw new IOException("Patch file was found, but it was empty.");
            }

            // Base required references for the patch
            AddPatchFrameworkRefereneces();

            AddNetCoreDefaultReferences();

            // Patch referenced libraries
            AddPatchReferences(info.references);

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceAsText.Trim());
            CSharpCompilation compilation = CSharpCompilation.Create(patchName)
                .WithOptions(new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release))
                .WithReferences(References)
                .AddSyntaxTrees(syntaxTree);
            
            using (Stream codeStream = new MemoryStream())
            {
                EmitResult compilationResult = compilation.Emit(codeStream);

                if (!compilationResult.Success)
                {
                    StringBuilder sb = new();
                    foreach (var diag in compilationResult.Diagnostics)
                    {
                        sb.AppendLine(diag.ToString());
                    }
                    string errorMessage = sb.ToString();
                    logger.Error(ErrorMessageTemplate,
                        sourceFilePath,
                        errorMessage);

                    throw new CompilerException(
                        string.Format(ErrorMessageTemplate,
                            sourceFilePath,
                            errorMessage));
                }
                byte[] assemblyBytes = ((MemoryStream)codeStream).ToArray();



                File.WriteAllBytes(compiledPatchFile, assemblyBytes);
                Assembly assembly = Assembly.LoadFrom(compiledPatchFile);
                File.WriteAllText(compiledPatchFile + ".md5", checksum);
                logger.Information(CompileMessageTemplate, patchName, checksum);
                Console.WriteLine(CompileMessageTemplate, patchName, checksum);
                return new PatchInfo(patchName, patchDesc, checksum, assembly);
            }
        }

        

        private void AddPatchFrameworkRefereneces()
        {
            AddAssembly(GetType());
            AddAssembly(typeof(ICodeInjection));
            AddAssembly(typeof(Enums.InjectionLocation));
            AddAssembly(typeof(AssemblyDefinition));
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
                if(path is null) 
                    return false;
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

                PortableExecutableReference systemReference = 
                    MetadataReference.CreateFromFile(type.Assembly.Location);
                References.Add(systemReference);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void AddGameReferencesAndUnityLibrariesReferences(string gamePath)
        {
            string gameAssemblyFolder = Utils.GetGameAssemblyFolder(gamePath);


            string[] assemblies = Directory.GetFiles(gameAssemblyFolder, "*.dll");

            AddAssemblies(assemblies);
        }


        private void AddPatchReferences(string file)
        {
            string[] references = Utils.ParsePatchLibraryReferences(file);

            if(references.Length == 0) 
                return;
            AddAssemblies(references);
        }
        private void AddPatchReferences(string[] references)
        {
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
                rtPath + "System.Collections.dll",
                rtPath + "System.Collections.Concurrent.dll",
                rtPath + "System.Collections.Generic.dll",
                rtPath + "System.Collections.NonGeneric.dll",
                rtPath + "netstandard.dll",
                rtPath + "Microsoft.CSharp.dll"
            );
        }
        private void AddNetFrameworkDefaultReferences()
        {
            AddAssembly("mscorlib.dll");
            AddAssembly("System.dll");
            AddAssembly("System.Core.dll");
            AddAssembly("Microsoft.CSharp.dll");
            AddAssembly("System.Net.Http.dll");
        }
    }
}
