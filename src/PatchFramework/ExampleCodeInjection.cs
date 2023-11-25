using Mono.Cecil;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Extensions;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    class ExampleCodeInjection : ICodeInjection
    {
#nullable enable
        public (MethodDefinition PatchMethod, InjectionLocation injectionLocation) GetPatchMethod()
        {
            return (typeof(ExampleCodeInjection).GetMethod(nameof(PatchingMethod)).ToDefinition(), InjectionLocation.Postfix);
        }

        public TypeDefinition GetTargetClass(AssemblyDefinition assembly)
        {
            ModuleDefinition mainModule = assembly.MainModule;
            return mainModule.GetType("", "Pickup");
        }

        public MethodDefinition GetTargetMethod(AssemblyDefinition assembly)
        {
            return GetTargetClass(assembly)?.GetMethod("Start");
        }
#nullable disable
        public static void PatchingMethod()
        {
            //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "HelloFromPatch.txt"), "This is from the patch method.");
            Console.WriteLine("Hello from patching method.");
        }
    }
}
