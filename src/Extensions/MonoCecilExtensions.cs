using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameAssemblyPatcher.Extensions
{
    public static class MonoCecilExtensions
    {
        public static MethodDefinition GetMethod(this TypeDefinition self, string name)
        {
            return self.Methods.Where(m => m.Name == name).First();
        }

        public static FieldDefinition GetField(this TypeDefinition self, string name)
        {
            return self.Fields.Where(f => f.Name == name).First();
        }

        public static TypeDefinition ToDefinition(this Type self)
        {
            var module = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(self.Module.FullyQualifiedName)));
            return (TypeDefinition)module.LookupToken(self.MetadataToken);
        }

        public static MethodDefinition ToDefinition(this MethodBase method)
        {
            var declaring_type = method.DeclaringType.ToDefinition();
            return (MethodDefinition)declaring_type.Module.LookupToken(method.MetadataToken);
        }

        public static FieldDefinition ToDefinition(this FieldInfo field)
        {
            var declaring_type = field.DeclaringType.ToDefinition();
            return (FieldDefinition)declaring_type.Module.LookupToken(field.MetadataToken);
        }        
    }
}
