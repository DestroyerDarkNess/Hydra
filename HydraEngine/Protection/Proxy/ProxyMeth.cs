using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
     public class ProxyMeth : Models.Protection
    {
        public ProxyMeth() : base("Protection.Proxy.ProxyMeth", "Renamer Phase", "Description for Renamer Phase") { }

        private  readonly Random Rand = new Random();
        private  readonly List<MemberRef> MemberRefList = new List<MemberRef>();

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                ScanMemberRef(module);
                foreach (TypeDef type in module.GetTypes())
                {
                    if (!Analyzer.CanRename(type)) continue;

                    foreach (MethodDef meth in type.Methods.ToArray())
                    {
                        if (!Analyzer.CanRename(meth)) continue;
                        if (!meth.HasBody || meth.Name.Contains("ProxyMeth")) continue;
                        var instr = meth.Body.Instructions;
                        for (var i = 0; i < instr.Count; i++)
                        {
                            if (meth.Body.Instructions[i].OpCode != OpCodes.Call) continue;
                            try
                            {
                                var original = (MemberRef)meth.Body.Instructions[i].Operand;
                                if (!original.HasThis)
                                {
                                    var proxy = GenerateSwitch(original, module);
                                    meth.DeclaringType.Methods.Add(proxy);
                                    instr[i].OpCode = OpCodes.Call;
                                    instr[i].Operand = proxy;
                                    var random = Rand.Next(0, 5);
                                    for (var b = 0; b < proxy.Body.Instructions.Count - 1; b++)
                                    {
                                        if (proxy.Body.Instructions[b].OpCode == OpCodes.Ldc_I4)
                                        {
                                            if (string.Compare(proxy.Body.Instructions[b].Operand.ToString(), random.ToString(), StringComparison.Ordinal) != 0)
                                            {
                                                proxy.Body.Instructions[b].OpCode = OpCodes.Call;
                                                proxy.Body.Instructions[b].Operand = MemberRefList.Where(m => m.MethodSig.Params.Count == original.MethodSig.Params.Count).ToList().Random();
                                            }
                                            else
                                            {
                                                proxy.Body.Instructions[b].OpCode = OpCodes.Call;
                                                proxy.Body.Instructions[b].Operand = original;
                                            }
                                        }
                                    }

                                    meth.Body.Instructions.Insert(i, Instruction.CreateLdcI4(random));

                                    //MethodSig originalsignature = original.MethodSig;
                                    //var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                                    //var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                                    //var meth1 = new MethodDefUser("ProxyMeth" + Rand.Next(0, int.MaxValue).ToString(),
                                    //           originalsignature,
                                    //            methImplFlags, methFlags);
                                    //module.GlobalType.Methods.Add(meth1);
                                    //meth1.Body = new CilBody();
                                    //for (int ia = 0; ia <= originalsignature.Params.Count - 1; ia++)
                                    //{
                                    //    meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, meth1.Parameters[ia]));
                                    //}
                                    //meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Call, original));
                                    //meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                    //instr[i].OpCode = OpCodes.Call;
                                    //instr[i].Operand = meth1;
                                }
                            }
                            catch
                            {
                                // ignored
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


        private void ScanMemberRef(ModuleDef module)
        {
            foreach (TypeDef type in module.Types)
            {
                if (!Analyzer.CanRename(type)) continue;
                foreach (MethodDef meth in type.Methods)
                {
                    if (!Analyzer.CanRename(meth)) continue;
                    if (!meth.HasBody || !meth.Body.HasInstructions) continue;
                    for (var i = 0; i < meth.Body.Instructions.Count - 1; i++)
                    {
                        if (meth.Body.Instructions[i].OpCode != OpCodes.Call) continue;
                        try
                        {
                            var original = (MemberRef)meth.Body.Instructions[i].Operand;
                            if (!original.HasThis)
                            {
                                MemberRefList.Add(original);
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }

        private  MethodDef GenerateSwitch(MemberRef original, ModuleDef md)
        {
            try
            {
                var type = original.MethodSig.Params.ToList();
                type.Add(md.CorLibTypes.Int32);
                var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                MethodDef meth = new MethodDefUser($"ProxyMeth{Rand.Next(0, int.MaxValue)}", MethodSig.CreateStatic(original.MethodSig.RetType, type.ToArray()), methImplFlags, methFlags)
                {
                    Body = new CilBody()
                };
                meth.Body.Variables.Add(new Local(md.CorLibTypes.Int32));
                meth.Body.Variables.Add(new Local(md.CorLibTypes.Int32));
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                var lst = new List<Instruction>();
                var switchs = new Instruction(OpCodes.Switch);
                meth.Body.Instructions.Add(switchs);
                var brS = new Instruction(OpCodes.Br_S);
                meth.Body.Instructions.Add(brS);
                for (var i = 0; i < 5; i++)
                {
                    for (var ia = 0; ia <= original.MethodSig.Params.Count - 1; ia++)
                    {
                        meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, meth.Parameters[ia]));
                        if (ia == 0)
                        {
                            lst.Add(Instruction.Create(OpCodes.Ldarg, meth.Parameters[ia]));
                        }
                    }
                    var ldstr = Instruction.Create(OpCodes.Ldc_I4, i);
                    meth.Body.Instructions.Add(ldstr);
                    meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                }

                var ldnull = Instruction.Create(OpCodes.Ldnull);
                meth.Body.Instructions.Add(ldnull);
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                brS.Operand = ldnull;
                switchs.Operand = lst;
                return meth;
            }
            catch
            {
                return null;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }

    public static class EnumerableHelper
    {
        private static readonly Random R;

        static EnumerableHelper()
        {
            R = new Random();
        }

        public static TE Random<TE>(IEnumerable<TE> input)
        {
            var enumerable = input as TE[] ?? input.ToArray();
            return enumerable.ElementAt(R.Next(enumerable.Length));
        }
    }

    public static class EnumerableExtensions
    {
        public static T Random<T>(this IEnumerable<T> input)
        {
            return EnumerableHelper.Random(input);
        }
    }

}
