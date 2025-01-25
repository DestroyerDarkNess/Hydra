using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.VM;
using System.Collections.Generic;
using System.Linq;

namespace VM.Core.Protections.Impl.Virtualization.RuntimeProtections
{
    internal static class CallToCalli
    {
        private static List<string> Blacklist = new List<string>() { "GetMethodParameters" }; //Issues when applied
        internal static void Execute(ModuleDefMD Module)
        {
            if(Module == Virtualizer.Instance.RTModule)
            {
                foreach (var Type in Module.GetTypes().Where(T => T.HasMethods && !T.IsGlobalModuleType && !T.Name.Contains("AssemblyLoader")).ToArray())
                    foreach (var Method in Type.Methods.Where(M => M.HasBody && M.Body.HasInstructions && !Blacklist.Contains(M.Name)).ToArray())
                        Process(Method);
            } else
            {
                foreach (var Method in Module.GlobalType.Methods.Where(M => M.HasBody && M.Body.HasInstructions).ToArray())
                    Process(Method);
            }
        }
        internal static void Process(MethodDef Method)
        {
            for (var i = 0; i < Method.Body.Instructions.Count; i++)
            {
                Instruction Instruction = Method.Body.Instructions[i];
                if (Instruction.OpCode == OpCodes.Call)
                    if (Instruction.Operand is MethodDef M)
                    {
                        Method.Body.Instructions[i] = new Instruction(OpCodes.Ldftn, M);
                        Method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Nop));
                        Method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Calli, M.MethodSig));
                    }
            }
        }
    }
}