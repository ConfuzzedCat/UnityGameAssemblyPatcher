
using Mono.Cecil;
using System.Reflection;
using System.Runtime.Versioning;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            foreach (var assemblyFile in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll"))
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile);
                string? targetVersion = Utils.GetTargetVersion(assemblyDefinition);
                Console.WriteLine($"file: {assemblyFile.Replace(Directory.GetCurrentDirectory(), "")}\n\ttarget: {targetVersion}\n");
            }
        }        
    }
}   