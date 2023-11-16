using System.Reflection;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class PatchInfo
    {
        internal string Name { get; }
        internal string Description { get; }
        internal string Checksum { get; }
        internal Assembly Assembly { get; }

        internal PatchInfo(string name, string desc, string checksum, Assembly asm)
        {
            Name = name;
            Description = desc;
            Checksum = checksum;
            Assembly = asm;
        }
    }
}
