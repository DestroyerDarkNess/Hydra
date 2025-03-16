using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit; // For DynamicMethod, ILGenerator
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using OperandType = dnlib.DotNet.Emit.OperandType;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace HydraEngine.Protection.Method
{
    /// <summary>
    /// Demonstration of storing method IL in a resource, removing it from the original,
    /// and dynamically reconstructing it at runtime. 
    /// </summary>
    public class MethodHider : Models.Protection
    {
        public MethodHider()
            : base("Protection.Method.MethodHider",
                   "Method Hider Phase",
                   "Stores the real IL externally and recreates it at runtime")
        {
        }

        public override async System.Threading.Tasks.Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                var myAssembly = ModuleDefMD.Load(typeof(ILSerializer).Module);

                // 2) Resolve the TypeDef for ILSerializer
                var ilSerializerTypeDef = myAssembly.ResolveTypeDef(
                    MDToken.ToRID(typeof(ILSerializer).MetadataToken));

                // 3) Inject ILSerializer into target module
                InjectHelper.Inject(ilSerializerTypeDef, module.GlobalType, module);

                // 4) Resolve the TypeDef for HelperRuntimeCode
                var helperRuntimeCodeTypeDef = myAssembly.ResolveTypeDef(
                    MDToken.ToRID(typeof(HelperRuntimeCode).MetadataToken));

                // 5) Inject HelperRuntimeCode into target module
                InjectHelper.Inject(helperRuntimeCodeTypeDef, module.GlobalType, module);

                HideMethodIL(module.EntryPoint, module);

                return true;
            }
            catch (Exception ex)
            {
                this.Errors = ex;
                return false;
            }
        }

        public override System.Threading.Tasks.Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts the IL of <paramref name="method"/>, stores it in an embedded resource,
        /// and replaces the original method body with a stub that dynamically reconstructs
        /// and executes that IL at runtime.
        /// </summary>
        private void HideMethodIL(MethodDef method, ModuleDefMD module)
        {
            if (method == null || !method.HasBody)
                return; // nothing to hide

            // 1) Convert the CIL instructions to a custom byte[] format
            var instructions = method.Body.Instructions;
            byte[] ilBytes = ILSerializer.SerializeInstructions(instructions, module);

            // 2) Store that in the assembly as an embedded resource
            string resourceName = "HiddenIL_" + method.Name + "_" + Guid.NewGuid().ToString("N");
            var resourceData = new EmbeddedResource(resourceName, ilBytes, ManifestResourceAttributes.Private);
            module.Resources.Add(resourceData);

            // 3) Replace the original method body with a stub:
            //    -> calls a special "HiddenMethodLauncher(resourceName, object[] args)"
            //    -> returns object or typed result
            //    We'll define "HiddenMethodLauncher" in a new type, or we can store it in <Module>.
            //    For clarity, let's do a "HiddenRuntime" static type inside the same assembly.

            method.Body = new CilBody();
            var il = method.Body.Instructions;
            var corlib = module.CorLibTypes; // for references to System types

            // We'll need "HiddenMethodLauncher(string, object[])" => object
            var launcherSig = MethodSig.CreateStatic(
                corlib.Object,                            // returns object
                corlib.String,                            // param: resourceName
                corlib.Object.ToSZArraySig()              // param: object[]
            );

            // We can create a MemberRef or MethodRefUser that points to the method we'll define.
            // For demonstration, let's pretend we attach "HiddenRuntime" to GlobalType,
            // or your custom type. We'll do it as a top-level type:
            var hiddenRuntimeType = EnsureHiddenRuntimeType(module);
            var launcherMethodDef = hiddenRuntimeType.FindMethod("HiddenMethodLauncher");

            if (launcherMethodDef == null)
                throw new InvalidOperationException("HiddenMethodLauncher method not found in HiddenRuntime type.");

            var launcherMemberRef = new MemberRefUser(
                module,
                launcherMethodDef.Name,
                launcherSig,
                hiddenRuntimeType
            );

            // Load the resource name
            il.Add(Instruction.Create(OpCodes.Ldstr, resourceName));

            // Next, create an object[] with size = method.Parameters.Count
            int paramCount = method.Parameters.Count;
            il.Add(Instruction.Create(OpCodes.Ldc_I4, paramCount));
            il.Add(Instruction.Create(OpCodes.Newarr, corlib.Object.ToTypeDefOrRef()));

            // For each parameter, we load it and box if necessary
            for (int i = 0; i < paramCount; i++)
            {
                il.Add(Instruction.Create(OpCodes.Dup)); // array reference
                il.Add(Instruction.Create(OpCodes.Ldc_I4, i)); // index

                // Load arg
                if (i == 0) il.Add(OpCodes.Ldarg_0.ToInstruction());
                else if (i == 1) il.Add(OpCodes.Ldarg_1.ToInstruction());
                else if (i == 2) il.Add(OpCodes.Ldarg_2.ToInstruction());
                else if (i == 3) il.Add(OpCodes.Ldarg_3.ToInstruction());
                else il.Add(OpCodes.Ldarg.ToInstruction((ushort)i));

                // box if value type
                var pType = method.Parameters[i].Type;
                if (pType.IsValueType)
                {
                    il.Add(Instruction.Create(OpCodes.Box, pType.ToTypeDefOrRef()));
                }

                il.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            // Call the launcher
            il.Add(Instruction.Create(OpCodes.Call, launcherMemberRef));

            // If method has a return type != void, we unbox
            var retType = method.MethodSig.RetType;
            if (!retType.IsVoid())
            {
                // unbox.any retType
                il.Add(Instruction.Create(OpCodes.Unbox_Any, retType.ToTypeDefOrRef()));
            }

            // return
            il.Add(Instruction.Create(OpCodes.Ret));

            // Done! The original method now just stubs out to HiddenMethodLauncher at runtime.
        }

        /// <summary>
        /// Ensures there's a "HiddenRuntime" type in the module with a 
        /// "public static object HiddenMethodLauncher(string, object[])" method.
        /// If it doesn't exist, we create it. If it does, we reuse it.
        /// </summary>
        private TypeDef EnsureHiddenRuntimeType(ModuleDefMD module)
        {
            const string RUNTIME_TYPE_NAME = "HiddenRuntime";
            var existing = module.Types.FirstOrDefault(t => t.Name == RUNTIME_TYPE_NAME);
            if (existing != null)
                return existing;

            // Create a new top-level type
            var hiddenRuntime = new TypeDefUser(
                "", // no namespace
                RUNTIME_TYPE_NAME,
                module.CorLibTypes.Object.ToTypeDefOrRef()
            );
            hiddenRuntime.Attributes = TypeAttributes.NotPublic
                                     | TypeAttributes.AutoLayout
                                     | TypeAttributes.AnsiClass
                                     | TypeAttributes.Class
                                     | TypeAttributes.Abstract
                                     | TypeAttributes.Sealed
                                     | TypeAttributes.BeforeFieldInit;

            module.Types.Add(hiddenRuntime);

            // Add the HiddenMethodLauncher method
            //   public static object HiddenMethodLauncher(string resName, object[] args)
            var methodSig = MethodSig.CreateStatic(
                module.CorLibTypes.Object, // return
                module.CorLibTypes.String, // param1
                module.CorLibTypes.Object.ToSZArraySig() // param2: object[]
            );

            var launcherMethod = new MethodDefUser(
                "HiddenMethodLauncher",
                methodSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static
            );

            hiddenRuntime.Methods.Add(launcherMethod);

            // Implement the body in C#. We'll generate IL for:
            //
            // object HiddenMethodLauncher(string resourceName, object[] args)
            // {
            //    // 1) read resource
            //    // 2) parse the IL bytes
            //    // 3) build a DynamicMethod
            //    // 4) emit instructions
            //    // 5) call the dynamic method
            //    //    handle the return
            // }
            //
            // For simplicity, let's write the method in C# and then convert it to IL 
            // using dnlib's MethodBody generation. Or we can just do a short IL inlined. 
            // We'll do a short IL approach below.

            var body = new CilBody { MaxStack = 8 };
            launcherMethod.Body = body;

            /*
             * PSEUDOCODE in C#:
             *
             * var asm = Assembly.GetExecutingAssembly();
             * using (var stream = asm.GetManifestResourceStream(resourceName))
             * {
             *     if (stream == null) throw new Exception("Resource not found!");
             *     byte[] data = new byte[stream.Length];
             *     stream.Read(data, 0, data.Length);
             *
             *     // Reconstruct IL
             *     var method = BuildDynamicMethodFromIL(data, args.Length);
             *
             *     // invoke dynamic method
             *     object result = method.Invoke(null, args);
             *     return result;
             * }
             */

            // For a minimal IL approach, let's do it “manually”:
            // We'll push "Assembly.GetExecutingAssembly()", etc. 
            // But that's quite tedious with raw IL. 
            // Alternatively, we can just write a short method in C# and let *you* compile it 
            // or inject it. For brevity, let's do partial IL generation. 
            // We'll do a simpler approach:
            //    => call a static helper method we define in this same class in plain C# 
            //       e.g. "return HiddenMethodLauncher_Impl(resourceName, args);"
            // Then we only generate IL for that call. 
            // We'll define that helper below in normal C#.

            // We'll do: 
            //    ldarg.0
            //    ldarg.1
            //    call HiddenRuntime::HiddenMethodLauncher_Impl
            //    ret
            var helperRef = new MemberRefUser(
                module,
                "HiddenMethodLauncher_Impl",
                MethodSig.CreateStatic(
                    module.CorLibTypes.Object,
                    module.CorLibTypes.String,
                    module.CorLibTypes.Object.ToSZArraySig()
                ),
                hiddenRuntime
            );

            var instr = body.Instructions;
            instr.Add(OpCodes.Ldarg_0.ToInstruction()); // resourceName
            instr.Add(OpCodes.Ldarg_1.ToInstruction()); // args
            instr.Add(OpCodes.Call.ToInstruction(helperRef));
            instr.Add(OpCodes.Ret.ToInstruction());

            // Now we define that "HiddenMethodLauncher_Impl" in normal C# below:
            AddHiddenMethodLauncherImpl(hiddenRuntime, module);

            return hiddenRuntime;
        }

        /// <summary>
        /// Adds a normal C#-style method to the <paramref name="hiddenRuntime"/> type that
        /// does the actual reflection, resource loading, IL reconstruction, etc.
        /// We'll compile it at runtime from this code for demonstration.
        /// </summary>
        private void AddHiddenMethodLauncherImpl(TypeDef hiddenRuntime, ModuleDefMD module)
        {
            // We'll write it as normal C#. This requires we do a bit of IL weaving or
            // an alternative is to store the code as a string and compile dynamically.
            // For demonstration, let's just embed a "MethodDefUser" with a simple body
            // that calls a static method in an external helper class we define in this file. 
            //
            // We'll define "HiddenRuntimeHelper.HiddenMethodLauncher_Impl(string, object[])" 
            // as a real method in the same assembly. Then we reference it from here.
            //
            // Another approach is to write the entire method in raw IL. 
            // We'll do the simpler route: we create a stub in IL that calls our external helper.

            const string HELPER_TYPE_NAME = "HiddenRuntimeHelper";
            var helperType = module.Types.FirstOrDefault(t => t.Name == HELPER_TYPE_NAME);
            if (helperType == null)
            {
                // create the helper type
                helperType = new TypeDefUser(
                    "",
                    HELPER_TYPE_NAME,
                    module.CorLibTypes.Object.ToTypeDefOrRef()
                );
                helperType.Attributes = TypeAttributes.NotPublic
                                      | TypeAttributes.AutoLayout
                                      | TypeAttributes.AnsiClass
                                      | TypeAttributes.Class
                                      | TypeAttributes.Abstract
                                      | TypeAttributes.Sealed
                                      | TypeAttributes.BeforeFieldInit;
                module.Types.Add(helperType);

                // Add the real C# method to it (below).
                var helperMethod = GenerateHelperMethodDef(module);
                helperType.Methods.Add(helperMethod);
            }

            var implSig = MethodSig.CreateStatic(
                module.CorLibTypes.Object,
                module.CorLibTypes.String,
                module.CorLibTypes.Object.ToSZArraySig()
            );

            var implMethod = new MethodDefUser(
                "HiddenMethodLauncher_Impl",
                implSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static
            );
            hiddenRuntime.Methods.Add(implMethod);

            // This method just forwards to "HiddenRuntimeHelper.HiddenMethodLauncher_Impl"
            var body = new CilBody { MaxStack = 8 };
            implMethod.Body = body;

            var helperImpl = helperType.FindMethod("HiddenMethodLauncher_Impl");
            var helperRef = new MemberRefUser(module, helperImpl.Name, implSig, helperType);

            // Generate IL:
            //   ldarg.0
            //   ldarg.1
            //   call HiddenRuntimeHelper.HiddenMethodLauncher_Impl(string, object[])
            //   ret
            body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            body.Instructions.Add(OpCodes.Call.ToInstruction(helperRef));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());
        }

        /// <summary>
        /// Creates a real "HiddenMethodLauncher_Impl(string, object[])" method in normal C#,
        /// compiled into a MethodDefUser, for demonstration.
        /// This method:
        ///   - loads resource data
        ///   - calls ILSerializer.DeserializeInstructions to parse a naive IL format
        ///   - builds a DynamicMethod and emits those instructions
        ///   - calls the DynamicMethod with the provided 'args'
        ///   - returns the result
        /// </summary>
        private MethodDef GenerateHelperMethodDef(ModuleDefMD module)
        {
            var methodSig = MethodSig.CreateStatic(
                module.CorLibTypes.Object,   // return object
                module.CorLibTypes.String,   // resourceName
                module.CorLibTypes.Object.ToSZArraySig() // object[] args
            );

            var mdef = new MethodDefUser(
                "HiddenMethodLauncher_Impl",
                methodSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static
            );

            // Let's write C# code directly, then compile. 
            // For the sake of brevity, we'll embed the entire method as IL using dnlib's 
            // "MethodBody" approach. But that is tedious to do by hand. 
            // Instead, let's do a quick approach: we store the entire method logic in a string
            // and use Reflection to compile, or we can do an IL injection.

            // We'll do a short hack: We'll define the *runtime* code in a separate static method 
            // in an external normal class, "HelperRuntimeCode" (below). Then we just call it.
            // That means the code for dynamic method creation is in plain C#. 
            // This approach avoids writing raw IL by hand.

            var body = new CilBody { MaxStack = 8 };
            mdef.Body = body;

            // We'll define "HelperRuntimeCode.RunHiddenMethod(resourceName, args)"
            // in a separate public class. Then we call it from here.

            // reference that method
            //var helperRuntimeMethod = typeof(HelperRuntimeCode).GetMethod("RunHiddenMethod",
            //    BindingFlags.Static | BindingFlags.Public);

            //if (helperRuntimeMethod == null)
            //    throw new InvalidOperationException("HelperRuntimeCode.RunHiddenMethod not found!");

            //// Import it into dnlib
            //var helperImport = module.Import(helperRuntimeMethod);

            var helperTypeDef = module.Types.First(t => t.Name == "HelperRuntimeCode");
            // or  t.FullName == "HelperRuntimeCode"

            var runHiddenMethod = helperTypeDef.Methods
                .First(m => m.Name == "RunHiddenMethod");

            // Then create a reference to that method
            var runHiddenMethodRef = new MemberRefUser(
                module,
                runHiddenMethod.Name,
                module.Import(runHiddenMethod).MethodSig,
                helperTypeDef
            );

            // ... use runHiddenMethodRef in your IL generation ...


            // ... use runHiddenMethodRef in your IL generation ...


            // IL:
            //   ldarg.0
            //   ldarg.1
            //   call object HelperRuntimeCode.RunHiddenMethod(string, object[])
            //   ret
            body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            body.Instructions.Add(OpCodes.Call.ToInstruction(runHiddenMethodRef));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());

            return mdef;
        }
    }

    /// <summary>
    /// The naive IL serializer: 
    /// Convert each instruction into a small binary format 
    /// [opcode | operandType | operandData].
    /// This does NOT handle all instructions, only a subset for demonstration.
    /// </summary>
    public static class ILSerializer
    {
        public static byte[] SerializeInstructions(IList<Instruction> instructions, ModuleDefMD module)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(instructions.Count);
                foreach (var instr in instructions)
                {
                    // Write opcode
                    bw.Write((ushort)instr.OpCode.Value);

                    // We'll handle only a small set of operand scenarios:
                    //   - No operand
                    //   - int32 operand
                    //   - int64 operand
                    //   - float32 or float64
                    //   - string operand (ldstr)
                    //   - method operand (call)
                    //   - null / metadata tokens are not fully handled
                    object operand = instr.Operand;
                    if (operand == null)
                    {
                        bw.Write((byte)OperandType.InlineNone);
                        continue;
                    }

                    if (operand is int i32)
                    {
                        bw.Write((byte)OperandType.InlineI);
                        bw.Write(i32);
                    }
                    else if (operand is long i64)
                    {
                        bw.Write((byte)OperandType.InlineI8);
                        bw.Write(i64);
                    }
                    else if (operand is float f32)
                    {
                        bw.Write((byte)OperandType.ShortInlineR);
                        bw.Write(f32);
                    }
                    else if (operand is double f64)
                    {
                        bw.Write((byte)OperandType.InlineR);
                        bw.Write(f64);
                    }
                    else if (operand is string s)
                    {
                        bw.Write((byte)OperandType.InlineString);
                        bw.Write(s);
                    }
                    else if (operand is IMethod methodRef)
                    {
                        bw.Write((byte)OperandType.InlineMethod);
                        // We'll store the metadata token
                        bw.Write((int)module.Import(methodRef).MDToken.Raw);
                    }
                    else
                    {
                        // Unhandled -> simple approach: write a special code
                        bw.Write((byte)255);
                    }
                }
                return ms.ToArray();
            }
        }

        public static readonly System.Reflection.Emit.OpCode[] OneByteOpCodes = new System.Reflection.Emit.OpCode[0x100];

        /// <summary>
        /// All two-byte opcodes (first byte is <c>0xFE</c>)
        /// </summary>
        public static readonly System.Reflection.Emit.OpCode[] TwoByteOpCodes = new System.Reflection.Emit.OpCode[0x100];

        /// <summary>
        /// Deserializes instructions from the above naive format.
        /// </summary>
        public static List<(System.Reflection.Emit.OpCode op, object operand)> DeserializeInstructions(byte[] data, Module module)
        {
            var result = new List<(System.Reflection.Emit.OpCode op, object operand)>();

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    ushort opVal = br.ReadUInt16();
                    var opCode = (opVal & 0xFF00) == 0xFE00
                        ? TwoByteOpCodes[opVal & 0xFF]
                        : OneByteOpCodes[opVal];

                    byte operandType = br.ReadByte();
                    if (operandType == (byte)System.Reflection.Emit.OperandType.InlineNone)
                    {
                        result.Add((opCode, null));
                        continue;
                    }

                    object operand = null;
                    switch ((OperandType)operandType)
                    {
                        case OperandType.InlineI:
                            operand = br.ReadInt32();
                            break;
                        case OperandType.InlineI8:
                            operand = br.ReadInt64();
                            break;
                        case OperandType.ShortInlineR:
                            operand = br.ReadSingle();
                            break;
                        case OperandType.InlineR:
                            operand = br.ReadDouble();
                            break;
                        case OperandType.InlineString:
                            operand = br.ReadString();
                            break;
                        case OperandType.InlineMethod:
                            {
                                int token = br.ReadInt32();
                                // Resolve the method from the token in the *runtime* module 
                                // or the same assembly (very naive approach). 
                                // We'll do a simple reflection load here.
                                // For a real obfuscator, you'd do a more robust resolution.
                                operand = ResolveMethodFromToken(token, module);
                            }
                            break;
                        default:
                            operand = null;
                            break;
                    }

                    result.Add((opCode, operand));
                }
            }
            return result;
        }

        /// <summary>
        /// Very naive method resolution by metadata token. 
        /// In a real scenario, you'd handle cross-assembly references, etc.
        /// Here, we assume it's in the same assembly.
        /// </summary>
        private static MethodInfo ResolveMethodFromToken(int token, Module module)
        {
            try
            {
                return module.ResolveMethod(token) as MethodInfo;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Contains the actual logic to reconstruct the IL into a DynamicMethod and invoke it.
    /// This is what runs at runtime in the final build, when the user calls the hidden method.
    /// </summary>
    public static class HelperRuntimeCode
    {
        public static object RunHiddenMethod(string resourceName, object[] args)
        {
            // 1) read resource from executing assembly
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception("Resource not found: " + resourceName);

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                // 2) parse the IL from our naive format
                var instructions = ILSerializer.DeserializeInstructions(data, asm.ManifestModule);

                // 3) Create a DynamicMethod with a matching signature: object(object[])
                //    Actually, we don't know the real param types from our naive approach, so let's 
                //    just do (object[]), returning object. If you do a more advanced approach,
                //    you'd store the real method signature.

                var dyn = new DynamicMethod(
                    "HiddenIL_" + Guid.NewGuid().ToString("N"),
                    typeof(object),
                    new Type[] { typeof(object[]) },
                    typeof(HelperRuntimeCode).Module, // restricted skip visibility if needed
                    true
                );

                var ilGen = dyn.GetILGenerator();

                // We'll assume the original method had N parameters, and they're all in object[].
                // If the original method used typed parameters, you'd unbox them here. 
                // For demonstration, let's simply re-emit the instructions we read. 
                // We'll do the minimal approach: each instruction that references ldarg, etc.
                // is no longer valid unless we do more advanced rewriting. We'll skip that for now,
                // or assume the original method was static with 0 or minimal parameters.

                // Emit all instructions
                foreach (var (op, operand) in instructions)
                {
                    if (operand == null)
                        ilGen.Emit(op);
                    else
                    {
                        if (operand is int i32) ilGen.Emit(op, i32);
                        else if (operand is long i64) ilGen.Emit(op, i64);
                        else if (operand is float f32) ilGen.Emit(op, f32);
                        else if (operand is double f64) ilGen.Emit(op, f64);
                        else if (operand is string s) ilGen.Emit(op, s);
                        else if (operand is MethodInfo mi) ilGen.Emit(op, mi);
                        else
                        {
                            // not handled in this demo
                            ilGen.Emit(op);
                        }
                    }
                }

                // In most real IL, the final instruction is "ret". We'll assume that was part
                // of the serialized instructions. If not, we'd add it. 
                // Next: create a delegate
                var del = (Func<object[], object>)dyn.CreateDelegate(typeof(Func<object[], object>));

                // 4) invoke dynamic method with 'args'
                object result = del(args);
                return result;
            }
        }
    }
    // Add this extension method to check if a TypeSig is void
    public static class TypeSigExtensions
    {
        public static bool IsVoid(this TypeSig typeSig)
        {
            return typeSig != null && typeSig.ElementType == ElementType.Void;
        }
    }
}
