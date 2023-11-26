using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameAssemblyPatcher.src.Exceptions
{
    internal class CompilerException : Exception
    {
        public CompilerException(string? message) : base(message)
        {
        }
    }
}
