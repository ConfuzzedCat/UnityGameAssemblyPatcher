using System.Reflection;
using UnityGameAssemblyPatcher.Enums;
using Mono.Cecil;
namespace UnityGameAssemblyPatcher.PatchFramework
{
    public interface ICodeInjection
    {
        public (MethodDefinition PatchMethod, InjectionLocation injectionLocation) GetPatchMethod();
        public TypeDefinition GetTargetClass(AssemblyDefinition assembly);
        public MethodDefinition GetTargetMethod(AssemblyDefinition assembly);
    }
}
