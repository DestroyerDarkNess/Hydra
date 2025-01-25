using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HydraEngine.Protection.Misc
{
    public class JunkCode : Models.Protection
    {
        public JunkCode() : base("Protection.Misc.JunkAttributes", "Renamer Phase", "Description for Renamer Phase") { }

        public string tag { get; set; } = "HailHydra";
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" + "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्";
        public int number { get; set; } = 100;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                for (int i = 0; i < number; i++)
                {
                    var junkatrb = new TypeDefUser(tag + RandomString(BaseChars, 20), tag + RandomString(BaseChars, 20), module.CorLibTypes.Object.TypeDefOrRef);
                    module.Types.Add(junkatrb);
                }

                foreach (TypeDef type in module.GetTypes())
                {
                    for (int ii = 0; ii < 100 * 4; ii++)
                    {
                        var meth1 = new MethodDefUser(tag + RandomString(BaseChars, 20), MethodSig.CreateStatic(module.CorLibTypes.Void), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot);
                        type.Methods.Add(meth1);

                        meth1.Body = new CilBody()
                        {

                            Instructions =
                        {
                            Instruction.Create(OpCodes.Ldnull),
                        Instruction.Create(OpCodes.Throw)
                        }
                        };

                        //mod.GlobalType.Methods.Add(meth1);
                    }
                }
                for (int i = 0; i < number; i++)
                {

                    try
                    {
                        var junk2 = new TypeDefUser(tag + RandomString(BaseChars, 20), tag + RandomString(BaseChars, 20), module.CorLibTypes.Object.TypeDefOrRef);

                        module.Types.Add(junk2);

                        for (int ii = 0; ii < 100; ii++)
                        {
                            var meth1 = new MethodDefUser(tag + RandomString(BaseChars, 20), MethodSig.CreateStatic(module.CorLibTypes.Object), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot);
                            junk2.Methods.Add(meth1);

                            meth1.Body = new CilBody()
                            {


                                Variables =
                    {
                        new Local(module.CorLibTypes.Object)
                    },
                                Instructions =
                        {
                            Instruction.Create(OpCodes.Nop),
                        Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)13),
                        Instruction.Create(OpCodes.Newarr, module.CorLibTypes.Object),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_0),
                        Instruction.Create(OpCodes.Ldstr, "H"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_1),
                        Instruction.Create(OpCodes.Ldstr, "a"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_2),
                        Instruction.Create(OpCodes.Ldstr, "i"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_3),
                        Instruction.Create(OpCodes.Ldstr, "l"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_4),
                        Instruction.Create(OpCodes.Ldstr, "H"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_5),
                        Instruction.Create(OpCodes.Ldstr, "y"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_6),
                        Instruction.Create(OpCodes.Ldstr, "d"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_7),
                        Instruction.Create(OpCodes.Ldstr, "r"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_8),
                        Instruction.Create(OpCodes.Ldstr, "a"),
                        Instruction.Create(OpCodes.Stelem_Ref),
                        Instruction.Create(OpCodes.Stloc_0),
                        Instruction.Create(OpCodes.Nop), // 56 => 57
                        Instruction.Create(OpCodes.Ldloc_0),
                        Instruction.Create(OpCodes.Ret)
                    }
                            };
                            meth1.Body.Instructions[56].OpCode = OpCodes.Br_S;
                            meth1.Body.Instructions[56].Operand = meth1.Body.Instructions[57];

                            //mod.GlobalType.Methods.Add(meth1);
                        }
                    }
                    catch
                    {
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

        private  Random random = new Random();
        private string RandomString(string chars, int length)
        {
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
