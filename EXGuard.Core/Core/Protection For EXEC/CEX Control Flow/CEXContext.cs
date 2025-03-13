using System;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Emit;

using EXGuard.DynCipher;
using EXGuard.Core.Helpers;
using EXGuard.Core.Services;

namespace EXGuard.Core.EXECProtections.CEXCFlow
{
    public class CEXContext
	{
        public int Depth
        {
            get;
            set;
        }

        public DynCipherService DynCipher
        {
            get;
            set;
        }

        public double Intensity
        {
            get;
            set;
        }

        public bool JunkCode
        {
            get;
            set;
        }

        public MethodDef Method
        {
            get;
            set;
        }

        public RandomGenerator Random
        {
            get;
            set;
        }

        public void ProcessMethod(CilBody body, CEXContext ctx)
        {
            var root = BlockParser.ParseBody(body);
            new SwitchMangler().Mangle(body, root, ctx);

            body.Instructions.Clear();
            root.ToBody(body);

            if (body.PdbMethod != null)
            {
                body.PdbMethod = new PdbMethod()
                {
                    Scope = new PdbScope()
                    {
                        Start = body.Instructions.First(),
                        End = body.Instructions.Last()
                    }
                };
            }

            foreach (ExceptionHandler eh in body.ExceptionHandlers)
            {
                var index = body.Instructions.IndexOf(eh.TryEnd) + 1;
                eh.TryEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
                index = body.Instructions.IndexOf(eh.HandlerEnd) + 1;
                eh.HandlerEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
            }

            body.KeepOldMaxStack = true;
        }

        public void AddJump(IList<Instruction> instrs, Instruction target)
        {
            instrs.Add(Instruction.Create(OpCodes.Br, target));
        }

        public void AddJunk(IList<Instruction> instrs)
        {
            if (Method.Module.IsClr40 || !JunkCode)
                return;

            switch (Random.NextInt32(6))
            {
                case 0:
                    instrs.Add(Instruction.Create(OpCodes.Pop));
                    break;
                case 1:
                    instrs.Add(Instruction.Create(OpCodes.Dup));
                    break;
                case 2:
                    instrs.Add(Instruction.Create(OpCodes.Throw));
                    break;
                case 3:
                    instrs.Add(Instruction.Create(OpCodes.Ldarg, new Parameter(0xff)));
                    break;
                case 4:
                    instrs.Add(Instruction.Create(OpCodes.Ldloc, new Local(null, null, 0xff)));
                    break;
                case 5:
                    instrs.Add(Instruction.Create(OpCodes.Ldtoken, Method));
                    break;
            }
        }
    }
}

