using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityGameAssemblyPatcher.Enums;
using UnityGameAssemblyPatcher.Exceptions;
using UnityGameAssemblyPatcher.Extensions;

namespace UnityGameAssemblyPatcher.CodeCompilation
{
    internal class ILMachine
    {

        internal static ILogger logger = Logging.GetLogger<ILMachine>();
        
        internal static bool Emit(ModuleDefinition module, ref MethodDefinition method, MethodInfo patchMethod, InjectionLocation loc)
        {
            if (!method.HasBody)
            {
                logger.Warning("Target method doesn't have a body. Method: {0}", method.FullName);
                return false;
            }
            
            var processor = method.Body.GetILProcessor();

            var patchMethodImport = module.ImportReference(patchMethod);

            var newInstruction = processor.Create(OpCodes.Call, patchMethodImport);

            if(IsMethodPatched(method, newInstruction, loc))
            {
                logger.Information("Method already patched at {0}. Skipping...", loc);
                return true;
            }

            processor.Body.SimplifyMacros();
            switch (loc)
            {
                case InjectionLocation.Prefix:
                    var firstInstruction = method.Body.Instructions[0];
                    processor.InsertBefore(firstInstruction, newInstruction);
                    break;
                case InjectionLocation.Postfix:
                    var lastInstruction = GetReturnInstruction(method);
                    foreach (var retInst in lastInstruction)
                    {
                        processor.InsertBefore(retInst, newInstruction);
                    }
                    break;
            }
            processor.Body.OptimizeMacros();
            logger.Information("Injected method {0} into {1} as {2}", patchMethod.Name, method.FullName, loc);
            return true;
        }
        internal static List<Instruction> GetReturnInstruction(MethodDefinition method)
        {
            Instruction[] instructions = method.Body.Instructions.ToArray();
            List<Instruction> result = new List<Instruction>();

            for (int i = instructions.Length - 1; i >= 0; i--)
            {
                if (instructions[i].OpCode == OpCodes.Ret)
                {
                    result.Add(instructions[i]);
                }
            }
            if(result.Count == 0)
            {
                throw new InstructionNotFoundException();
            }
            logger.Verbose("Found {0} ret opcodes in method: {1}", result.Count, method.FullName);
            return result;
        }
        internal static bool IsMethodPatched(MethodDefinition method, Instruction instruction, InjectionLocation location)
        {
            Instruction ins;
            switch (location)
            {
                case InjectionLocation.Prefix:
                    ins = method.Body.Instructions[1];
                    if (ins.OpCode == OpCodes.Call && ins.EqualsCallOperand(instruction))
                    {
                        return true;
                    }
                    break;
                case InjectionLocation.Postfix:
                    for (int i = method.Body.Instructions.Count - 1; i >= 0; i--)
                    {
                        ins = method.Body.Instructions[i];
                        if (ins.OpCode == OpCodes.Call && ins.EqualsCallOperand(instruction))
                        {
                            if (i == 0)
                            {
                                return false;
                            }
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
    }
}
