using Mono.Cecil;
using UnityGameAssemblyPatcher.Enums;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    public interface ICodeInjection
    {
        public TypeDefinition GetTargetClass(AssemblyDefinition assemblyDefinition);
        public Dictionary<MethodDefinition, Dictionary<InjectionLocation, MethodDefinition>> GetTargetMethodsAndInfo(AssemblyDefinition assemblyDefinition);
    }
}
