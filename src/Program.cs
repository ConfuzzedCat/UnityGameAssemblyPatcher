using HarmonyLib;
using UnityGameAssemblyPatcher.CodeCompilation;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher
{
    internal class Program
    {
        private const string GivePathForGameString = "Give the path for game to be patched: ";
        private const string InvalidPathForGameString = "Invalid path for the game. Type it again: ";
        private const string HelpString = "UnityGameAssemblyPatcher.exe                             : To patch an game's assembly.\n" +
                                          "UnityGameAssemblyPatcher.exe (-d,-dir,--directory)       : To patch an game's assembly at given directory.\n" +
                                          "UnityGameAssemblyPatcher.exe (-r,-restore,--restore)     : To restore an game's assembly.\n" +
                                          "UnityGameAssemblyPatcher.exe (-h,-help,--help)           : To show this.";

        static void Main(string[] args)
        {
            string? gamePath;
            string arg = string.Join(' ', args);
            switch (args.Length)
            {
                case 0:
                    gamePath = GetGamePath();
                    PatchAtPath(gamePath);
                    return;
                case 1:                    
                        if (
                        args[0].Equals("-r")        ||
                        args[0].Equals("-restore")  ||
                        args[0].Equals("--restore")
                        )
                    {
                        gamePath = GetGamePath();
                        Utils.RestoreGameAssembly(gamePath);
                        Console.WriteLine("Restored game assembly file.");
                        Console.ReadKey();
                        return;
                    }
                    if (
                        args[0].Equals("-h")     ||
                        args[0].Equals("-help")  ||
                        args[0].Equals("--help")
                        )
                    {
                        Console.WriteLine(HelpString);
                        return;
                    }
                    break;
                case 2:
                    if (
                        args[0].Equals("-d")            ||
                        args[0].Equals("-dir")          ||
                        args[0].Equals("--directory")
                        )
                    {
                        gamePath = ValidateDir(args[1]);
                        PatchAtPath(gamePath);
                        return;
                    }
                    break;
            }
            Console.WriteLine("Invalid argument(s): {0}", arg);
        }

        private static void PatchAtPath(string gamePath)
        {
            AssemblyPatcher assemblyPatcher = AssemblyPatcher.GetInstance();
            assemblyPatcher.Patch(gamePath);
        }

        private static string GetGamePath()
        {
            Console.Write(GivePathForGameString);
            string? gamePath = Console.ReadLine();
            return ValidateDir(gamePath);
        }

        private static string ValidateDir(string? gamePath)
        {
            while (gamePath == null || !Directory.Exists(gamePath))
            {
                Console.Clear();
                Console.Write(InvalidPathForGameString);
                gamePath = Console.ReadLine();
            }
            return gamePath;
        }
    }
}