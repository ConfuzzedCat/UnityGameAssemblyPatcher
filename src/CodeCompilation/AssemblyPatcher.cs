using Mono.Cecil;
using Serilog;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Extensions;
using UnityGameAssemblyPatcher.PatchFramework;
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

        private ILogger _logger;
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
                if (CompilePatches(gamePath) is Patch[] patches && patches.Any())
                {
                    foreach (var patch in patches)
                    {
                        _logger.Information(ApplyPatchMessageTemplate, patch.Name);
                        ApplyPatch(patch);
                    }
                    
                    if (_gameAssembly.MainModule.HasTypes && _gameAssembly.MainModule.Types.Any())
                    {
                        _logger.Information("Assembly state is valid. Proceeding to write the assembly.");
                    }
                    else
                    {
                        _logger.Warning("Assembly state is invalid. Aborting the write operation.");
                        return;
                    }
                    var writerParameters = new WriterParameters
                    {
                        WriteSymbols = false, 
                    };

                    _gameAssembly.Write(writerParameters);
                    _gameAssembly.Dispose();
                    CreateModAssembly(patches);
                }
            }

        private IEnumerable<Patch> CompilePatches(string gamePath)
        {
            _logger.Information(CompilingPatchesMessage);
            string[] sourceFiles = Utils.GetAllSourcePatchFiles(gamePath);
            _logger.Information(PatchesFoundMessageTemplate, sourceFiles.Length);
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
                    continue;
                yield return a;
            }
        }
        private void ApplyPatch(Patch patch)
        {
            AssemblyDefinition patchAsm = AssemblyDefinition.ReadAssembly(patch.FileLocation);
            
            
            
            
            
            // TODO: check if target is in the assembly
            //var entry = Utils.GetEntryTypeOfCompiledPatch(patch.Assembly);
            //_logger.Information(EntryMessageTemplate, entry);
            //var assemblyInstance = (ICodeInjection)patch.Assembly.CreateInstance(entry.FullName!)!;
            ModuleDefinition module;
            if (patch.TargetModule == "MainModule")
            {
                module = _gameAssembly.MainModule;
            }
            else
            {
                module = _gameAssembly.Modules.Single(definition => definition.Name == patch.TargetModule);
            }
            TypeDefinition targetType = GetTargetClass(module, patch.TargetNamespace, patch.TargetClass);
            MethodDefinition targetMethod = GetTargetMethod(targetType, patch.TargetMethod);
            MethodDefinition patchMethod = GetPatchMethod(patchAsm, patch.PatchClass, patch.PatchMethod);
            InjectionLocation location = patch.TargetLocation;
            bool injectedSuccessful = ILMachine.Emit(_gameAssembly.MainModule, ref targetMethod, patchMethod, location);
            _logger.Verbose("Was Patch emitted successful?: {0} - {1}", patch.Name, injectedSuccessful);
        }
        public TypeDefinition GetTargetClass(ModuleDefinition module, string targetNamespace, string targetclassName)
        {   
            return module.GetType(targetNamespace, targetclassName);
        }

        public MethodDefinition GetTargetMethod(TypeDefinition targetTypeDefinition, string methodName)
        {
            return targetTypeDefinition.GetMethod(methodName);
        }

        public MethodDefinition GetPatchMethod(AssemblyDefinition patchAsm, string patchClassName, string patchMethodName)
        {
            return patchAsm.MainModule.Types.Single(type => type.Name == patchClassName).GetMethod(patchMethodName);
        }

        private void CreateModAssembly(Patch[] patches)
        {
            //TODO: Add logging for this method, and called methods
            string mergedAsmPath = Path.Combine(Utils.GetGameAssemblyFolder(_gamePath), "PatchesMerged.dll");
            if (File.Exists(mergedAsmPath))
            {
                File.Delete(mergedAsmPath);
            }
            
            List<AssemblyDefinition> patchAsmDefs = patches.Select(patch => AssemblyDefinition.ReadAssembly(patch.FileLocation)).ToList();
            AssemblyDefinition newAssembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("Patches", new Version(1, 0, 0, 0)),
                "PatchesModule",
                ModuleKind.Dll);
            
            MergeAssemblies(newAssembly, patchAsmDefs);
            
            
            newAssembly.Write(mergedAsmPath);
        }
        private void MergeAssemblies(AssemblyDefinition newAssembly, List<AssemblyDefinition> patchAssemblies)
    {
        var newModule = newAssembly.MainModule;

        foreach (var patchAssembly in patchAssemblies)
        {
            foreach (var type in patchAssembly.MainModule.Types)
            {
                Console.WriteLine(type.FullName);
                if (!ImplementsICodeInjection(type) && !IsEmpty(type) )//&& !IsCompilerLeftoverClasses(type))
                {
                    var newType = new TypeDefinition(
                        type.Namespace, type.Name, type.Attributes, newModule.ImportReference(type.BaseType));

                    foreach (var field in type.Fields)
                    {
                        var newField = new FieldDefinition(field.Name, field.Attributes, newModule.ImportReference(field.FieldType));
                        newType.Fields.Add(newField);
                    }

                    foreach (var method in type.Methods)
                    {
                        if (!IsInjectionMethod(method))
                        {
                            var newMethod = new MethodDefinition(method.Name, method.Attributes, newModule.ImportReference(method.ReturnType));

                            foreach (var parameter in method.Parameters)
                            {
                                newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, newModule.ImportReference(parameter.ParameterType)));
                            }

                            foreach (var variable in method.Body.Variables)
                            {
                                newMethod.Body.Variables.Add(new VariableDefinition(newModule.ImportReference(variable.VariableType)));
                            }

                            foreach (var instruction in method.Body.Instructions)
                            {
                                newMethod.Body.Instructions.Add(ImportInstruction(instruction, newModule));
                            }

                            newMethod.Body.InitLocals = method.Body.InitLocals;
                            newType.Methods.Add(newMethod);
                        }
                    }

                    newModule.Types.Add(newType);
                }
            }
        }
    }

        private bool IsCompilerLeftoverClasses(TypeDefinition type)
        {
            string[] fullNames =
            {
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute",
                "System.Runtime.CompilerServices.RefSafetyRulesAttribute"
            };
            return fullNames.Contains(type.FullName);
        }

        private bool IsEmpty(TypeDefinition type)
        {
            return type is
            {
                HasEvents: false, 
                HasFields: false, 
                HasInterfaces: false, 
                HasMethods: false, 
                HasProperties: false, 
                HasCustomAttributes: false, 
                HasNestedTypes: false
            };
        }

        private bool ImplementsICodeInjection(TypeDefinition type)
        {
            foreach (var iface in type.Interfaces)
            {
                if (iface.InterfaceType.FullName == typeof(ICodeInjection).FullName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsInjectionMethod(MethodDefinition method)
        {
            return method.Name == "GetPatchMethod" || method.Name == "GetTargetClass" || method.Name == "GetTargetMethod";
        }
        private Instruction ImportInstruction(Instruction instruction, ModuleDefinition targetModule)
        {
            if (instruction.Operand is MethodReference methodReference)
            {
                return Instruction.Create(instruction.OpCode, targetModule.ImportReference(methodReference));
            }
            if (instruction.Operand is FieldReference fieldReference)
            {
                return Instruction.Create(instruction.OpCode, targetModule.ImportReference(fieldReference));
            }
            if (instruction.Operand is TypeReference typeReference)
            {
                return Instruction.Create(instruction.OpCode, targetModule.ImportReference(typeReference));
            }
            /*
            TODO: look at later
            if (instruction.Operand is CallSite callSite)
            {
                return Instruction.Create(instruction.OpCode, targetModule.ImportReference(callSite));
            }

            return Instruction.Create(instruction.OpCode, instruction.Operand);
            */
            return instruction;
        }
    }
}
