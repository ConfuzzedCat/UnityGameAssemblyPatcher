
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Reflection;
using System.Runtime.Versioning;
using UnityGameAssemblyPatcher.CodeCompilation;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.PatchFramework;
using UnityGameAssemblyPatcher.src.CodeCompilation;
using UnityGameAssemblyPatcher.Utilities;

namespace UnityGameAssemblyPatcher
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Writing test code.");
            string sourceFile = Path.Combine(Directory.GetCurrentDirectory(), "test.cs");
            string code = @"
/*
This is the header of the patch
@Name=This Is The Name Of The Patch 
*/

using System;
using System.Reflection;
using HarmonyLib;
using UnityGameAssemblyPatcher.PatchFramework;
using UnityGameAssemblyPatcher.Enums;
namespace TestSpace
{
    
    class TestClass : ICodeInjection
    {
//#nullable enable
        public (MethodInfo? PatchMethod, InjectionLocation injectionLocation) GetPatchMethod()
        {
            return (typeof(TestClass).GetMethod(nameof(PatchingMethod)), InjectionLocation.Postfix);
        }

        public Type? GetTargetClass(Assembly assembly)
        {
            return assembly.GetType(""Pickup"");
        }

        public MethodInfo? GetTargetMethod(Assembly assembly)
        {
            return GetTargetClass(assembly).GetMethod(""Start"", BindingFlags.NonPublic | BindingFlags.Instance);
        }
//#nullable disable
        public static void PatchingMethod(string _value, ref string ___Value)
        {
            ___Value = ""patched value from compiled patch..."";
            Console.WriteLine(""Hello from patching method."");
        }
    }
}
            ";

            File.WriteAllText(sourceFile, code);

            Console.WriteLine("Compiling test code.");

            var codeCompiler = new CodeCompiler();

            var Patch = codeCompiler.Compile(sourceFile, "");

            var harmony = new Harmony("com.github.confuzzedcat.unitygameassemblypatcher");


            if (Patch != null)
            {
                AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Path.Combine(Directory.GetCurrentDirectory(), "Assembly-Csharp.dll"));
                ICodeInjection assemblyInstance = new ExampleCodeInjection(); //(ICodeInjection)Patch.Assembly.CreateInstance("TestSpace.TestClass");
                var targetType = assemblyInstance.GetTargetClass(game);
                var targetMethod = assemblyInstance.GetTargetMethod(game);
                (MethodDefinition PatchMethod, InjectionLocation injectionLocation) patchingMethodTuple = assemblyInstance.GetPatchMethod();
                ILMachine.Emit(targetMethod, patchingMethodTuple.PatchMethod, patchingMethodTuple.injectionLocation);

            }

            foreach (var assemblyFile in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll"))
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile);
                string? targetVersion = Utils.GetTargetVersion(assemblyDefinition);
                Console.WriteLine($"file: {assemblyFile.Replace(Directory.GetCurrentDirectory(), "")}\n\ttarget: {targetVersion}\n");

            }
            Console.WriteLine("---------------------------");
            foreach (var assemblyFile in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.patch"))
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile);
                string? targetVersion = Utils.GetTargetVersion(assemblyDefinition);
                Console.WriteLine($"file: {assemblyFile.Replace(Directory.GetCurrentDirectory(), "")}\n\ttarget: {targetVersion}\n");

            }
        }
    }

    public class TestingGroundsClass
    {
        public string Value = "Hello!";
        
        public void MethodSetValue(string _value)
        {
            Value = _value;
        }
        
        public void SomeMethod()
        {
            
            
            Console.WriteLine(Value);
        }
    }
}