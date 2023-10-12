using Mono.Cecil;
using UnityGameAssemblyPatcher.Enums;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    public abstract class ACodeInjection
    {
        public string sourceChecksum;
        public abstract TypeDefinition GetTargetClass(AssemblyDefinition assemblyDefinition);
        public abstract Dictionary<MethodDefinition, Dictionary<InjectionLocation, MethodDefinition>> GetTargetMethodsAndInfo(AssemblyDefinition assemblyDefinition);
    }

}
