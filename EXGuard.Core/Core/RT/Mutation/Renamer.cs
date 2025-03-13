using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

using dnlib.DotNet;
using dnlib.DotNet.Writer;

using EXGuard.Core.Helpers;
using EXGuard.Core.Services;

namespace EXGuard.Core.RT.Mutation
{
    internal class NameService
    {
        private ModuleDef RTMD;

        private readonly Dictionary<string, string> nameMap = new Dictionary<string, string>();
        private static RandomGenerator _RND = new RandomGenerator(32);

        public NameService(ModuleDef rt = null)
        {
            RTMD = rt;

            nameMap = new Dictionary<string, string>();
            _RND = new RandomGenerator(32);
        }

        public string NewName(string name)
        {
            string result;
            if (!nameMap.TryGetValue(name, out result))
            {
                result = nameMap[name] = Random_VMProtect_HEX();
            }
            return result;
        }

        private string Random_VMProtect_HEX()
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < 4; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(32 + (decimal)_RND.NextInt32('z') - 32)));
                builder.Append(ch);
            }

            /*/
             * HEX'e dönüştürülmesini istemiyorsan return kısmını builder.ToString() yap.
             * kaç sayıda random str üretmeyi seçmek için for (int i = 0; i < 4; i++) kısmındaki "4" kısmını değiştirebilirsin.
            /*/
            return string.Join(string.Empty, builder.ToString().Select(c => string.Format("{0:X2}", System.Convert.ToInt32(c))).ToArray());
        }

        public void Process()
        {
            foreach (var type in RTMD.GetTypes())
            {
                if (type.Name == RTMap.Mutation)
                {
                    type.Namespace = string.Empty;
                    type.Name = NewName(type.Name);

                    RTMap.Mutation = type.Name;

                    foreach (var method in type.Methods)
                    {
                        if (method.Name == RTMap.Mutation_Placeholder)
                        {
                            method.Name = NewName(method.Name);
                            RTMap.Mutation_Placeholder = method.Name;
                        }
                        else if (method.Name == RTMap.Mutation_Placeholder)
                        {
                            method.Name = NewName(method.Name);
                            RTMap.Mutation_Placeholder = method.Name;
                        }
                        else if(method.Name == RTMap.Mutation_Value_T)
                        {
                            method.Name = NewName(method.Name);
                            RTMap.Mutation_Value_T = method.Name;
                        }
                        else if (method.Name == RTMap.Mutation_Value_T_Arg0)
                        {
                            method.Name = NewName(method.Name);
                            RTMap.Mutation_Value_T_Arg0 = method.Name;
                        }
                        else if (method.Name == RTMap.Mutation_Crypt)
                        {
                            method.Name = NewName(method.Name);
                            RTMap.Mutation_Crypt = method.Name;
                        }

                        #region Rename MutationType Method Parameters
                        /////////////////////////////////////////////
                        foreach (var Param in method.Parameters)
                        {
                            Param.Name = NewName(Param.Name);
                        }
                        /////////////////////////////////////////////
                        #endregion
                    }

                    foreach (var field in type.Fields)
                    {
                        #region Replace Mutation Key Name
                        //////////////////////////////////////////////////////////////////////////////
                        switch (field.Name)
                        {
                            #region IntKey
                            ////////////////////////////////////////////////////////
                            case "IntKey0":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 0;
                                break;
                            case "IntKey1":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 1;
                                break;
                            case "IntKey2":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 2;
                                break;
                            case "IntKey3":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 3;
                                break;
                            case "IntKey4":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 4;
                                break;
                            case "IntKey5":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 5;
                                break;
                            case "IntKey6":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 6;
                                break;
                            case "IntKey7":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 7;
                                break;
                            case "IntKey8":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 8;
                                break;
                            case "IntKey9":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 9;
                                break;
                            case "IntKey10":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 10;
                                break;
                            case "IntKey11":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 11;
                                break;
                            case "IntKey12":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 12;
                                break;
                            case "IntKey13":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 13;
                                break;
                            case "IntKey14":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 14;
                                break;
                            case "IntKey15":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 15;
                                break;
                            case "IntKey16":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 16;
                                break;
                            case "IntKey17":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 17;
                                break;
                            case "IntKey18":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 18;
                                break;
                            case "IntKey19":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 19;
                                break;
                            case "IntKey20":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2IntIndex[field.Name] = 20;
                                break;
                            ////////////////////////////////////////////////////////
                            #endregion

                            #region LongKey
                            ////////////////////////////////////////////////////////
                            case "LongKey0":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 0;
                                break;
                            case "LongKey1":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 1;
                                break;
                            case "LongKey2":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 2;
                                break;
                            case "LongKey3":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 3;
                                break;
                            case "LongKey4":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 4;
                                break;
                            case "LongKey5":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 5;
                                break;
                            case "LongKey6":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 6;
                                break;
                            case "LongKey7":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 7;
                                break;
                            case "LongKey8":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 8;
                                break;
                            case "LongKey9":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 9;
                                break;
                            case "LongKey10":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 10;
                                break;
                            case "LongKey11":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 11;
                                break;
                            case "LongKey12":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 12;
                                break;
                            case "LongKey13":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 13;
                                break;
                            case "LongKey14":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 14;
                                break;
                            case "LongKey15":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 15;
                                break;
                            case "LongKey16":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 16;
                                break;
                            case "LongKey17":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 17;
                                break;
                            case "LongKey18":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 18;
                                break;
                            case "LongKey19":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 19;
                                break;
                            case "LongKey20":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LongIndex[field.Name] = 20;
                                break;
                            ////////////////////////////////////////////////////////
                            #endregion

                            #region LongKey
                            ////////////////////////////////////////////////////////
                            case "ULongKey0":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 0;
                                break;
                            case "ULongKey1":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 1;
                                break;
                            case "ULongKey2":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 2;
                                break;
                            case "ULongKey3":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 3;
                                break;
                            case "ULongKey4":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 4;
                                break;
                            case "ULongKey5":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 5;
                                break;
                            case "ULongKey6":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 6;
                                break;
                            case "ULongKey7":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 7;
                                break;
                            case "ULongKey8":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 8;
                                break;
                            case "ULongKey9":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 9;
                                break;
                            case "ULongKey10":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 10;
                                break;
                            case "ULongKey11":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 11;
                                break;
                            case "ULongKey12":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 12;
                                break;
                            case "ULongKey13":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 13;
                                break;
                            case "ULongKey14":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 14;
                                break;
                            case "ULongKey15":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 15;
                                break;
                            case "ULongKey16":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 16;
                                break;
                            case "ULongKey17":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 17;
                                break;
                            case "ULongKey18":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 18;
                                break;
                            case "ULongKey19":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 19;
                                break;
                            case "ULongKey20":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2ULongIndex[field.Name] = 20;
                                break;
                            ////////////////////////////////////////////////////////
                            #endregion

                            #region LdstrKey
                            /////////////////////////////////////////////////////////////////////////////////////////////
                            case "LdstrKey0":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(0);
                                break;
                            case "LdstrKey1":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(1);
                                break;
                            case "LdstrKey2":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(2);
                                break;
                            case "LdstrKey3":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(3);
                                break;
                            case "LdstrKey4":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(4);
                                break;
                            case "LdstrKey5":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(5);
                                break;
                            case "LdstrKey6":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(6);
                                break;
                            case "LdstrKey7":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(7);
                                break;
                            case "LdstrKey8":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(8);
                                break;
                            case "LdstrKey9":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(9);
                                break;
                            case "LdstrKey10":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(10);
                                break;
                            case "LdstrKey11":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(11);
                                break;
                            case "LdstrKey12":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(12);
                                break;
                            case "LdstrKey13":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(13);
                                break;
                            case "LdstrKey14":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(14);
                                break;
                            case "LdstrKey15":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(15);
                                break;
                            case "LdstrKey16":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(16);
                                break;
                            case "LdstrKey17":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(17);
                                break;
                            case "LdstrKey18":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(18);
                                break;
                            case "LdstrKey19":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(19);
                                break;
                            case "LdstrKey20":
                                field.Name = NewName(field.Name);
                                MutationHelper.Field2LdstrIndex[field.Name] = Convert.ToString(20);
                                break;
                                /////////////////////////////////////////////////////////////////////////////////////////////
                                #endregion
                        }
                        //////////////////////////////////////////////////////////////////////////////
                        #endregion
                    }
                }
                else
                {
                    if (type.FullName == "System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute" || 
                        type.FullName == "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute")
                        continue;

                    type.Namespace = string.Empty;
                    type.Name = NewName(type.Name);

                    foreach (var GenParam in type.GenericParameters)
                        GenParam.Name = NewName(GenParam.Name);

                    var isDelegate = type.BaseType != null &&
                                     (type.BaseType.FullName == "System.Delegate" ||
                                      type.BaseType.FullName == "System.MulticastDelegate");

                    foreach (var method in type.Methods)
                    {
                        if (method.HasBody)
                            foreach (var instr in method.Body.Instructions)
                            {
                                var memberRef = instr.Operand as MemberRef;
                                if (memberRef != null)
                                {
                                    var typeDef = memberRef.DeclaringType.ResolveTypeDef();

                                    if (memberRef.IsMethodRef && typeDef != null)
                                    {
                                        var target = typeDef.ResolveMethod(memberRef);
                                        if (target != null && target.IsRuntimeSpecialName)
                                            typeDef = null;
                                    }

                                    if (typeDef != null && typeDef.Module == RTMD)
                                        memberRef.Name = NewName(memberRef.Name);
                                }
                            }

                        foreach (var Param in method.Parameters)
                            Param.Name = NewName(Param.Name);

                        if (method.IsRuntimeSpecialName || isDelegate)
                            continue;

                        method.Name = NewName(method.Name);
                    }


                    for (var i = 0; i < type.Fields.Count; i++)
                    {
                        var field = type.Fields[i];
                        if (field.IsLiteral)
                        {
                            type.Fields.RemoveAt(i--);
                            continue;
                        }

                        if (field.IsRuntimeSpecialName)
                            continue;

                        field.Name = NewName(field.Name);
                    }

                    type.Properties.Clear();
                    type.Events.Clear();
                }
            }
        }
    }
}