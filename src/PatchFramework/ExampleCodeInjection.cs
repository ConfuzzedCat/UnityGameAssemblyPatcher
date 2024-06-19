/*
*Optional*
// If left empty, will default to "UnnamedPatch(checksum)"
@Name=Test Patch

*Optional*
// This can be any file in the managed dir.
// If left empty, will default to "Assembly-CSharp.dll"
@TargetAssembly=Assembly-CSharp.dll

*Optional*
// If left empty, will default to "MainModule"
@TargetModule=MainModule

*Optional*
// If left empty, will default to "" (empty string)
@TargetNamespace=

*required*
// The class you want to add the reference to your method
@TargetClass=SomeClass

*required*
// The method you want to add the reference to your method into
@TargetMethod=Start

*required*
// The location you want the reference to be inserted at
// Valid: Prefix, Postfix
@TargetLocation=Prefix

*required*
// Name of the entry class to your patch
// Needs to public
@PatchClass=PatchClass

*required*
// Name of the entry method to your patch
// Needs to be public and static
@PatchMethod=PatchingMethod
*/
using System;
using System.IO;

public class PatchClass
{
    public static void PatchingMethod()
    {
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "HelloFromPatch.txt"), "This is from the patch method.");
        //Console.WriteLine("Hello from patching method.");
    }
}