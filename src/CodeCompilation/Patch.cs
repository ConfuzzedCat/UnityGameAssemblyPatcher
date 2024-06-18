using UnityGameAssemblyPatcher.Enums;

namespace UnityGameAssemblyPatcher.CodeCompilation;

internal class Patch
{
    internal Patch(
        string name,
        string description,
        string[] references,
        string checksum,
        string fileLocation,
        string targetFramework,
        string targetAssembly,
        string targetModule, 
        string targetNamespace,
        string targetClass,
        string targetMethod,
        InjectionLocation targetLocation,
        string patchClass,
        string patchMethod)
    {
        this.Name = name;
        this.Description = description;
        this.References = references;
        this.Checksum = checksum;
        this.FileLocation = fileLocation;
        this.TargetFramework = targetFramework;
        this.TargetAssembly = targetAssembly;
        this.TargetModule = targetModule;
        this.TargetNamespace = targetNamespace;
        this.TargetClass = targetClass;
        this.TargetMethod = targetMethod;
        this.TargetLocation = targetLocation;
        this.PatchClass = patchClass;
        this.PatchMethod = patchMethod;
    }

    internal string Name { get; }
    internal string Description { get; }
    internal string[] References { get; }
    internal string FileLocation { get; }
    internal string Checksum { get; }

    internal string TargetFramework { get; }
    internal string TargetAssembly { get; }
    internal string TargetModule { get; }
    internal string TargetNamespace { get; }
    internal string TargetClass { get; }
    internal string TargetMethod { get; }
    internal InjectionLocation TargetLocation { get; }
    
    internal string PatchClass { get; }
    internal string PatchMethod { get; }
}