using HarmonyLib;
using Mono.Cecil;
using System.Reflection;
using UnityGameAssemblyPatcher.Enums;

namespace UnityGameAssemblyPatcher.PatchFramework
{
    public interface ICodeInjection
    {
        public Type? GetTargetClass(Assembly assembly);
        public MethodInfo? GetTargetMethod(Assembly assembly);
        public (MethodInfo? PatchMethod, InjectionLocation injectionLocation) GetPatchMethod();
    }
}
