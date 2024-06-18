using Mono.Cecil;
using Serilog;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Exceptions;
using UnityGameAssemblyPatcher.PatchFramework;
using Patch = UnityGameAssemblyPatcher.CodeCompilation.Patch;

namespace UnityGameAssemblyPatcher.Utilities
{
    internal class Utils
    {
        private static readonly ILogger logger = Logging.GetLogger<Utils>();

        internal static string[] GetHeaderCommentLines(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Couldn't find source file.", file);
            }

            string[] fileLines = File.ReadAllLines(file);

            List<string> lines = new();
            bool hasCommentStarted = false;
            int index = 0;
            while (true)
            {
                string line = fileLines[index].Trim();
                if (line.StartsWith("namespace") || line.StartsWith("class") || line.StartsWith("{"))
                {
                    break;
                }
                if (line.StartsWith("*/"))
                {
                    break;
                }
                if (hasCommentStarted)
                {
                    lines.Add(line);
                }
                if(!hasCommentStarted && line.StartsWith("/*"))
                {
                    hasCommentStarted = true;
                }
                index++;
            }

            return lines.ToArray();
        }
        internal static string ParsePatchName(string file, string checksum)
        {
            string[] commentLines = GetHeaderCommentLines(file);
            return ParsePatchName(commentLines, checksum);
        }

        internal static string ParsePatchName(string[] commentLines, string checksum)
        {
            string? name = ParseLine(commentLines, "@Name=");
            return string.IsNullOrEmpty(name) ? $"UnnamedPatch({checksum})" : name;
        }

        internal static string[] ParsePatchLibraryReferences(string file)
        {

            string[] commentLines = GetHeaderCommentLines(file);
            return ParsePatchLibraryReferences(commentLines, Path.GetDirectoryName(file)!);
        }

