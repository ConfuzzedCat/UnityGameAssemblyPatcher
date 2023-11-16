
using HarmonyLib;
using Mono.Cecil;
using System.Reflection;
using System.Runtime.Versioning;
using UnityGameAssemblyPatcher.CodeCompilation;
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
@Name=ThisIsTheNameOfThePatch 
*/

using System;
using System.Collections.Generic;
using Mono.Cecil;
using HarmonyLib;
namespace TestSpace
{
    [HarmonyPatch(""UnityGameAssemblyPatcher.TestingGroundsClass"", ""UnityGameAssemblyPatcher.TestingGroundsClass.MethodSetValue"")]
    class TestClass
    {
        static void Postfix(string _value, ref string ___Value)
        {
            Console.WriteLine(""Hello, This is from the test code after it is compiled."");
            ___Value = ""patched value"";
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
                TestingGroundsClass testingGrounds = new TestingGroundsClass();
                Console.WriteLine("Before patch.");
                testingGrounds.MethodSetValue("unpatched value!");
                testingGrounds.SomeMethod();
                var assemblyType = Patch.Assembly.GetType("TestSpace.TestClass");
                MethodInfo methodInfo = assemblyType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(typeof(TestingGroundsClass).GetMethod(nameof(TestingGroundsClass.MethodSetValue)), postfix: new HarmonyMethod(methodInfo));
                //methodInfo.Invoke(new object(), null);
                Console.WriteLine("After patch");
                testingGrounds.MethodSetValue("unpatched another value!");
                testingGrounds.SomeMethod();
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
        public string Value = string.Empty;
        
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