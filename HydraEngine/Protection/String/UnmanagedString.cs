﻿using System;
using System.Threading.Tasks;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Native;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File.Headers;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.Writer;
using System.Linq;

namespace HydraEngine.Protection.String
{
    public class UnmanagedString : Models.Protection
    {
        public UnmanagedString() : base("Protection.Renamer.UnmanagedString", "Renamer Phase", "Description for Renamer Phase") { ManualReload = true; }

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                var module = ModuleDefinition.FromFile(moduledef);
                var importer = new ReferenceImporter(module);

                var stringSbytePointerCtor = importer.ImportMethod(typeof(string).GetConstructor(new[] { typeof(sbyte*) }));
                var stringCharPointerCtor = importer.ImportMethod(typeof(string).GetConstructor(new[] { typeof(char*) }));
                var stringSbytePointerWithLengthCtor = importer.ImportMethod(typeof(string).GetConstructor(new[] { typeof(sbyte*), typeof(int), typeof(int) }));
                var stringCharPointerWithLengthCtor = importer.ImportMethod(typeof(string).GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) }));


                module.Attributes &= ~DotNetDirectoryFlags.ILOnly;
                var isx86 = module.MachineType == MachineType.I386;

                if (isx86)
                {
                    module.PEKind = OptionalHeaderMagic.Pe32;
                    module.MachineType = MachineType.I386;
                    module.Attributes |= DotNetDirectoryFlags.Bit32Required;
                }
                else
                {
                    module.PEKind = OptionalHeaderMagic.Pe32Plus;
                    module.MachineType = MachineType.Amd64;
                }

                var encodedStrings = new Dictionary<string, MethodDefinition>();

                foreach (var type in module.GetAllTypes().ToArray())
                {

                    //if (string.IsNullOrEmpty(type.Namespace) == false)
                    //{
                    //    if ( type.Namespace  == "Costura")
                    //        continue;
                    //    if (type.Namespace.ToString().Contains("My"))
                    //        continue;
                    //}

                    //if (type.Interfaces.Count > 0)
                    //    continue;


                    //if (type.IsSpecialName != false || type.IsRuntimeSpecialName != false ) continue;

                    //if (type.Name.ToString().StartsWith("<") || type.Name == "GeneratedInternalTypeHelper" || type.Name == "Resources" || type.Name == "MySettings" || type.Name == "Settings")
                    //    continue;
                    //if (type.DeclaringType is null || type.DeclaringType == type.Module.CorLibTypeFactory.Void.Type)
                    //    continue;
                    //if (type.IsInterface)
                    //    continue;
                    //if (type.IsForwarder)
                    //    continue;
                    //if (type.IsSerializable)
                    //    continue;
                    //if (type.IsAbstract)
                    //    continue;
                    //if (type.IsEnum)
                    //    continue;
                    //if (type.IsNestedFamilyOrAssembly)
                    //    continue;
                    //if (type.IsNestedFamilyAndAssembly)
                    //    continue;

                    foreach (var method in type.Methods.ToArray())
                    {

                        if (method == null)
                            continue;


                        //if (method.DeclaringType.BaseType != null)
                        //{
                        //    if (method.DeclaringType.BaseType.Name.Contains("Delegate"))
                        //        continue;
                        //}

                        //if (method.DeclaringType.IsInterface)
                        //    continue;
                        //if (method.DeclaringType.IsDelegate || method.DeclaringType.IsAbstract)
                        //    continue;
                        //if (method.DeclaringType.Name == "GeneratedInternalTypeHelper")
                        //    continue;

                        //if (method.Name == ".ctor" || method.Name == ".cctor")
                        //    continue;

                        //if (method.DeclaringType.FullName == "System.Windows.Forms.Binding" && method.Name.ToString() == ".ctor")
                        //    continue;
                        //if (method.Name == "Invoke")
                        //    continue;
                        //if (method.IsSetMethod || method.IsGetMethod)
                        //    continue;
                        //if (method.IsSpecialName)
                        //    continue;
                        //if (method.IsFamilyAndAssembly)
                        //    continue;
                        //if (method.IsFamily)
                        //    continue;
                        //if (method.IsRuntime)
                        //    continue;
                        //if (method.IsRuntimeSpecialName)
                        //    continue;
                        //if (method.IsConstructor)
                        //    continue;
                        //if (method.IsNative)
                        //    continue;
                        //if (method.IsPInvokeImpl || method.IsUnmanagedExport)
                        //    continue;
                        //if (method == null)
                        //    continue;
                        //if (method.Name.ToString().StartsWith("<"))
                        //    continue;
                        //if (method.DeclaringType is null || method.DeclaringType == method.Module.CorLibTypeFactory.Void.Type)
                        //    continue;
                        //if (method.DeclaringType.IsForwarder)
                        //    continue;
                        //if (method.IsVirtual)
                        //    continue;
                        //if (method.ImplementationMap != null)
                        //    continue;



                        if (method.HasMethodBody == false || method.CilMethodBody == null) continue;

                        if (method.CilMethodBody.Instructions.Count == 0) continue;

                        for (var index = 0; index < method.CilMethodBody.Instructions.Count; ++index)
                        {
                            var instruction = method.CilMethodBody.Instructions[index];

                            //if (instruction.OpCode == CilOpCodes.Ldstr &&
                            //    instruction.Operand is string { Length: > 0 } content) { }

                            if (instruction.OpCode == CilOpCodes.Ldstr)
                            {
                                string content = instruction.Operand as string;
                                if (content != null && content.Length > 0)
                                {
                                    var useUnicode = !CanBeEncodedIn7BitAscii(content);
                                    var addNullTerminator = !HasNullCharacter(content);

                                    if (!encodedStrings.TryGetValue(content, out var nativeMethod)) // reuse encoded strings
                                    {
                                        nativeMethod = CreateNewNativeMethodWithString(content, module, isx86, useUnicode, addNullTerminator);
                                        if (nativeMethod == null) continue;
                                        encodedStrings.Add(content, nativeMethod);
                                    }

                                    instruction.ReplaceWith(CilOpCodes.Call, nativeMethod);
                                    if (addNullTerminator)
                                    {
                                        method.CilMethodBody.Instructions.Insert(++index,
                                            new CilInstruction(CilOpCodes.Newobj,
                                                useUnicode ? stringCharPointerCtor : stringSbytePointerCtor));
                                    }
                                    else
                                    {
                                        method.CilMethodBody.Instructions.Insert(++index,
                                            CilInstruction.CreateLdcI4(0));
                                        method.CilMethodBody.Instructions.Insert(++index,
                                            CilInstruction.CreateLdcI4(content.Length));
                                        method.CilMethodBody.Instructions.Insert(++index,
                                            new CilInstruction(CilOpCodes.Newobj,
                                                useUnicode ? stringCharPointerWithLengthCtor : stringSbytePointerWithLengthCtor));
                                    }
                                }
                            }
                        }
                    }
                }

                MemoryStream OuputAssembly = new MemoryStream();
                module.Write(OuputAssembly);

                TempModule = OuputAssembly;

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        private static MethodDefinition CreateNewNativeMethodWithString(string content, ModuleDefinition originalModule,
        bool isX86, bool useUnicode, bool addNullTerminator)
        {
            if (originalModule == null) return null;  //       ArgumentNullException.ThrowIfNull(originalModule);
            if (content == null) return null;  //  ArgumentNullException.ThrowIfNull(content);

            var factory = originalModule.CorLibTypeFactory;

            var methodName = Guid.NewGuid().ToString();
            var method = new MethodDefinition(methodName, MethodAttributes.Public | MethodAttributes.Static,
                MethodSignature.CreateStatic(factory.SByte.MakePointerType()));

            method.ImplAttributes |= MethodImplAttributes.Native | MethodImplAttributes.Unmanaged |
                                     MethodImplAttributes.PreserveSig;
            method.Attributes |= MethodAttributes.PInvokeImpl;

            originalModule.GetOrCreateModuleType().Methods.Add(method);

            if (addNullTerminator)
            {
                content += "\0"; // not adding on byte level as it has encoding-dependent size
            }

            var stringBytes = useUnicode
                ? Encoding.Unicode.GetBytes(content)
                : Encoding.ASCII.GetBytes(content);

            //var prefix = isX86
            //    ? stackalloc byte[]
            //    {
            //    0x55, // push ebp
            //    0x89, 0xE5, // mov ebp, esp
            //    0xE8, 0x05, 0x00, 0x00, 0x00, // call <jump1>
            //    0x83, 0xC0, 0x01, // add eax, 1
            //    // <jump2>:
            //    0x5D, // pop ebp
            //    0xC3, // ret
            //    // <jump1>:
            //    0x58, // pop eax
            //    0x83, 0xC0, 0x0B, // add eax, 0xb
            //    0xEB, 0xF8 // jmp <jump2>
            //    }
            //    : stackalloc byte[]
            //    {
            //    0x48, 0x8D, 0x05, 0x01, 0x00, 0x00, 0x00, // lea rax, [rip + 0x1]
            //    0xC3 // ret
            //    };

            byte[] prefix;
            if (isX86)
            {
                prefix = new byte[]
                {
        0x55, // push ebp
        0x89, 0xE5, // mov ebp, esp
        0xE8, 0x05, 0x00, 0x00, 0x00, // call <jump1>
        0x83, 0xC0, 0x01, // add eax, 1
        // <jump2>:
        0x5D, // pop ebp
        0xC3, // ret
        // <jump1>:
        0x58, // pop eax
        0x83, 0xC0, 0x0B, // add eax, 0xb
        0xEB, 0xF8 // jmp <jump2>
                };
            }
            else
            {
                prefix = new byte[]
                {
        0x48, 0x8D, 0x05, 0x01, 0x00, 0x00, 0x00, // lea rax, [rip + 0x1]
        0xC3 // ret
                };
            }


            byte[] code = new byte[prefix.Length + stringBytes.Length];
            //prefix.CopyTo(code);
            //stringBytes.CopyTo(code[prefix.Length..]);
            Array.Copy(prefix, 0, code, 0, prefix.Length);
            Array.Copy(stringBytes, 0, code, prefix.Length, stringBytes.Length);

            var body = new NativeMethodBody(method)
            {
                Code = code
            };

            Console.WriteLine($"Created new native method with name: {methodName} for string: {content.TrimEnd()}");
            method.NativeMethodBody = body;
            return method;
        }

        private static bool CanBeEncodedIn7BitAscii(string text)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] > '\x7f')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasNullCharacter(string text)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\0')
                {
                    return true;
                }
            }

            return false;
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            throw new NotImplementedException();
        }
    }
}
