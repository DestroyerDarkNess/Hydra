using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.INT
{
    public static class CollatzConjecture
    {
        //https://en.wikipedia.org/wiki/Collatz_conjecture

        //if it does not return 1 for every positive integer
        //then we've solved a huge mathematical problem

        public static int ConjetMe(int i)
        {
            while (i != 1)
            {
                if (i % 2 == 0)
                {
                    i = i / 2;
                }
                else
                {
                    i = (3 * i) + 1;
                }
            }
            return i;
        }
    }

    public class AddIntPhase : Models.Protection
    {
        public AddIntPhase() : base("Protection.INT.Confusion", "INT Confusion", "Description for Renamer Phase") { }

        private Random Random = new Random();
        public MethodDef CollatzCtor;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                ModuleDefMD typeModule = ModuleDefMD.Load(typeof(CollatzConjecture).Module);
                MethodDef cctor = module.GlobalType.FindOrCreateStaticConstructor();
                TypeDef typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(CollatzConjecture).MetadataToken));
                IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                CollatzCtor = (MethodDef)members.Single(method => method.Name == "ConjetMe");

                foreach (var type in module.GetTypes())
                {
                    if (type.IsGlobalModuleType) continue;
                    foreach (var meth in type.Methods)
                    {
                        if (!meth.HasBody) continue;
                        {
                            for (var i = 0; i < meth.Body.Instructions.Count; i++)
                            {
                                if (!meth.Body.Instructions[i].IsLdcI4()) continue;
                                var numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                                var div = new Random(Guid.NewGuid().GetHashCode()).Next();
                                var num = numorig ^ div;

                                var nop = OpCodes.Nop.ToInstruction();

                                var local = new Local(meth.Module.ImportAsTypeSig(typeof(int)));
                                meth.Body.Variables.Add(local);

                                meth.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                                meth.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, meth.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                                meth.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                                meth.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                                meth.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                                meth.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                                meth.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                                meth.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                                meth.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                                meth.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, meth.Module.Import(typeof(float))));
                                meth.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                                meth.Body.Instructions.Insert(i + 12, nop);
                                i += 12;
                            }
                            meth.Body.SimplifyBranches();
                        }
                    }
                }

                foreach (var type in module.Types.ToArray())
                {
                    if (!AnalyzerPhase.CanRename(type)) continue;

                    foreach (var meth in type.Methods.ToArray())
                    {
                        if (!meth.HasBody) continue;

                        if (!meth.Body.HasInstructions) continue;

                        var instr = meth.Body.Instructions;
                        for (int i = 0; i < instr.Count; i++)
                        {
                            if (instr[i].OpCode == OpCodes.Ldc_I4)
                            {
                                ProtectIntegers(meth, i);
                                i += 10;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }


        public void ProtectIntegers(MethodDef method, int i)
        {
            ReplaceValue(method, i);
            OutelineValue(method, i);
        }

        public List<MethodDef> ProxyMethodConst = new List<MethodDef>();
        public List<MethodDef> ProxyMethodStr = new List<MethodDef>();

        public void OutelineValue(MethodDef method, int i)
        {

            if ((!ProxyMethodConst.Contains(method)))
            {
                for (int index = 0; index < method.Body.Instructions.Count; index++)
                {
                    Instruction instr = method.Body.Instructions[index];
                    if (instr.OpCode == OpCodes.Ldc_I4)
                    {
                        MethodDef proxy_method = CreateReturnMethodDef(instr.GetLdcI4Value(), method);
                        method.DeclaringType.Methods.Add(proxy_method);
                        ProxyMethodConst.Add(proxy_method);
                        instr.OpCode = OpCodes.Call;
                        instr.Operand = proxy_method;
                    }
                    else if (instr.OpCode == OpCodes.Ldc_R4)
                    {
                        MethodDef proxy_method = CreateReturnMethodDef(instr, method);
                        method.DeclaringType.Methods.Add(proxy_method);
                        ProxyMethodConst.Add(proxy_method);
                        instr.OpCode = OpCodes.Call;
                        instr.Operand = proxy_method;
                    }
                    else if (instr.Operand is string && instr.OpCode == OpCodes.Ldstr)
                    {
                        MethodDef proxy_method = CreateReturnMethodDef(instr, method);
                        method.DeclaringType.Methods.Add(proxy_method);
                        ProxyMethodConst.Add(proxy_method);
                        instr.OpCode = OpCodes.Call;
                        instr.Operand = proxy_method;
                    }
                }
            }
        }

        public MethodDef CreateReturnMethodDef(object constantvalue, MethodDef source_method)
        {
            CorLibTypeSig corlib = null;

            if (constantvalue is int)
            {
                corlib = source_method.Module.CorLibTypes.Int32;
            }
            else
            {
                if (constantvalue is Instruction)
                {
                    var abecede = constantvalue as Instruction;
                    constantvalue = abecede.Operand;
                }
            }
            if (constantvalue is float)
            {
                corlib = source_method.Module.CorLibTypes.Single;
            }
            if (constantvalue is string)
            {
                corlib = source_method.Module.CorLibTypes.String;
            }

            var meth = new MethodDefUser("_" + source_method.Name + "_" + constantvalue.ToString(),
                MethodSig.CreateStatic(corlib),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            { Body = new CilBody() };

            Local return_value = new Local(corlib);
            meth.Body.Variables.Add(return_value);

            //Method body
            meth.Body.Instructions.Add(OpCodes.Nop.ToInstruction());
            if (constantvalue is int)
            {
                meth.Body.Instructions.Add((int)constantvalue != 0
                    ? Instruction.Create(OpCodes.Ldc_I4, (Int32)constantvalue)
                    : Instruction.Create(OpCodes.Ldc_I4_0));
            }
            if (constantvalue is float)
            {
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (Single)constantvalue));
            }
            if (constantvalue is string)
            {
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, (string)constantvalue));
            }
            meth.Body.Instructions.Add(OpCodes.Stloc_0.ToInstruction());
            var test_ldloc = new Instruction(OpCodes.Ldloc_0);
            meth.Body.Instructions.Add(test_ldloc);
            meth.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            Instruction target = meth.Body.Instructions[3];
            meth.Body.Instructions.Insert(3, Instruction.Create(OpCodes.Br_S, target));
            return meth;
        }

        public void ReplaceValue(MethodDef method, int i)
        {
            var instr = method.Body.Instructions;
            if (instr[i].OpCode != OpCodes.Ldc_I4) return;
            var value = instr[i].GetLdcI4Value();
            if (value == 1)
                CollatzConjecture(method, i);
            if (value == 0)
                EmptyTypes(method, i);
        }

        public void CollatzConjecture(MethodDef method, int i)
        {
            var instr = method.Body.Instructions;
            instr[i].Operand = Random.Next(1, 15); //the created logic three should be little enough here
            method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Call, CollatzCtor));
        }

        public void EmptyTypes(MethodDef method, int i)
        {
            switch (Random.Next(0, 2))
            {
                case 0:
                    method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Add));
                    break;

                case 1:
                    method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Sub));
                    break;
            }
            method.Body.Instructions.Insert(i + 1,
                Instruction.Create(OpCodes.Ldsfld,
                    method.Module.Import((typeof(Type).GetField("EmptyTypes")))));
            method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldlen));
        }
    }
}
