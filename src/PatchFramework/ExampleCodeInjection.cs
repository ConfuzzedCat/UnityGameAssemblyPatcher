using Mono.Cecil;
using UnityGameAssemblyPatcher.Enums;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    class ExampleCodeInjection : ICodeInjection
    {
        public (MethodInfo? PatchMethod, InjectionLocation injectionLocation) GetPatchMethod()
        {
            return (typeof(ExampleCodeInjection).GetMethod(nameof(PatchingMethod)), InjectionLocation.Postfix);
        }

        public Type? GetTargetClass(Assembly assembly)
        {
            return assembly.GetType("SomeClass");
        }

        public MethodInfo? GetTargetMethod(Assembly assembly)
        {
            return GetTargetClass(assembly)?.GetMethod("SomeMethod");
        }
        public void PatchingMethod()
        {

        }
    }
}
