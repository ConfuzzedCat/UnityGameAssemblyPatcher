using System.Reflection;
using UnityGameAssemblyPatcher.Enums;
using Mono.Cecil;
namespace UnityGameAssemblyPatcher.PatchFramework
{
    public interface ICodeInjection
    {
        public (MethodInfo PatchMethod, InjectionLocation injectionLocation) GetPatchMethod();
        public MethodDefinition GetTargetMethod(AssemblyDefinition assembly);
    }
}
