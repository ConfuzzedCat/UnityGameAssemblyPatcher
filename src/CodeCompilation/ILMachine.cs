using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Exceptions;
using UnityGameAssemblyPatcher.Extensions;

namespace UnityGameAssemblyPatcher.src.CodeCompilation
{
    internal class ILMachine
    {

        internal static void Emit(MethodDefinition method, MethodDefinition methodRef, InjectionLocation loc)
        {            
            var processor = method.Body.GetILProcessor();
            var newInstruction = processor.Create(OpCodes.Call, methodRef);

            switch (loc)
            {
                case InjectionLocation.Prefix:
                    var firstInstruction = method.Body.Instructions[0];
                    processor.InsertBefore(firstInstruction, newInstruction);
                    break;
                case InjectionLocation.Postfix:
                    var lastInstruction = GetReturnInstruction(method);
                    processor.InsertBefore(lastInstruction, newInstruction);
                    break;
            }

        }
        internal static Instruction GetReturnInstruction(MethodDefinition method)
        {
            Instruction[] instructions = method.Body.Instructions.ToArray();

            for (int i = instructions.Length - 1; i >= 0; i--)
            {
                if (instructions[i].OpCode == OpCodes.Ret)
                {
                    return instructions[i];
                }
            }
            throw new InstructionNotFoundException();
        }
    }
}
