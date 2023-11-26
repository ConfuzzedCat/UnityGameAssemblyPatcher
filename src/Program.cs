using UnityGameAssemblyPatcher.CodeCompilation;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher
{
    internal class Program
    {
        private const string GivePathForGameString = "Give the path for game to be patched: ";
        private const string InvalidPathForGameString = "Invalid path for the game. Type it again: ";
        static void Main(string[] args)
        {
            string? gamePath;
            switch (args.Length)
            {
                case 0:
                    gamePath = GetGamePath();
                    AssemblyPatcher assemblyPatcher = AssemblyPatcher.GetInstance();
                    assemblyPatcher.Patch(gamePath);
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
                        Console.WriteLine("UnityGameAssemblyPatcher.exe                             : To patch an game's assembly.");
                        Console.WriteLine("UnityGameAssemblyPatcher.exe (-r,-restore,--restore)     : To restore an game's assembly.");
                        Console.WriteLine("UnityGameAssemblyPatcher.exe (-h,-help,--help)           : To show this.");
                        return;
                    }
                    break;
            }
            string argsLine = string.Empty;
            foreach (var item in args)
            {
                argsLine += " " + item;
            }
            Console.WriteLine("Invalid argument(s): {0}", argsLine);
        }

        private static string GetGamePath()
        {
            Console.Write(GivePathForGameString);
            string? gamePath = Console.ReadLine();

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