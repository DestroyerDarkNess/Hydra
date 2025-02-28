using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.Core
{
    public static class DnlibUtils
    {
        public static bool CanObfuscate(IList<Instruction> instructions, int i)
        {
            try
            {
                if (instructions[i + 1].GetOperand() != null && instructions[i + 1].Operand.ToString().Contains("bool"))
                {
                    return false;
                }
                if (instructions[i + 1].GetOpCode() == dnlib.DotNet.Emit.OpCodes.Newobj)
                {
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }

        public static void excludeMethod(this MethodDef method, ModuleDef module)
        {
            TypeRef typeRef = module.CorLibTypes.GetTypeRef("System", "ObsoleteAttribute");
            MemberRefUser ctor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), typeRef);
            CustomAttribute customAttribute = new CustomAttribute(ctor);
            customAttribute.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, "Exclude"));
            method.CustomAttributes.Add(customAttribute);
        }

        public static bool MethodHasL2FAttribute(this MethodDef method)
        {
            foreach (CustomAttribute customAttribute in method.CustomAttributes)
            {
                if (!(customAttribute.AttributeType.FullName == "System.ObsoleteAttribute"))
                {
                    continue;
                }
                foreach (CAArgument constructorArgument in customAttribute.ConstructorArguments)
                {
                    if (constructorArgument.Type.ElementType == ElementType.String && constructorArgument.Value.ToString() == "Exclude")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool InheritsFrom(this TypeDef type, string baseType)
        {
            if (type.BaseType == null)
            {
                return false;
            }
            TypeDef typeDef = type;
            do
            {
                typeDef = typeDef.BaseType.ResolveTypeDefThrow();
                if (typeDef.ReflectionFullName == baseType)
                {
                    return true;
                }
            }
            while (typeDef.BaseType != null);
            return false;
        }

        public static bool InheritsFromCorlib(this TypeDef type, string baseType)
        {
            if (type.BaseType == null)
            {
                return false;
            }
            TypeDef typeDef = type;
            do
            {
                typeDef = typeDef.BaseType.ResolveTypeDefThrow();
                if (typeDef.ReflectionFullName == baseType)
                {
                    return true;
                }
            }
            while (typeDef.BaseType != null && typeDef.BaseType.DefinitionAssembly.IsCorLib());
            return false;
        }

        public static bool Implements(this TypeDef type, string fullName)
        {
            foreach (InterfaceImpl @interface in type.Interfaces)
            {
                if (@interface.Interface.ReflectionFullName == fullName)
                {
                    return true;
                }
            }
            if (type.BaseType == null)
            {
                return false;
            }
            return false;
        }

        public static bool IsPublic(this PropertyDef property)
        {
            return property.AllMethods().Any((MethodDef method) => method.IsPublic);
        }

        public static bool IsPublic(this EventDef evt)
        {
            return evt.AllMethods().Any((MethodDef method) => method.IsPublic);
        }

        private static IEnumerable<MethodDef> AllMethods(this EventDef evt)
        {
            return from m in new MethodDef[3] { evt.AddMethod, evt.RemoveMethod, evt.InvokeMethod }.Concat(evt.OtherMethods)
                   where m != null
                   select m;
        }

        private static IEnumerable<MethodDef> AllMethods(this PropertyDef property)
        {
            return from m in new MethodDef[2] { property.GetMethod, property.SetMethod }.Concat(property.OtherMethods)
                   where m != null
                   select m;
        }

        public static bool IsVisibleOutside(this TypeDef typeDef, bool exeNonPublic = true, bool hideInternals = true)
        {
            if (exeNonPublic && (typeDef.Module.Kind == ModuleKind.Windows || typeDef.Module.Kind == ModuleKind.Console))
            {
                return false;
            }
            if (typeDef.IsSerializable)
            {
                return true;
            }
            do
            {
                if (typeDef.DeclaringType != null)
                {
                    if (hideInternals)
                    {
                        if (!typeDef.IsNestedPublic && !typeDef.IsNestedFamily && !typeDef.IsNestedFamilyOrAssembly)
                        {
                            return false;
                        }
                    }
                    else if ((typeDef.IsNotPublic || typeDef.IsNestedPrivate) && !typeDef.IsNestedPublic && !typeDef.IsNestedFamily && !typeDef.IsNestedFamilyOrAssembly)
                    {
                        return false;
                    }
                    typeDef = typeDef.DeclaringType;
                    continue;
                }
                return typeDef.IsPublic || !hideInternals;
            }
            while (typeDef != null);
            throw new Exception();
        }

        public static bool IsDelegate(this TypeDef type)
        {
            if (type.BaseType == null)
            {
                return false;
            }
            string fullName = type.BaseType.FullName;
            return fullName == "System.Delegate" || fullName == "System.MulticastDelegate";
        }

        public static bool IsComImport(this TypeDef type)
        {
            return type.IsImport || type.HasAttribute("System.Runtime.InteropServices.ComImportAttribute") || type.HasAttribute("System.Runtime.InteropServices.TypeLibTypeAttribute");
        }

        public static bool HasAttribute(this IHasCustomAttribute obj, string fullName)
        {
            return obj.CustomAttributes.Any((CustomAttribute attr) => attr.TypeFullName == fullName);
        }

        public static byte[] GetILAsByteArray(this CilBody body)
        {
            List<byte> list = new List<byte>();
            foreach (Instruction instruction in body.Instructions)
            {
                byte[] bytes = BitConverter.GetBytes(instruction.GetOpCode().Value);
                byte[] array = bytes;
                byte[] array2 = array;
                foreach (byte item in array2)
                {
                    list.Add(item);
                }
            }
            return list.ToArray();
        }

        public static void EnsureNoInlining(MethodDef method)
        {
            method.ImplAttributes &= ~MethodImplAttributes.AggressiveInlining;
            method.ImplAttributes |= MethodImplAttributes.NoInlining;
        }

        public static void HideMethods(MethodDef methodDef)
        {
            methodDef.Body.Instructions.Insert(1, new Instruction(dnlib.DotNet.Emit.OpCodes.Br_S, methodDef.Body.Instructions[1]));
            methodDef.Body.Instructions.Insert(2, new Instruction(dnlib.DotNet.Emit.OpCodes.Unaligned, 0));
        }

        public static void InsertInstructions(IList<Instruction> instructions, Dictionary<Instruction, int> keyValuePairs)
        {
            foreach (KeyValuePair<Instruction, int> keyValuePair in keyValuePairs)
            {
                Instruction key = keyValuePair.Key;
                int value = keyValuePair.Value;
                instructions.Insert(value, key);
            }
        }

        public static MethodDef ResolveThrow(this IMethod method)
        {
            if (method is MethodDef result)
            {
                return result;
            }
            if (method is MethodSpec methodSpec)
            {
                return methodSpec.Method.ResolveThrow();
            }
            return ((MemberRef)method).ResolveMethodThrow();
        }

        public static bool hasExceptionHandlers(MethodDef methodDef)
        {
            if (methodDef.Body.HasExceptionHandlers)
            {
                return true;
            }
            return false;
        }

        public static bool HasUnsafeInstructions(MethodDef methodDef)
        {
            if (methodDef.HasBody && methodDef.Body.HasVariables)
            {
                return methodDef.Body.Variables.Any((Local x) => x.Type.IsPointer);
            }
            return false;
        }

        public static List<Instruction> Calc(int value)
        {
            List<Instruction> list = new List<Instruction>();
            Random random = new Random(Guid.NewGuid().GetHashCode());
            int num = random.Next(0, 100000);
            int num2 = random.Next(0, 100000);
            bool flag = Convert.ToBoolean(random.Next(0, 2));
            list.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Ldc_I4, value - num + (flag ? (-num2) : num2)));
            list.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Ldc_I4, num));
            list.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Add));
            list.Add(Instruction.Create(dnlib.DotNet.Emit.OpCodes.Ldc_I4, num2));
            list.Add(Instruction.Create(flag ? dnlib.DotNet.Emit.OpCodes.Add : dnlib.DotNet.Emit.OpCodes.Sub));
            return list;
        }

        public static bool Simplify(MethodDef methodDef)
        {
            if (methodDef.Parameters == null)
            {
                return false;
            }
            methodDef.Body.SimplifyMacros(methodDef.Parameters);
            methodDef.Body.SimplifyBranches();
            return true;
        }

        public static bool Optimize(MethodDef methodDef)
        {
            if (methodDef.Body == null)
            {
                return false;
            }
            methodDef.Body.OptimizeMacros();
            methodDef.Body.OptimizeBranches();
            return true;
        }

        public static bool InGlobalModuleType(this MethodDef method)
        {
            return method.DeclaringType.IsGlobalModuleType || method.DeclaringType2.IsGlobalModuleType || method.FullName.Contains("My.");
        }

        public static bool InGlobalModuleType(this TypeDef type)
        {
            return type.IsGlobalModuleType || (type.DeclaringType != null && type.DeclaringType.IsGlobalModuleType) || (type.DeclaringType2 != null && type.DeclaringType2.IsGlobalModuleType);
        }

        public static void MergeCall(this CilBody targetBody, Instruction callInstruction)
        {
            if (!(callInstruction.Operand is MethodDef methodDef))
            {
                throw new ArgumentException("Call instruction has invalid operand");
            }
            if (!methodDef.HasBody)
            {
                throw new Exception("Method to merge has no body!");
            }
            Dictionary<int, Local> dictionary = methodDef.Parameters.ToDictionary((Parameter param) => param.Index, (Parameter param) => new Local(param.Type));
            Dictionary<Local, Local> dictionary2 = methodDef.Body.Variables.ToDictionary((Local local) => local, (Local local) => new Local(local.Type));
            foreach (KeyValuePair<int, Local> item in dictionary)
            {
                targetBody.Variables.Add(item.Value);
            }
            foreach (KeyValuePair<Local, Local> item2 in dictionary2)
            {
                targetBody.Variables.Add(item2.Value);
            }
            int num = targetBody.Instructions.IndexOf(callInstruction) + 1;
            callInstruction.OpCode = dnlib.DotNet.Emit.OpCodes.Nop;
            callInstruction.Operand = null;
            Instruction operand = targetBody.Instructions[num];
            int num2 = 0;
            foreach (dnlib.DotNet.Emit.ExceptionHandler exceptionHandler in targetBody.ExceptionHandlers)
            {
                if (targetBody.Instructions.IndexOf(exceptionHandler.TryStart) < num)
                {
                    num2 = targetBody.ExceptionHandlers.IndexOf(exceptionHandler);
                }
            }
            foreach (KeyValuePair<int, Local> item3 in dictionary.Reverse())
            {
                targetBody.Instructions.Insert(num++, new Instruction(dnlib.DotNet.Emit.OpCodes.Stloc, item3.Value));
            }
            Dictionary<Instruction, Instruction> instrMap = new Dictionary<Instruction, Instruction>();
            List<Instruction> list = new List<Instruction>();
            foreach (Instruction instruction2 in methodDef.Body.Instructions)
            {
                Instruction instruction;
                if (instruction2.OpCode == dnlib.DotNet.Emit.OpCodes.Ret)
                {
                    instruction = new Instruction(dnlib.DotNet.Emit.OpCodes.Br, operand);
                }
                else if (instruction2.IsLdarg())
                {
                    dictionary.TryGetValue(instruction2.GetParameterIndex(), out var value);
                    instruction = new Instruction(dnlib.DotNet.Emit.OpCodes.Ldloc, value);
                }
                else if (instruction2.IsStarg())
                {
                    dictionary.TryGetValue(instruction2.GetParameterIndex(), out var value2);
                    instruction = new Instruction(dnlib.DotNet.Emit.OpCodes.Stloc, value2);
                }
                else if (instruction2.IsLdloc())
                {
                    dictionary2.TryGetValue(instruction2.GetLocal(methodDef.Body.Variables), out var value3);
                    instruction = new Instruction(dnlib.DotNet.Emit.OpCodes.Ldloc, value3);
                }
                else if (instruction2.IsStloc())
                {
                    dictionary2.TryGetValue(instruction2.GetLocal(methodDef.Body.Variables), out var value4);
                    instruction = new Instruction(dnlib.DotNet.Emit.OpCodes.Stloc, value4);
                }
                else
                {
                    instruction = new Instruction(instruction2.OpCode, instruction2.Operand);
                }
                list.Add(instruction);
                instrMap[instruction2] = instruction;
            }
            foreach (Instruction item4 in list)
            {
                if (item4.Operand != null && item4.Operand is Instruction key && instrMap.ContainsKey(key))
                {
                    item4.Operand = instrMap[key];
                }
                else if (item4.Operand is Instruction[] source)
                {
                    item4.Operand = source.Select((Instruction target) => instrMap[target]).ToArray();
                }
                targetBody.Instructions.Insert(num++, item4);
            }
            foreach (dnlib.DotNet.Emit.ExceptionHandler exceptionHandler2 in methodDef.Body.ExceptionHandlers)
            {
                targetBody.ExceptionHandlers.Insert(++num2, new dnlib.DotNet.Emit.ExceptionHandler(exceptionHandler2.HandlerType)
                {
                    CatchType = exceptionHandler2.CatchType,
                    TryStart = instrMap[exceptionHandler2.TryStart],
                    TryEnd = instrMap[exceptionHandler2.TryEnd],
                    HandlerStart = instrMap[exceptionHandler2.HandlerStart],
                    HandlerEnd = instrMap[exceptionHandler2.HandlerEnd],
                    FilterStart = ((exceptionHandler2.FilterStart == null) ? null : instrMap[exceptionHandler2.FilterStart])
                });
            }
        }

        public static System.Reflection.Emit.OpCode ToReflectionOp(this dnlib.DotNet.Emit.OpCode op)
        {
            switch (op.Code)
            {
                case Code.Ldarg_0:
                    return System.Reflection.Emit.OpCodes.Ldarg_0;
                case Code.Ldc_I4:
                    return System.Reflection.Emit.OpCodes.Ldc_I4;
                case Code.Ret:
                    return System.Reflection.Emit.OpCodes.Ret;
                case Code.Add:
                    return System.Reflection.Emit.OpCodes.Add;
                case Code.Sub:
                    return System.Reflection.Emit.OpCodes.Sub;
                case Code.Mul:
                    return System.Reflection.Emit.OpCodes.Mul;
                case Code.And:
                    return System.Reflection.Emit.OpCodes.And;
                case Code.Or:
                    return System.Reflection.Emit.OpCodes.Or;
                case Code.Xor:
                    return System.Reflection.Emit.OpCodes.Xor;
                default:
                    throw new NotImplementedException();
            }
        }

        public static IEnumerable<IDnlibDef> FindDefinitions(this ModuleDef module)
        {
            yield return module;
            foreach (TypeDef type in module.GetTypes())
            {
                yield return type;
                foreach (MethodDef method in type.Methods)
                {
                    yield return method;
                }
                foreach (FieldDef field in type.Fields)
                {
                    yield return field;
                }
                foreach (PropertyDef property in type.Properties)
                {
                    yield return property;
                }
                foreach (EventDef @event in type.Events)
                {
                    yield return @event;
                }
            }
        }

        public static IEnumerable<IDnlibDef> FindDefinitions(this TypeDef typeDef)
        {
            yield return typeDef;
            foreach (TypeDef nestedType in typeDef.NestedTypes)
            {
                yield return nestedType;
            }
            foreach (MethodDef method in typeDef.Methods)
            {
                yield return method;
            }
            foreach (FieldDef field in typeDef.Fields)
            {
                yield return field;
            }
            foreach (PropertyDef property in typeDef.Properties)
            {
                yield return property;
            }
            foreach (EventDef @event in typeDef.Events)
            {
                yield return @event;
            }
        }

        public static bool IsTypePublic(this TypeDef type)
        {
            do
            {
                if (type.IsPublic || type.IsNestedFamily || type.IsNestedFamilyAndAssembly || type.IsNestedFamilyOrAssembly || type.IsNestedPublic || type.IsPublic)
                {
                    type = type.DeclaringType;
                    continue;
                }
                return false;
            }
            while (type != null);
            return true;
        }
    }
}
