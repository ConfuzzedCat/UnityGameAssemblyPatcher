using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class AssemblyPatcher
    {
        private static AssemblyPatcher instance;

        private ILogger logger;
        private Assembly gameAssembly;

        internal static AssemblyPatcher GetInstance()
        {
            return instance ??= new();
        }

        private AssemblyPatcher()
        {
            logger = Logging.GetInstance();
        }

        internal void Patch()
        {
            Console.Write("Give the path for game to be patched: ");
            string? gamePath = Console.ReadLine();

            while(gamePath == null || !Directory.Exists(gamePath))
            {
                Console.Clear();
                Console.Write("Invalid path for the game. Type it again: ");
                gamePath = Console.ReadLine();
            }

            Patch(gamePath);
        }

        internal void Patch(string gamePath)
        {
            logger.Information("Patching game at given path: {0}", gamePath);

            string gameName = Utils.GetGameName(gamePath);

            Utils.MakeDirectoriesInGameFolder(gamePath);
            Utils.MakeGameFilesInCurrentFolder(gameName, out bool didGameFolderExist);
            if(!didGameFolderExist)            
                Utils.BackupGameAssembly(gamePath);
            
            gameAssembly = Assembly.LoadFrom(Utils.GetGameAssemblyFile(gamePath));
            Assembly[] compiledPatches = CompilePatches(gamePath);



        }
        private Assembly[] CompilePatches(string gamePath)
        {
            logger.Information("Compiling patches. Only new files will be compiled.");
            string[] sourceFiles = Utils.GetAllSourcePatchFiles(gamePath);
            int nullAssembliesAmount = 0;
            Assembly?[] patchesWithNulls = new Assembly?[sourceFiles.Length];
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                Assembly? a = new CodeCompiler().Compile(sourceFiles[i], gamePath);
                if(a is null)
                    nullAssembliesAmount++;
                patchesWithNulls[i] = a;
            }
            return Utils.RemoveNullsAndResizeArray(patchesWithNulls, nullAssembliesAmount);
        }
        private void ApplyPatch(Assembly patch)
        {

        }
    }
}
