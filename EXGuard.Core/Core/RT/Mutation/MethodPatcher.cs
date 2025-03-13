using System;
using System.Reflection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EXGuard.Core.RT.Mutation {
	internal class MethodPatcher {
		public static void Patch(RuntimeSearch rtscr, MethodDef method) {
            var body = new CilBody();
            method.Body = body;

            body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, method.Module.Import((FieldDef)rtscr.VMEntry_Invoke[4])));

            if (method.Parameters.Count == 0)
                body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            else
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, method.Parameters.Count));
                body.Instructions.Add(Instruction.Create(OpCodes.Newarr, method.Module.CorLibTypes.Object.ToTypeDefOrRef()));

                foreach (var param in method.Parameters)
                {
                    body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                    body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, param.Index));
                    body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, param));

                    if (param.Type.IsByRef)
                    {
                        body.Instructions.Add(Instruction.Create(OpCodes.Mkrefany, param.Type.Next.ToTypeDefOrRef()));
                        body.Instructions.Add(Instruction.Create(OpCodes.Newobj, method.Module.Import(rtscr.TypedRef_Ctor)));
                    }
                    else if (param.Type.IsPointer)
                    {
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, param.Type.ToTypeDefOrRef()));
                        body.Instructions.Add(Instruction.Create(OpCodes.Call,
                       method.Module.Import(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static))));

                        body.Instructions.Add(Instruction.Create(OpCodes.Call,
                            method.Module.Import(typeof(Pointer).GetMethod("Box", BindingFlags.Public | BindingFlags.Static))));
                    }
                    else if (param.Type.IsValueType)
                        body.Instructions.Add(Instruction.Create(OpCodes.Box, param.Type.ToTypeDefOrRef()));
                    else
                        body.Instructions.Add(Instruction.Create(OpCodes.Box, param.Type.ToTypeDefOrRef()));

                    body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }
            }

            body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, method));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, method.Module.Import((MethodDef)rtscr.VMEntry_Invoke[3])));
            body.Instructions.Add(Instruction.Create(OpCodes.Calli, method.Module.Import((MethodDef)rtscr.VMEntry_Invoke[3]).MethodSig));

            TypeSig retType = null;
            if (method.ReturnType.Next is null)
                retType = method.ReturnType;
            else
                retType = method.ReturnType.Next;

            if (method.ReturnType.IsPointer || (retType.IsPointer && method.ReturnType.IsByRef))
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Call,
                    method.Module.Import(typeof(Pointer).GetMethod("Unbox", BindingFlags.Public | BindingFlags.Static))));

                if (method.ReturnType.IsByRef)
                {
                    if (method.ReturnType.Next.ToTypeDefOrRef().FullName == "System.Void*")
                        retType = null;
                }
                else
                {
                    if (method.ReturnType.ToTypeDefOrRef().FullName == "System.Void*")
                        retType = null;
                }
            }

            if (method.ReturnType.ElementType == ElementType.Void)
                body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            else if (method.ReturnType.IsValueType)
            {
                if (method.ReturnType.ToTypeDefOrRef().FullName != "System.Object&" && retType != null)
                    if (method.ReturnType.IsByRef)
                        body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.ReturnType.Next.ToTypeDefOrRef()));
                    else
                        body.Instructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.ReturnType.ToTypeDefOrRef()));
            }
            else
            {
                if (method.ReturnType.ToTypeDefOrRef().FullName != "System.Object&" && retType != null)
                    if (method.ReturnType.IsByRef)
                        body.Instructions.Add(Instruction.Create(OpCodes.Castclass, method.ReturnType.Next.ToTypeDefOrRef()));
                    else
                        body.Instructions.Add(Instruction.Create(OpCodes.Castclass, method.ReturnType.ToTypeDefOrRef()));
            }

            if (method.ReturnType.ElementType != ElementType.Void)
                if (method.ReturnType.IsByRef)
                {
                    var retLoc = new Local(method.ReturnType.Next);
                    body.Variables.Add(retLoc);

                    body.Instructions.Add(Instruction.Create(OpCodes.Stloc, retLoc));
                    body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, retLoc));
                }

            body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            body.OptimizeMacros();
            body.OptimizeBranches();

            // Old Hide Method
            ///////////////////////////////////////////////////////////////////////////////////////
            body.Instructions.Insert(1, new Instruction(OpCodes.Br_S, body.Instructions[1]));
            body.Instructions.Insert(2, new Instruction(OpCodes.Unaligned, (byte)0));
            ///////////////////////////////////////////////////////////////////////////////////////
        }
    }
}