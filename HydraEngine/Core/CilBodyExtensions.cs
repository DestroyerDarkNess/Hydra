using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace HydraEngine.Core
{
    public static class CilBodyExtensions
    {
        public static CilBody Clone(this CilBody original)
        {
            var newBody = new CilBody
            {
                InitLocals = original.InitLocals,
                MaxStack = original.MaxStack,
                KeepOldMaxStack = original.KeepOldMaxStack
            };

            // Clonar variables locales
            foreach (var local in original.Variables)
            {
                newBody.Variables.Add(new Local(local.Type));
            }

            // Clonar instrucciones
            var instrMap = new Dictionary<Instruction, Instruction>();
            foreach (var instr in original.Instructions)
            {
                var newInstr = new Instruction(instr.OpCode, instr.Operand)
                {
                    SequencePoint = instr.SequencePoint
                };
                instrMap[instr] = newInstr;
                newBody.Instructions.Add(newInstr);
            }

            // Corregir referencias en operandos
            foreach (var instr in newBody.Instructions)
            {
                switch (instr.Operand)
                {
                    case Instruction target:
                        instr.Operand = instrMap[target];
                        break;
                    case IList<Instruction> targets:
                        var newTargets = new List<Instruction>();
                        foreach (var target in targets)
                            newTargets.Add(instrMap[target]);
                        instr.Operand = newTargets;
                        break;
                }
            }

            // Clonar manejadores de excepción
            foreach (var eh in original.ExceptionHandlers)
            {
                newBody.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                {
                    TryStart = instrMap[eh.TryStart],
                    TryEnd = instrMap[eh.TryEnd],
                    HandlerStart = instrMap[eh.HandlerStart],
                    HandlerEnd = instrMap[eh.HandlerEnd],
                    FilterStart = eh.FilterStart != null ? instrMap[eh.FilterStart] : null,
                    CatchType = eh.CatchType
                });
            }

            return newBody;
        }
    }
}
