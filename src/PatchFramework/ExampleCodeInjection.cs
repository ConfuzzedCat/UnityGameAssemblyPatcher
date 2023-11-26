using Mono.Cecil;
using System.Reflection;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Extensions;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    class ExampleCodeInjection : ICodeInjection
    {
#nullable enable
        public (MethodInfo PatchMethod, InjectionLocation injectionLocation) GetPatchMethod()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            
            // Get the location of the assembly file
            string assemblyPath = executingAssembly.Location;

            // Read the assembly into an AssemblyDefinition
            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        
            foreach (ModuleDefinition module in assemblyDefinition.Modules)
            {
                Console.WriteLine("patchmodule: " + module.Name);
            }

            return (typeof(ExampleCodeInjection).GetMethod(nameof(PatchingMethod))!, InjectionLocation.Postfix);
        }
        public MethodDefinition GetTargetMethod(AssemblyDefinition assembly)
        {
            var type = assembly.MainModule.GetType("", "Pickup");
            return type!.GetMethod("Start");
        }
#nullable disable
        public static void PatchingMethod()
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "HelloFromPatch.txt"), "This is from the patch method.");
            Console.WriteLine("Hello from patching method.");
        }
    }
}
