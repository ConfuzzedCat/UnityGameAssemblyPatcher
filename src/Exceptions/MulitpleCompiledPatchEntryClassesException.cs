using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameAssemblyPatcher.Exceptions
{
    internal class MulitpleCompiledPatchEntryClassesException : Exception
    {
        internal MulitpleCompiledPatchEntryClassesException(string? message) : base(message)
        {
        }

        internal static string FormatTypesForErrorMessage(List<Type> derivedTypes)
        {
            StringBuilder result = new();

            foreach (Type type in derivedTypes)
            {
                result.AppendFormat("\"{0}\", ", type.Name);
            }

            // Remove the trailing ", " if the list is not empty
            if (result.Length > 2)
            {
                result.Length -= 2;
            }
            return result.ToString();
        }
    }
}
