using Mono.Cecil;
using Serilog;
using System.Reflection;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.PatchFramework;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class AssemblyPatcher
    {

        private const string EntryMessageTemplate = "Found Entry for patch at: {0}";
        private const string PatchingGameMessageTemplate = "Patching game at given path: {0}";
        private const string CompilingPatchesMessage = "Compiling patches. Only new files will be compiled.";
        private const string ApplyPatchMessageTemplate = "Applying patch: {0}";
        private const string PatchesFoundMessageTemplate = "Found {0} patches. Only new or modified patches will be compiled.";

        private static AssemblyPatcher _instance;

        private ILogger logger;
        private AssemblyDefinition gameAssembly;
        private string gameTarget;

        internal static AssemblyPatcher GetInstance()
        {
            return _instance ??= new AssemblyPatcher();
        }

        private AssemblyPatcher()
        {
            logger = Logging.GetLogger<AssemblyPatcher>();
        }

        internal void Patch(string gamePath)
        {
            logger.Information(PatchingGameMessageTemplate, gamePath);

            string gameName = Utils.GetGameName(gamePath);
            logger.Information("Game name: {0}", gameName);
            Utils.MakeDirectoriesInGameFolder(gamePath);
            Utils.MakeGameFilesInCurrentFolder(gameName, out bool didGameFolderExist);
            if(!didGameFolderExist)
            {
                Utils.BackupGameAssembly(gamePath);
            }
            var gameAssemblyPath = Utils.GetGameAssemblyFile(gamePath);
            gameAssembly = AssemblyDefinition.ReadAssembly(gameAssemblyPath, new ReaderParameters { ReadWrite = true });
            gameTarget = Utils.GetTargetVersion(gameAssembly);
            var patches = CompilePatches(gamePath);
            var patchInfos = patches as PatchInfo[] ?? patches.ToArray();
            if (patchInfos.Any())
            {
                foreach (var patch in patchInfos)
                {
                    logger.Information(ApplyPatchMessageTemplate, patch.Name);
                    ApplyPatch(patch);
                }
                gameAssembly.Write();
                gameAssembly.Dispose();
            }
        }

        private IEnumerable<PatchInfo> CompilePatches(string gamePath)
        {
            logger.Information(CompilingPatchesMessage);
            string[] sourceFiles = Utils.GetAllSourcePatchFiles(gamePath);
            logger.Information(PatchesFoundMessageTemplate, sourceFiles.Length);
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                PatchInfo? a = null;
                try
                {
                    a = new CodeCompiler().Compile(sourceFiles[i], gamePath, gameTarget);
                } catch (Exception e)
                {
                    logger.Error("There was an error while compiling the patch.\n{0}", e);
                }
                if (a is null)
                    continue;
                yield return a;
            }
        }
        private void ApplyPatch(PatchInfo patch)
        {
            var entry = Utils.GetEntryTypeOfCompiledPatch(patch.Assembly);
            logger.Information(EntryMessageTemplate, entry);
            var assemblyInstance = (ICodeInjection)patch.Assembly.CreateInstance(entry.FullName!)!;
            var targetMethod = assemblyInstance.GetTargetMethod(gameAssembly);
            (MethodInfo PatchMethod, InjectionLocation injectionLocation) patchingMethodTuple = assemblyInstance.GetPatchMethod();
            ILMachine.Emit(gameAssembly.MainModule, ref targetMethod, patchingMethodTuple.PatchMethod, patchingMethodTuple.injectionLocation);
        }
    }
}