        internal static string[] ParsePatchLibraryReferences(string[] commentLines, string gamePatchPath)
        {

            List<string> assemblyNames = new();
            foreach (string line in commentLines)
            {
                if (line.StartsWith("@using ") 
                    && (line.EndsWith(".dll;", StringComparison.OrdinalIgnoreCase) 
                    || line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                {
                    string _line = line.Replace("@using ", "").Replace(";", "").Trim();
                    if(!Path.IsPathFullyQualified(_line))
                    {
                        _line = GetAbsolutePath(gamePatchPath, _line);
                    }
                    assemblyNames.Add(_line);
                }
            }
            return assemblyNames.ToArray();
        }
        internal static string GetAbsolutePath(string basePath, string relativePath)
        {
            string combinedPath = Path.Combine(basePath, relativePath);
            string absolutePath = Path.GetFullPath(combinedPath);
            return absolutePath;
        }

        internal static Patch ParsePatchComments(string file, string checksum)
        {
            string[] lines = GetHeaderCommentLines(file);
            
            string            name = ParsePatchName(lines, checksum);
            string            desc = string.Empty; //TODO: add parser for description.
            string[]          references = ParsePatchLibraryReferences(lines, Path.GetDirectoryName(file)!);
            string            fileLocation = null;
            
            string            targetFramework = ParsePatchTargetFramework(lines);
            string            targetAssembly = ParseTargetAssembly(lines);
            string            targetModule = ParseTargetModule(lines);
            string            targetNamespace = ParseTargetNamespace(lines);
            string            targetClass = ParseTargetClass(lines);
            string            targetMethod = ParseTargetMethod(lines);
            InjectionLocation targetLocation = ParseTargetLocation(lines);

            string            patchClass = ParsePatchClass(lines);
            string            patchMethod = ParsePatchMethod(lines);
            
            return new Patch(
                name,
                desc,
                references,
                checksum,
                fileLocation,
                targetFramework,
                targetAssembly,
                targetModule,
                targetNamespace,
                targetClass,
                targetMethod,
                targetLocation,
                patchClass,
                patchMethod);
        }

        private static string ParseTargetNamespace(string[] lines)
        {
            string? tmodule = ParseLine(lines, "@TargetModule=");
            if (string.IsNullOrEmpty(tmodule))
            {
                return "MainModule";
            }
            return tmodule;
        }

        private static string ParseTargetModule(string[] lines)
        {
            string? tnamespace = ParseLine(lines, "@TargetNamespace=");
            if (string.IsNullOrEmpty(tnamespace))
            {
                return "";
            }
            return tnamespace;
        }

        private static string ParsePatchMethod(string[] lines)
        {
            string? pmethod = ParseLine(lines, "@PatchMethod=");
            if (string.IsNullOrEmpty(pmethod))
            {
                throw new ArgumentNullException(nameof(lines),"Argument \"@PatchMethod\" in patch was empty.");
            }
            return pmethod;
        }

        private static string ParsePatchClass(string[] lines)
        {
            string? pclass = ParseLine(lines, "@PatchClass=");
            if (string.IsNullOrEmpty(pclass))
            {
                throw new ArgumentNullException(nameof(lines),"Argument \"@PatchClass\" in patch was empty.");
            }
            return pclass;
        }

        private static InjectionLocation ParseTargetLocation(string[] lines)
        {
            string? tloc = ParseLine(lines, "@TargetLocation=");
            if (string.IsNullOrEmpty(tloc))
            {
                throw new ArgumentNullException(nameof(lines),"Argument \"@TargetLocation\" in patch was empty.");
            }

            if (Enum.TryParse(tloc, out InjectionLocation location))
            {
                return location;
            }

            string validLoc = Enum.GetNames(typeof(InjectionLocation)).Join();
            throw new ArgumentException($"{tloc} is a invalid target injection location. Valid inputs: {validLoc}.");
        }

        private static string ParseTargetMethod(string[] lines)
        {
            string? tmethod = ParseLine(lines, "@TargetMethod=");
            if (string.IsNullOrEmpty(tmethod))
            {
                throw new ArgumentNullException(nameof(lines),"Argument \"@TargetMethod\" in patch was empty.");
            }
            return tmethod;
        }

        private static string ParseTargetClass(string[] lines)
        {
            string? tclass = ParseLine(lines, "@TargetClass=");
            if (string.IsNullOrEmpty(tclass))
            {
                throw new ArgumentNullException(nameof(lines),"Argument \"@TargetClass\" in patch was empty.");
            }
            return tclass;
        }

        private static string ParseTargetAssembly(string[] lines)
        {
            string? target = ParseLine(lines, "@TargetAssembly=");
            
            if (!string.IsNullOrEmpty(target))
            {
                return target.Contains(Path.DirectorySeparatorChar) ? Path.GetFileName(target) : target;
            }
            return "Assembly-CSharp.dll";
        }

        internal static bool DoesAssemblyExist(string file, string gamePath)
        {
            string managedDir = GetGameAssemblyFolder(gamePath);
            return File.Exists(Path.Combine(managedDir, file));
        }

        internal static string ParsePatchTargetFramework(string[] lines)
        {
            var patchTargetFramework = ParseLine(lines, "@TargetFramework=");
            if (patchTargetFramework != null)
            {
                return patchTargetFramework;
            }

            string? version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName;
            if(version is null)
            {
                throw new NullReferenceException("Couldn't resolve Target Framework of both patch and this program.");
            }
            return version;
        }

        private static string? ParseLine(string[] lines, string parseString)
        {
            foreach (string line in lines)
            {
                if (line.StartsWith(parseString))
                {
                    return line.Replace(parseString, "").Trim();
                }
            }

            return null;
        }

        internal static string GetSourceCode(string file, string targetFramework)
        { 
            return GetSourceCode(
                File.ReadAllLines(file), 
                targetFramework);
        }
        internal static string GetSourceCode(string[] lines, string targetFramework)
        {
            StringBuilder sb = new StringBuilder();
            bool isCode = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                bool usinglineFlag = line.Contains("using ");
                bool namespaceLineFlag = line.Contains("namespace ");
                bool classLineFlag = line.Contains("class ");

                bool flag = usinglineFlag || namespaceLineFlag || classLineFlag;

                if (flag && !isCode)
                {
                    isCode = true;
                }
                if (isCode)
                {
                    if (namespaceLineFlag)
                    {
                        /*
                        if (targetFramework.Contains(".NETFramework"))
                        {
                            sb.AppendLine("using System.Reflection;");
                        }
                        else
                        {
                        }
                        */
                        sb.AppendLine("using System.Runtime.Versioning;");
                        sb.AppendLine($"[assembly:TargetFramework(\"{targetFramework}\")]");
                    }
                    sb.AppendLine(line);
                }
            }
            //Console.WriteLine(sb.ToString());
            return sb.ToString();
        }

        internal static string CalculateMD5Checksum(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }
        internal static Type GetEntryTypeOfCompiledPatch(Assembly compiledPatchAssembly)
        {
            Type baseType = typeof(ICodeInjection);

            List<Type> derivedTypes = new();

            foreach (Type type in compiledPatchAssembly.GetTypes())
            {
                if (baseType.IsAssignableFrom(type) && type != baseType)
                {
                    derivedTypes.Add(type);
                }
            }
            if(derivedTypes.Count > 1)
            {
                string typesFormattedForError = MulitpleCompiledPatchEntryClassesException.FormatTypesForErrorMessage(derivedTypes);
                string errorMessage = string.Format("Compiled patch: \"{0}\", found multiple classes of type: {1}. Types found: {2}.",
                    compiledPatchAssembly, 
                    typeof(ICodeInjection).Name, 
                    typesFormattedForError);
                throw new MulitpleCompiledPatchEntryClassesException(errorMessage);
            }
            return derivedTypes[0];
        }

        internal static void MakeGameFilesInCurrentFolder(string gameName, out bool doesExist)
        {
            string gamesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Games");
            if (!Directory.Exists(gamesFolder))
            {
                logger.Verbose("Creating folder for game hashes in current directory.");
                Directory.CreateDirectory(gamesFolder);
                doesExist = false;
                return;
            }
            
            string gameNameHashFile = Path.Combine(gamesFolder, gameName+".md5");
            doesExist = File.Exists(gameNameHashFile);
        }
        internal static void MakeDirectoriesInGameFolder(string gamePath)
        {
            string patchSourcePath = Path.Combine(gamePath, "Patches");
            string patchSourceLibPath = Path.Combine(patchSourcePath, "Lib");
            string patchCompiledPath = Path.Combine(gamePath, "CompiledPatches");

            if (!Directory.Exists(patchSourcePath))
            {
                logger.Verbose("\"Patches\" folder didn't exist, creating...");
                Directory.CreateDirectory(patchSourcePath);
            }
            if (!Directory.Exists(patchSourceLibPath))
            {
                logger.Verbose("\"Lib\" folder didn't exist, creating...");
                Directory.CreateDirectory(patchSourceLibPath);
            }
            if (!Directory.Exists(patchCompiledPath))
            {
                logger.Verbose("\"CompiledPatches\" folder didn't exist, creating...");
                Directory.CreateDirectory(patchCompiledPath);
            }
        }

        internal static string GetGameAssemblyFile(string gamePath)
        {
            string assemblyFolder = GetGameAssemblyFolder(gamePath);
            string gameAssemblyFile = Path.Combine(assemblyFolder, "Assembly-CSharp.dll");
            if (!File.Exists(gameAssemblyFile))
            {
                FileNotFoundException e = new("Assembly-CSharp.dll");
                logger.Error("");
                throw e;
            }
            return gameAssemblyFile;
        }

        internal static string GetGameAssemblyFolder(string gamePath)
        {
            string? gameDataFolder = Directory.GetDirectories(gamePath)
                            .First(path => path.EndsWith(
                                "_Data",
                                StringComparison.InvariantCultureIgnoreCase));
            if (gameDataFolder == null)
            {

                IOException e = new("No directory that ends with \"_Data\"");

                logger.Error("Could not find \"_Data\" folder for the given game, at the given path: {0}\nError: {1}",
                    gamePath,
                    e);
                throw e;
            }
            string[] gameDataDirs = Directory.GetDirectories(gameDataFolder);
            string? gameAssemblyFolder = gameDataDirs
                .First(path =>
                {
                    path = new DirectoryInfo(path).Name;
                    

                    return path.Equals(
                                        "Managed",
                                        StringComparison.InvariantCultureIgnoreCase);
                });
            if (gameAssemblyFolder == null)
            {
                IOException e = new("No directory the name with \"Managed\"");

                logger.Error("Could not find \"Managed\" folder for the given game, at the given path: {0}\nError: {1}",
                    gameDataFolder,
                    e);
                throw e;
            }
            return gameAssemblyFolder;
        }

        internal static string CalculateMD5ChecksumOfGameAssembly(string gamePath)
        {            
            return CalculateMD5Checksum(GetGameAssemblyFile(gamePath));
        }

        internal static string GetGameName(string gamePath)
        {
            //TODO: make it platform independent
            string gameFile = Directory.GetFiles(gamePath, "*.exe").First(file => !file.Contains("unity", StringComparison.InvariantCultureIgnoreCase));            
            return Path.GetFileNameWithoutExtension(gameFile);
        }

        internal static void BackupGameAssembly(string gamePath)
        {
            string gamesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Games");
            string gamesFolderGameAssemblyFileName = GetGameName(gamePath);
            string gameAssemblyFile = GetGameAssemblyFile(gamePath);
            string gameAssemblyBackupFile = Path.Combine(gamesFolder, gamesFolderGameAssemblyFileName);
            File.WriteAllText(gameAssemblyBackupFile + ".md5", CalculateMD5ChecksumOfGameAssembly(gamePath));
            File.Copy(gameAssemblyFile, gameAssemblyBackupFile + ".dll", false);
        }

        internal static void RestoreGameAssembly(string gamePath)
        {
            string gameName = GetGameName(gamePath);
            string gamesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Games");
            string gameAssemblyBackupFile = Path.Combine(gamesFolder, gameName);

            if (File.Exists(gameAssemblyBackupFile + ".dll"))
            {
                string gameAssemblyFile = GetGameAssemblyFile(gamePath);
                File.Copy(gameAssemblyBackupFile + ".dll", gameAssemblyFile, true);
                logger.Information("Game assembly restored successfully.");
            }
            else
            {
                logger.Warning("Backup file not found. Restoration failed.");
            }
        }

        internal static bool IsGameAssemblyModified(string gameName)
        {
            string gamesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Games");
            string gameAssemblyFile = GetGameAssemblyFile(gameName);
            string gameAssemblyBackupFile = Path.Combine(gamesFolder, gameName);

            string gameAssemblyBackupChecksumFile = gameAssemblyBackupFile + ".md5";
            string currentAssemblyChecksum = CalculateMD5ChecksumOfGameAssembly(gameAssemblyFile);

            if (File.Exists(gameAssemblyBackupChecksumFile))
            {
                string storedChecksum = File.ReadAllText(gameAssemblyBackupChecksumFile).Trim();

                if (currentAssemblyChecksum == storedChecksum)
                {
                    logger.Information("Game assembly has not been modified.");
                    return false;
                }
                else
                {
                    logger.Warning("Game assembly has been modified.");
                    return true;
                }
            }
            else
            {
                logger.Warning("No backup MD5 checksum file found. Assuming the game assembly has not been modified.");
                return false;
            }
        }

        internal static string[] GetAllSourcePatchFiles(string gamePath)
        {
            string patchSourcePath = Path.Combine(gamePath, "Patches");
            return Directory.GetFiles(patchSourcePath, "*.cs");
        }

        internal static T[] ResizeArray<T>(T[] oldArray, int newSize)
        {
            T[] newArray = new T[newSize];
            int elementsToCopy = Math.Min(oldArray.Length, newSize);

            for (int i = 0; i < elementsToCopy; i++)
            {
                newArray[i] = oldArray[i];
            }

            return newArray;
        }
        internal static T[] RemoveNullsAndResizeArray<T>(T?[] oldArray, int nullAmount)
        {
            int elementsToCopy = oldArray.Length - nullAmount;
            T[] newArray = new T[elementsToCopy];
            int index = 0;
            foreach (T? element in oldArray)
            {
                if(element is not null)
                {
                    newArray[index] = element;
                    index++;
                }
            }           
            return newArray;
        }
        internal static string GetTargetVersion(string file)
        {
            string version = string.Empty;
            using (AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(file))
            {
                version = GetTargetVersion(asmDef);
            }
            return version;
        }
        internal static string GetTargetVersion(AssemblyDefinition assemblyDefinition)
        {
            foreach (var i in assemblyDefinition.CustomAttributes)
            {
                if (i.AttributeType.Name.Equals("TargetFrameworkAttribute"))
                {
                    return (string)i.ConstructorArguments[0].Value ?? string.Empty;
                }
            }
            return string.Empty;
        }
    }
}
