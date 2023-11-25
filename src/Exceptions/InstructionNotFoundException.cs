using System.Runtime.Serialization;

namespace UnityGameAssemblyPatcher.Exceptions
{
    [Serializable]
    internal class InstructionNotFoundException : Exception
    {
        public InstructionNotFoundException()
        {
        }

        public InstructionNotFoundException(string? message) : base(message)
        {
        }

        public InstructionNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InstructionNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}