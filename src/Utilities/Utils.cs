using System.Security.Cryptography;

namespace UnityGameAssemblyPatcher.Utilities
{
    internal class Utils
    {
        internal static string[] ParsePatchLibraryReferences(string file)
        {

            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Couldn't find source file.", file);
            }

            string[] fileLines = File.ReadAllLines(file);

            List<string> assemblyNames = new();
            foreach (string line in fileLines)
            {
                if (line.StartsWith("namespace") || line.StartsWith("class") || line.StartsWith("{"))
                {
                    break;
                }
                if (line.StartsWith("/*") || line.StartsWith("*/"))
                {
                    continue;
                }
                if (line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyNames.Add(line.Trim());
                }
            }
            return assemblyNames.ToArray();
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
    }
}
