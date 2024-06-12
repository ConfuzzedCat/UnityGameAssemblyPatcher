# UnityGameAssemblyPatcher
A tool to patch Unity Game Assembly statically.

## Usage
Simply run and type the path to the unity game.
IMPORTANT: The game must be Mono, and not IL2CPP.
It is recommended to run this once, to create the folders.

## How to Restore the game
To restore the game, simply run the tool again with `-r`, `-restore` or `--restore`, and type the path to the unity game.

## How to patch
After the folders are created, you can put your patches in the `Patches` folder.
The patches must have the `.cs` extension and have a class that implements [ICodeInjection](src/PatchFramework/ICodeInjection.cs).
The patch also needs atleast one more method, which is `MethodInfo PatchMethod` of the `GetPatchMethod()`.
This method is the method that will be injected into the game assembly.

How to add metadata to the patch:
```cs
/*
This is the header of the patch.
Any line without a @ is ignored.
@Name=This Is The Name Of The Patch

This is how to add libraries to the patch.
These files will be used for compiling the patch.
It can be a relative path, or an absolute path.
@using SomeLibrary.dll;
@using c:\absolute\path\to\AnotherLibrary.dll

You can also specify the target framework of the patch. This isn't required, nor is it used for the compiler.
If it is not specified, it will default to the target framework of UnityGameAssemblyPatcher
@TargetFramework=.NETCoreApp,Version=v7.0
*/
```

## Example patch
```cs
/*
This is the header of the patch
@Name=This Is The Name Of The Patch 
@using ./Lib/UnityEngine.SharedInternalsModule.dll
@using C:\Users\user\Downloads\Unity Game\Unity Game_Data\Managed\UnityEngine.CoreModule.dll;
*/

using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Extensions;
using UnityGameAssemblyPatcher.PatchFramework;

class TestClass : ICodeInjection
{
#nullable enable
    public (MethodInfo PatchMethod, InjectionLocation injectionLocation) GetPatchMethod()
    {
                          
            return (typeof(PatchClass).GetMethod(nameof(PatchClass.PatchingMethod)), InjectionLocation.Postfix);
    }

    public TypeDefinition GetTargetClass(AssemblyDefinition assembly)
    {   
        ModuleDefinition mainModule = assembly.MainModule;
        return mainModule.GetType("SomeNamespace", "SomeClass");
    }

    public MethodDefinition GetTargetMethod(AssemblyDefinition assembly)
    {
        return GetTargetClass(assembly)?.GetMethod("SomeMethod");
    }
}
#nullable disable

public class PatchClass
{
    public static void PatchingMethod()
    {
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "HelloFromPatch.txt"), "This is from the patch method.");
        //Console.WriteLine("Hello from patching method.");
    }
}
```

Notes:
1)
    I would recommend using Harmony for patching, this tool is mainly for running code before and after a method.
    It won't be able to inject properties, fields, or method arguments.
    UnityGameAssemblyPatcher does come with HarmonyX, which you can reference in your patch, and use it to patch the game.

2)
    To call the compiler less, it will only compile the patch if the patch file is new or modified compiled patch.
    If you want to force compile the patch, you can delete the checksum file (.md5), and it will recompile it.
3) 
    The tool will log to a file called `UnityGamePatcherYYYYMMDDHH.log` in the same directory as the tool.
    where YYYY is the year, MM is the month, DD is the day, and HH is the hour (24h format). I think.

## Why?
I made to learn more about Mono.Cecil, and to have a tool to patch Unity games, without having to use dnSpy or Modloaders like MelonLoader or BepInEx.
I Also made to patch games "quicker" in the sense that I don't have to open Visual Studio, create a new project, add MelonLoader, etc.

## Libraries used
- [HarmonyX](https://github.com/BepInEx/HarmonyX)
- [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/)
- [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File/)

## Credits
- [Rick Strahl](https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-CSharp-Code-Compilation-Revisited-for-Roslyn)
    for the blog descriping how to interact with Roslyn API, eg. compiling code at runtime.
- [snaphat](https://github.com/snaphat/MonoCecilExtensions)
	for their Mono.Cecil.Extensions.
