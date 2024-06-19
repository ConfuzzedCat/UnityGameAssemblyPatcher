using Mono.Cecil;
using Serilog;
using Mono.Cecil.Cil;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Extensions;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class AssemblyPatcher
    {

        private const string PatchingGameMessageTemplate = "Patching game at given path: {0}";
        private const string CompilingPatchesMessage = "Compiling patches. Only new files will be compiled.";
        private const string ApplyPatchMessageTemplate = "Applying patch: {0}";
        private const string PatchesFoundMessageTemplate = "Found {0} patches. Only new or modified patches will be compiled.";

        private static AssemblyPatcher _instance;

        private readonly ILogger _logger;
        
        private AssemblyDefinition _gameAssembly;
        private string _gameTarget;
        private string _gamePath;

        internal static AssemblyPatcher GetInstance()
        {
            return _instance ??= new AssemblyPatcher();
        }

        private AssemblyPatcher()
        {
            _logger = Logging.GetLogger<AssemblyPatcher>();
        }

            internal void Patch(string gamePath)
            {
                _logger.Information(PatchingGameMessageTemplate, gamePath);
                _gamePath = gamePath;
                string gameName = Utils.GetGameName(gamePath);
                _logger.Information("Game name: {0}", gameName);
                Utils.MakeDirectoriesInGameFolder(gamePath);
                Utils.MakeGameFilesInCurrentFolder(gameName, out bool didGameFolderExist);
                if(!didGameFolderExist)
                {
                    Utils.BackupGameAssembly(gamePath);
                }
                var gameAssemblyPath = Utils.GetGameAssemblyFile(gamePath);
                
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(gameAssemblyPath));
                var readerParameters = new ReaderParameters
                {
                    ReadWrite = true,
                    AssemblyResolver = resolver,
                    ReadSymbols = false
                };
                
                _gameAssembly = AssemblyDefinition.ReadAssembly(gameAssemblyPath, readerParameters);
                _gameTarget = Utils.GetTargetVersion(_gameAssembly);
                Patch[] patches = CompilePatches(gamePath);

                if (patches.Any())
                {
                    var writerParameters = new WriterParameters
                    {
                        WriteSymbols = false, 
                    };
                    
                    //string mergedPatchFile = CreateModAssembly(patches);
                    
                    Dictionary<string, List<Patch>> otherAsmTargets = new();
                    foreach (var patch in patches)
                    {
                        //patch.FileLocation = mergedPatchFile;
                        _logger.Information(ApplyPatchMessageTemplate, patch.Name);
                        if (patch.TargetAssembly != Path.GetFileName(gameAssemblyPath))
                        {
                            if (otherAsmTargets.ContainsKey(patch.TargetAssembly))
                            {
                                otherAsmTargets[patch.TargetAssembly].Add(patch);
                            }
                            else
                            {
                                otherAsmTargets.Add(patch.TargetAssembly, new List<Patch> { patch });
                            }
                            continue;
                        }
                        ApplyPatch(patch);
                    }

                    if (otherAsmTargets.Any())
                    {
                        Utils.BackupOtherAssemblies(gamePath, otherAsmTargets.Keys.ToArray());
                        foreach (var target in otherAsmTargets)
                        {
                            string targetAsmFile = Utils.GetAssemblyFile(target.Key, _gamePath);
                            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(targetAsmFile, readerParameters);
                            foreach (var patch in target.Value)
                            {
                                ApplyPatch(patch, asm);
                            }
                            if (_gameAssembly.MainModule.HasTypes && _gameAssembly.MainModule.Types.Any())
                            {
                                _logger.Information("{0} state is valid. Proceeding to write the assembly.", asm);
                            }
                            else
                            {
                                _logger.Warning("{0} state is invalid. Aborting the write operation.", asm);
                                return;
                            }
                            asm.Write(writerParameters);
                            asm.Dispose();
                        }
                    }
                    
                    if (_gameAssembly.MainModule.HasTypes && _gameAssembly.MainModule.Types.Any())
                    {
                        _logger.Information("{0} state is valid. Proceeding to write the assembly.", _gameAssembly);
                    }
                    else
                    {
                        _logger.Warning("{0} state is invalid. Aborting the write operation.", _gameAssembly);
                        return;
                    }

                    _gameAssembly.Write(writerParameters);
                    _gameAssembly.Dispose();
                }
            }

        private Patch[] CompilePatches(string gamePath)
        {
            _logger.Information(CompilingPatchesMessage);
            string[] sourceFiles = Utils.GetAllSourcePatchFiles(gamePath);
            _logger.Information(PatchesFoundMessageTemplate, sourceFiles.Length);
            List<Patch> patches = new();
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                Patch? a = null;
                try
                {
                    a = new CodeCompiler().Compile(sourceFiles[i], gamePath, _gameTarget);
                } catch (Exception e)
                {
                    _logger.Error("There was an error while compiling the patch.\n{0}", e);
                }

                if (a is null)
                {
                    continue;
                }
                patches.Add(a);
            } 
            return patches.ToArray();
        }

        private void ApplyPatch(Patch patch)
        {
            ApplyPatch(patch, _gameAssembly);
        }
        
        
        private void ApplyPatch(Patch patch, AssemblyDefinition gameAssembly)
        {
            AssemblyDefinition patchAsm = AssemblyDefinition.ReadAssembly(patch.FileLocation);
            ModuleDefinition module;
            if (patch.TargetModule == "MainModule")
            {
                module = gameAssembly.MainModule;
            }
            else
            {
                module = gameAssembly.Modules.Single(definition => definition.Name == patch.TargetModule);
            }
            TypeDefinition targetType = GetTargetClass(module, patch.TargetNamespace, patch.TargetClass);
            MethodDefinition targetMethod = GetTargetMethod(targetType, patch.TargetMethod);
            MethodDefinition patchMethod = GetPatchMethod(patchAsm, patch.PatchClass, patch.PatchMethod);
            InjectionLocation location = patch.TargetLocation;
            bool injectedSuccessful = ILMachine.Emit(gameAssembly.MainModule, ref targetMethod, patchMethod, location);
            _logger.Verbose("Was Patch emitted successful?: {0} - {1}", patch.Name, injectedSuccessful);
            if (injectedSuccessful)
            {
                string dst = Path.Combine(Utils.GetGameAssemblyFolder(_gamePath), Path.GetFileName(patch.FileLocation));
                if (File.Exists(dst))
                {
                    File.Delete(dst);
                }
                File.Copy(patch.FileLocation, dst);
            }
        }

        private TypeDefinition GetTargetClass(ModuleDefinition module, string targetNamespace, string targetClassName)
        {   
            return module.GetType(targetNamespace, targetClassName);
        }

        private MethodDefinition GetTargetMethod(TypeDefinition targetTypeDefinition, string methodName)
        {
            return targetTypeDefinition.Methods.Single(m => m.Name == methodName);
        }

        private MethodDefinition GetPatchMethod(AssemblyDefinition patchAsm, string patchClassName, string patchMethodName)
        {
            return patchAsm.MainModule.Types.Single(type => type.Name == patchClassName).GetMethod(patchMethodName);
        }
    }
}
