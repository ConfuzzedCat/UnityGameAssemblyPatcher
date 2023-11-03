using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGameAssemblyPatcher.Enums;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    internal class ExampleCodeInjection : ICodeInjection
    {
        public TypeDefinition GetTargetClass(AssemblyDefinition assemblyDefinition)
        {
            return assemblyDefinition.MainModule.Types.First();
        }

        public Dictionary<MethodDefinition, Dictionary<InjectionLocation, MethodDefinition>> GetTargetMethodsAndInfo(AssemblyDefinition assemblyDefinition)
        {
            throw new NotImplementedException();
        }
    }
}
