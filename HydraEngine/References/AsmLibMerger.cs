using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;

namespace HydraEngine.References
{
    public class AsmLibMerger
    {
        public Exception Errors { get; set; } = new Exception("Undefined");
        public bool Ouput { get; set; } = true;

        public bool MergeAssemblies(string loaderPath, List<string> dllPaths, string output)
        {
            try
            {
                Console.WriteLine($"[AsmLibMerger] Loader: {loaderPath}");
                var loaderModule = ModuleDefinition.FromFile(loaderPath);

                var importer = new ReferenceImporter(loaderModule);
                var moduleType = loaderModule.GetOrCreateModuleType();

                // Crear método decompresión LZF y resolvedor AssemblyResolve que lo usa
                var lzfMethod = CreateLzfDecompressMethod(loaderModule, importer);
                moduleType.Methods.Add(lzfMethod);
                var resolverMethod = CreateResolveAllEmbeddedMethod(loaderModule, importer, lzfMethod);
                moduleType.Methods.Add(resolverMethod);

                var cctor = loaderModule.GetOrCreateModuleConstructor();

                if (cctor.CilMethodBody == null)
                {
                    cctor.CilMethodBody = new CilMethodBody(cctor);
                }

                var body = cctor.CilMethodBody;
                body.Instructions.ExpandMacros();
                // Registrar: AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(<resolverMethod>);
                var getCurrentDomain = GetCorLibMethod(loaderModule, "System", nameof(AppDomain), $"get_{nameof(AppDomain.CurrentDomain)}", Array.Empty<string>());
                var handlerCtor = GetCorLibMethod(loaderModule, "System", nameof(ResolveEventHandler), ".ctor", "System.Object", "System.IntPtr");
                var addAssemblyResolve = GetCorLibMethod(loaderModule, "System", nameof(AppDomain), $"add_{nameof(AppDomain.AssemblyResolve)}", "System.ResolveEventHandler");
                body.Instructions.InsertRange(0, new[] {
                new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getCurrentDomain)),
                new CilInstruction(CilOpCodes.Ldnull),
                new CilInstruction(CilOpCodes.Ldftn, resolverMethod),
                new CilInstruction(CilOpCodes.Newobj, importer.ImportMethod(handlerCtor)),
                new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(addAssemblyResolve))
            });
                body.Instructions.OptimizeMacros();
                body.InitializeLocals = true;

                var outDir = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                Console.WriteLine($"[AsmLibMerger] output: {output}");
                loaderModule.Write(output, new ManagedPEImageBuilder(MetadataBuilderFlags.PreserveAll));

                long before = new FileInfo(output).Length;
                Console.WriteLine($"[AsmLibMerger] size before append: {before} bytes");

                // Anexar cada payload: [DLL][MAGIC 4 bytes 'S1NG'][LEN 4 bytes little-endian]
                using (var fs = new FileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    fs.Seek(0, SeekOrigin.End);
                    foreach (var dllPath in dllPaths)
                    {
                        if (!File.Exists(dllPath)) continue;
                        var dllBytes = File.ReadAllBytes(dllPath);
                        var rle = RleCompress(dllBytes);
                        //var lzf = LzfTryCompress(dllBytes); // puede devolver null si no mejora
                        byte algId = 0; // 0=raw, 2=RLE, 3=LZF
                        byte[] compData = dllBytes;
                        //if (lzf != null) // && lzf.Length + 9 < compData.Length
                        //{
                        //    algId = 3;
                        //    compData = lzf;
                        //}  else
                        if (rle != null && rle.Length + 9 < compData.Length)
                        {
                            algId = 2;
                            compData = rle;
                        }
                        int uncompressedLen = dllBytes.Length;
                        int compressedLen = compData.Length;
                        // payload = [AlgId(1)][UncompLen(4)][CompLen(4)][Data]
                        int payloadLen = 1 + 4 + 4 + compressedLen;
                        var payload = new byte[payloadLen];
                        payload[0] = algId;
                        Buffer.BlockCopy(BitConverter.GetBytes(uncompressedLen), 0, payload, 1, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(compressedLen), 0, payload, 5, 4);
                        Buffer.BlockCopy(compData, 0, payload, 9, compressedLen);
                        fs.Write(payload, 0, payload.Length);
                        var magic = new byte[] { (byte)'S', (byte)'1', (byte)'N', (byte)'G' };
                        fs.Write(magic, 0, magic.Length);
                        var lenBytes = BitConverter.GetBytes(payloadLen);
                        fs.Write(lenBytes, 0, lenBytes.Length);
                        Console.WriteLine($"[AsmLibMerger] appended {Path.GetFileName(dllPath)} (raw {dllBytes.Length} -> {compressedLen} bytes, alg {(algId == 3 ? "LZF" : algId == 2 ? "RLE" : "RAW")})");
                    }
                    fs.Flush(true);
                }
                long after = new FileInfo(output).Length;
                Console.WriteLine($"[AsmLibMerger] size after append: {after} bytes (delta {after - before})");
                return true;
            }
            catch (Exception ex)
            {
                Errors = ex;
                return false;
            }
        }

        private static MethodDefinition CreateResolveAllEmbeddedMethod(
            ModuleDefinition module,
            ReferenceImporter importer,
            MethodDefinition lzfDecompressMethod)
        {
            // Tipos y firmas
            var assemblyType = new TypeReference(module.CorLibTypeFactory.CorLibScope, "System.Reflection", nameof(Assembly));
            var resolveArgsType = new TypeReference(module.CorLibTypeFactory.CorLibScope, "System", nameof(ResolveEventArgs));
            var assemblyNameType = new TypeReference(module.CorLibTypeFactory.CorLibScope, "System.Reflection", nameof(AssemblyName));

            var returnType = importer.ImportTypeSignature(assemblyType.ToTypeSignature());
            var sig = MethodSignature.CreateStatic(returnType,
                module.CorLibTypeFactory.Object,
                importer.ImportTypeSignature(resolveArgsType.ToTypeSignature()));

            var method = new MethodDefinition("ResolveAllEmbedded", MethodAttributes.Private | MethodAttributes.Static, sig);

            method.CilMethodBody = new CilMethodBody(method);
            var body = method.CilMethodBody;
            var il = body.Instructions;

            // Locales
            var stringSig = module.CorLibTypeFactory.String;
            var byteArraySig = module.CorLibTypeFactory.Byte.MakeSzArrayType();
            var intSig = module.CorLibTypeFactory.Int32;
            var assemblySig = importer.ImportTypeSignature(assemblyType.ToTypeSignature());

            var pathLocal = new CilLocalVariable(importer.ImportTypeSignature(stringSig));
            var exeBytesLocal = new CilLocalVariable(importer.ImportTypeSignature(byteArraySig));
            var offsetLocal = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var lenLocal = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var payloadLocal = new CilLocalVariable(importer.ImportTypeSignature(byteArraySig));
            var loadedAsmLocal = new CilLocalVariable(assemblySig);
            var reqNameLocal = new CilLocalVariable(importer.ImportTypeSignature(stringSig));
            var loadedNameLocal = new CilLocalVariable(importer.ImportTypeSignature(stringSig));
            body.LocalVariables.Add(pathLocal);
            body.LocalVariables.Add(exeBytesLocal);
            body.LocalVariables.Add(offsetLocal);
            body.LocalVariables.Add(lenLocal);
            body.LocalVariables.Add(payloadLocal);
            body.LocalVariables.Add(loadedAsmLocal);
            body.LocalVariables.Add(reqNameLocal);
            body.LocalVariables.Add(loadedNameLocal);

            // Métodos usados
            var args_getName = GetCorLibMethod(module, "System", nameof(ResolveEventArgs), $"get_{nameof(ResolveEventArgs.Name)}", Array.Empty<string>());
            var asmNameCtor = GetCorLibMethod(module, "System.Reflection", nameof(AssemblyName), ".ctor", "System.String");
            var asmName_getName = GetCorLibMethod(module, "System.Reflection", nameof(AssemblyName), $"get_{nameof(AssemblyName.Name)}", Array.Empty<string>());
            var stringEquals = GetCorLibMethod(module, "System", nameof(String), nameof(string.Equals), "System.String", "System.String");
            var getExecuting = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), nameof(Assembly.GetExecutingAssembly), Array.Empty<string>());
            var getLocation = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), $"get_{nameof(Assembly.Location)}", Array.Empty<string>());
            var fileReadAllBytes = GetCorLibMethod(module, "System.IO", nameof(File), nameof(File.ReadAllBytes), "System.String");
            var bitConverterToInt32 = GetCorLibMethod(module, "System", nameof(BitConverter), nameof(BitConverter.ToInt32), "System.Byte[]", "System.Int32");
            var arrayCopy = GetCorLibMethod(module, "System", nameof(Array), nameof(Array.Copy), "System.Array", "System.Int32", "System.Array", "System.Int32", "System.Int32");
            var asmLoad = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), nameof(Assembly.Load), "System.Byte[]");
            var asm_getName = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), nameof(Assembly.GetName), Array.Empty<string>());

            // reqName = new AssemblyName(args.Name).Name
            il.Add(new CilInstruction(CilOpCodes.Ldarg_1));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(args_getName)));
            il.Add(new CilInstruction(CilOpCodes.Newobj, importer.ImportMethod(asmNameCtor)));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(asmName_getName)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, reqNameLocal));

            // path = Assembly.GetExecutingAssembly().Location
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getExecuting)));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getLocation)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, pathLocal));

            // exeBytes = File.ReadAllBytes(path)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pathLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(fileReadAllBytes)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, exeBytesLocal));

            // offset = exeBytes.Length
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldlen));
            il.Add(new CilInstruction(CilOpCodes.Conv_I4));
            il.Add(new CilInstruction(CilOpCodes.Stloc, offsetLocal));

            var loopCheck = new CilInstruction(CilOpCodes.Nop);
            var loopCheckLabel = new CilInstructionLabel(loopCheck);
            var loopStart = new CilInstruction(CilOpCodes.Nop);
            var loopStartLabel = new CilInstructionLabel(loopStart);
            var retNull = new CilInstruction(CilOpCodes.Nop);
            var retNullLabel = new CilInstructionLabel(retNull);

            // goto loopCheck
            il.Add(new CilInstruction(CilOpCodes.Br, loopCheckLabel));

            // loopStart:
            il.Add(loopStart);

            // len = BitConverter.ToInt32(exeBytes, offset - 4)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 4));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(bitConverterToInt32)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, lenLocal));

            // payload = exeBytes[(offset-8-len) .. (offset-8)]
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Newarr, module.CorLibTypeFactory.Byte.Type));
            il.Add(new CilInstruction(CilOpCodes.Stloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Sub)); // srcIndex
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal)); // dest
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0)); // destIndex
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal)); // length
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(arrayCopy)));

            // Decode header: AlgId, UncompLen, CompLen
            var algLocal = new CilLocalVariable(module.CorLibTypeFactory.Byte);
            var uncompLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            var compLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            body.LocalVariables.Add(algLocal);
            body.LocalVariables.Add(uncompLocal);
            body.LocalVariables.Add(compLocal);

            // alg = payload[0]
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Stloc, algLocal));
            // uncomp = BitConverter.ToInt32(payload, 1)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(bitConverterToInt32)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, uncompLocal));
            // comp = BitConverter.ToInt32(payload, 5)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 5));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(bitConverterToInt32)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, compLocal));

            // dataPtr = 9, dataLen = comp
            var dataPtrLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            body.LocalVariables.Add(dataPtrLocal);
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 9));
            il.Add(new CilInstruction(CilOpCodes.Stloc, dataPtrLocal));

            // outBytes = (alg==0) ? payload[9..9+comp] : RLE decode
            var outBytesLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Byte.MakeSzArrayType()));
            body.LocalVariables.Add(outBytesLocal);

            var useRaw = new CilInstruction(CilOpCodes.Nop);
            var useRawRef = new CilInstructionLabel(useRaw);
            var afterDecode = new CilInstruction(CilOpCodes.Nop);
            var afterDecodeRef = new CilInstructionLabel(afterDecode);

            // if (alg == 3) -> LZF
            var lzfPath = new CilInstruction(CilOpCodes.Nop);
            var lzfPathRef = new CilInstructionLabel(lzfPath);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, algLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_3));
            il.Add(new CilInstruction(CilOpCodes.Ceq));
            il.Add(new CilInstruction(CilOpCodes.Brtrue, lzfPathRef));

            // if (alg == 2) -> RLE, else RAW
            il.Add(new CilInstruction(CilOpCodes.Ldloc, algLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_2));
            il.Add(new CilInstruction(CilOpCodes.Ceq));
            il.Add(new CilInstruction(CilOpCodes.Brfalse, useRawRef));

            // RLE decode → outBytes = new byte[uncomp]; for (i=0, p=9; i<uncomp;) { count=payload[p++]; value=payload[p++]; fill }
            var iLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            var pLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            var countLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            var valLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Byte));
            body.LocalVariables.Add(iLocal);
            body.LocalVariables.Add(pLocal);
            body.LocalVariables.Add(countLocal);
            body.LocalVariables.Add(valLocal);

            il.Add(new CilInstruction(CilOpCodes.Ldloc, uncompLocal));
            il.Add(new CilInstruction(CilOpCodes.Newarr, module.CorLibTypeFactory.Byte.Type));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 9));
            il.Add(new CilInstruction(CilOpCodes.Stloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Stloc, iLocal));

            var loopRle = new CilInstruction(CilOpCodes.Nop);
            var loopRleRef = new CilInstructionLabel(loopRle);
            il.Add(new CilInstruction(CilOpCodes.Br, loopRleRef));

            // cuerpo: count = payload[p++]; val = payload[p++]; for (k=0;k<count;k++) out[i++]=val;
            var bodyRle = new CilInstruction(CilOpCodes.Nop);
            var bodyRleRef = new CilInstructionLabel(bodyRle);
            il.Add(bodyRle);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Stloc, countLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Stloc, valLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, pLocal));

            var kLocal = new CilLocalVariable(importer.ImportTypeSignature(module.CorLibTypeFactory.Int32));
            body.LocalVariables.Add(kLocal);
            var loopKCheck = new CilInstruction(CilOpCodes.Nop);
            var loopKCheckRef = new CilInstructionLabel(loopKCheck);
            var loopKBody = new CilInstruction(CilOpCodes.Nop);
            var loopKBodyRef = new CilInstructionLabel(loopKBody);
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Stloc, kLocal));
            il.Add(new CilInstruction(CilOpCodes.Br, loopKCheckRef));
            il.Add(loopKBody);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, iLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, valLocal));
            il.Add(new CilInstruction(CilOpCodes.Stelem_I1));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, iLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, iLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, kLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, kLocal));
            il.Add(loopKCheck);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, kLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, countLocal));
            il.Add(new CilInstruction(CilOpCodes.Blt, loopKBodyRef));

            // if (i < uncomp) goto bodyRle
            il.Add(loopRle);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, iLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, uncompLocal));
            il.Add(new CilInstruction(CilOpCodes.Blt, bodyRleRef));
            il.Add(new CilInstruction(CilOpCodes.Br, afterDecodeRef));

            // LZF path
            il.Add(lzfPath);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, dataPtrLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, compLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, uncompLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(lzfDecompressMethod)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Br, afterDecodeRef));

            // useRaw: outBytes = new byte[comp]; Array.Copy(payload, 9, outBytes, 0, comp)
            il.Add(useRaw);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, compLocal));
            il.Add(new CilInstruction(CilOpCodes.Newarr, module.CorLibTypeFactory.Byte.Type));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 9));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, compLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(arrayCopy)));

            il.Add(afterDecode);

            // loadedAsm = Assembly.Load(outBytes)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(asmLoad)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, loadedAsmLocal));

            // loadedName = loadedAsm.GetName().Name
            il.Add(new CilInstruction(CilOpCodes.Ldloc, loadedAsmLocal));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(asm_getName)));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(asmName_getName)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, loadedNameLocal));

            // if (string.Equals(loadedName, reqName)) return loadedAsm;
            il.Add(new CilInstruction(CilOpCodes.Ldloc, loadedNameLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, reqNameLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(stringEquals)));
            var contLabel = new CilInstruction(CilOpCodes.Nop);
            var contLabelRef = new CilInstructionLabel(contLabel);
            il.Add(new CilInstruction(CilOpCodes.Brfalse, contLabelRef));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, loadedAsmLocal));
            il.Add(new CilInstruction(CilOpCodes.Ret));
            il.Add(contLabel);

            // offset -= (8 + len)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Stloc, offsetLocal));

            // loopCheck:
            il.Add(loopCheck);
            // if (offset < 8) return null;
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Blt, retNullLabel));
            // Comprobar magic 'S1NG'
            for (int k = 0; k < 4; k++)
            {
                il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
                il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
                il.Add(new CilInstruction(CilOpCodes.Sub));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, k));
                il.Add(new CilInstruction(CilOpCodes.Add));
                il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, k == 0 ? (int)'S' : k == 1 ? (int)'1' : k == 2 ? (int)'N' : (int)'G'));
                il.Add(new CilInstruction(CilOpCodes.Bne_Un, retNullLabel));
            }
            // saltar a loopStart
            il.Add(new CilInstruction(CilOpCodes.Br, loopStartLabel));

            // ret null
            il.Add(retNull);
            il.Add(new CilInstruction(CilOpCodes.Ldnull));
            il.Add(new CilInstruction(CilOpCodes.Ret));

            return method;
        }

        private static MethodDefinition CreatePreloadEmbeddedMethod(
            ModuleDefinition module,
            ReferenceImporter importer)
        {
            var voidSig = module.CorLibTypeFactory.Void;
            var sig = MethodSignature.CreateStatic(voidSig);
            var method = new MethodDefinition("PreloadEmbedded", MethodAttributes.Private | MethodAttributes.Static, sig);
            method.CilMethodBody = new CilMethodBody(method);

            var body = method.CilMethodBody;
            var il = body.Instructions;

            // Locales: string path; byte[] exeBytes; int offset; int len; byte[] payload;
            var stringSig = module.CorLibTypeFactory.String;
            var byteArraySig = module.CorLibTypeFactory.Byte.MakeSzArrayType();
            var intSig = module.CorLibTypeFactory.Int32;
            var pathLocal = new CilLocalVariable(importer.ImportTypeSignature(stringSig));
            var exeBytesLocal = new CilLocalVariable(importer.ImportTypeSignature(byteArraySig));
            var offsetLocal = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var lenLocal = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var payloadLocal = new CilLocalVariable(importer.ImportTypeSignature(byteArraySig));
            body.LocalVariables.Add(pathLocal);
            body.LocalVariables.Add(exeBytesLocal);
            body.LocalVariables.Add(offsetLocal);
            body.LocalVariables.Add(lenLocal);
            body.LocalVariables.Add(payloadLocal);

            // Métodos usados
            var getExecuting = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), nameof(Assembly.GetExecutingAssembly), Array.Empty<string>());
            var getLocation = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), $"get_{nameof(Assembly.Location)}", Array.Empty<string>());
            var fileReadAllBytes = GetCorLibMethod(module, "System.IO", nameof(File), nameof(File.ReadAllBytes), "System.String");
            var bitConverterToInt32 = GetCorLibMethod(module, "System", nameof(BitConverter), nameof(BitConverter.ToInt32), "System.Byte[]", "System.Int32");
            var arrayCopy = GetCorLibMethod(module, "System", nameof(Array), nameof(Array.Copy), "System.Array", "System.Int32", "System.Array", "System.Int32", "System.Int32");
            var asmLoad = GetCorLibMethod(module, "System.Reflection", nameof(Assembly), nameof(Assembly.Load), "System.Byte[]");

            // path = Assembly.GetExecutingAssembly().Location
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getExecuting)));
            il.Add(new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getLocation)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, pathLocal));

            // exeBytes = File.ReadAllBytes(path)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, pathLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(fileReadAllBytes)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, exeBytesLocal));

            // offset = exeBytes.Length
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldlen));
            il.Add(new CilInstruction(CilOpCodes.Conv_I4));
            il.Add(new CilInstruction(CilOpCodes.Stloc, offsetLocal));

            // while (offset >= 8) { len = BitConverter.ToInt32(exeBytes, offset - 4); if (exeBytes[offset-8..offset-4] != 'S1NG') break; payload = new byte[len]; Array.Copy(...); Assembly.Load(payload); offset -= (8 + len); }
            var loopCheck = new CilInstruction(CilOpCodes.Nop);
            var loopCheckLabel = new CilInstructionLabel(loopCheck);
            var loopStart = new CilInstruction(CilOpCodes.Nop);
            var loopStartLabel = new CilInstructionLabel(loopStart);

            // goto loopCheck
            il.Add(new CilInstruction(CilOpCodes.Br, loopCheckLabel));

            // loopStart:
            il.Add(loopStart);

            // len = BitConverter.ToInt32(exeBytes, offset - 4)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 4));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(bitConverterToInt32)));
            il.Add(new CilInstruction(CilOpCodes.Stloc, lenLocal));

            // Comprobar 'S','1','N','G' en offset-8..offset-5
            // if (offset < 8) break; (ya garantizado por condición)

            // payload = new byte[len]
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Newarr, module.CorLibTypeFactory.Byte.Type));
            il.Add(new CilInstruction(CilOpCodes.Stloc, payloadLocal));

            // Array.Copy(exeBytes, offset - 8 - len, payload, 0, len)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Sub)); // srcIndex
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(arrayCopy)));

            // Assembly.Load(payload)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, payloadLocal));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(asmLoad)));
            il.Add(new CilInstruction(CilOpCodes.Pop));

            // offset -= (8 + len)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, lenLocal));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Stloc, offsetLocal));

            // loopCheck:
            il.Add(loopCheck);
            // if (offset < 8) return;
            var afterReturn = new CilInstruction(CilOpCodes.Nop);
            var afterReturnLabel = new CilInstructionLabel(afterReturn);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Blt_S, afterReturnLabel));
            // if (exeBytes[offset-8..offset-4] != 'S1NG') return;
            // Comprobar magic
            // Cargar exeBytes[offset-8 + k]
            for (int k = 0; k < 4; k++)
            {
                il.Add(new CilInstruction(CilOpCodes.Ldloc, exeBytesLocal));
                il.Add(new CilInstruction(CilOpCodes.Ldloc, offsetLocal));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
                il.Add(new CilInstruction(CilOpCodes.Sub));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, k));
                il.Add(new CilInstruction(CilOpCodes.Add));
                il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
                il.Add(new CilInstruction(CilOpCodes.Ldc_I4, k == 0 ? (int)'S' : k == 1 ? (int)'1' : k == 2 ? (int)'N' : (int)'G'));
                il.Add(new CilInstruction(CilOpCodes.Bne_Un_S, afterReturnLabel));
            }
            // saltar a loopStart
            il.Add(new CilInstruction(CilOpCodes.Br, loopStartLabel));
            // afterReturn:
            il.Add(afterReturn);
            il.Add(new CilInstruction(CilOpCodes.Ret));

            return method;
        }

        private static byte[] RleCompress(byte[] input)
        {
            try
            {
                if (input == null || input.Length == 0) return Array.Empty<byte>();
                using (var ms = new MemoryStream())
                {
                    int i = 0;
                    while (i < input.Length)
                    {
                        byte value = input[i];
                        int count = 1;
                        while (count < 255 && i + count < input.Length && input[i + count] == value)
                            count++;
                        ms.WriteByte((byte)count);
                        ms.WriteByte(value);
                        i += count;
                    }
                    return ms.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        private static byte[] RleDecompress(byte[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<byte>();
            using (var ms = new MemoryStream())
            {
                int i = 0;
                while (i < input.Length)
                {
                    byte count = input[i];
                    byte value = input[i + 1];
                    for (int j = 0; j < count; j++)
                    {
                        ms.WriteByte(value);
                    }
                    i += 2;
                }
                return ms.ToArray();
            }
        }

        private static byte[] LzfTryCompress(byte[] input)
        {
            try
            {
                // Implementación simple LZF (liblzf) encoder: si no mejora, devuelve null.
                if (input == null || input.Length == 0) return Array.Empty<byte>();
                // Estimar tamaño: peor caso ~ input.Length + input.Length/16 + 64 + 3
                var hashSize = 1 << 14;
                var hash = new int[hashSize];
                for (int i = 0; i < hash.Length; i++) hash[i] = -1;
                var output = new byte[input.Length + (input.Length >> 4) + 64 + 3];
                int ip = 0, op = 0;
                int lit = 0;

                void FlushLiteral()
                {
                    if (lit == 0) return;
                    // ctrl = lit - 1 (0..31)
                    output[op++] = (byte)(lit - 1);
                    Buffer.BlockCopy(input, ip - lit, output, op, lit);
                    op += lit;
                    lit = 0;
                }

                while (ip + 2 < input.Length)
                {
                    // Hash trío de bytes
                    int h = ((input[ip] << 8) ^ (input[ip + 1] << 4) ^ input[ip + 2]) & (hashSize - 1);
                    int refPos = hash[h];
                    hash[h] = ip;

                    int dist = ip - refPos - 1;
                    // Comprobar coincidencia mínima de 3 bytes y distancia < 8192
                    if (refPos >= 0 && dist < 8192 &&
                        input[refPos] == input[ip] && input[refPos + 1] == input[ip + 1] && input[refPos + 2] == input[ip + 2])
                    {
                        // Emitir literales pendientes
                        FlushLiteral();

                        // Avanzar tras los 3 bytes base
                        ip += 3;
                        refPos += 3;

                        // Medir longitud extra (más allá de 3), limitado a 7 + 255 (extensión)
                        int extra = 0;
                        int maxExtra = 7 + 255;
                        while (extra < maxExtra && ip < input.Length && refPos < input.Length && input[refPos] == input[ip])
                        {
                            extra++;
                            ip++;
                            refPos++;
                        }

                        int lEncoded = 1 + extra; // (len real = 2 + lEncoded) → mínimo 3
                        int ctrlLen = lEncoded < 7 ? lEncoded : 7; // 0..7 pero nunca 0 aquí

                        // ctrl: [len(3 bits)] [dist_hi(5 bits)]
                        int ctrl = (ctrlLen << 5) | ((dist >> 8) & 0x1F);
                        output[op++] = (byte)ctrl;
                        output[op++] = (byte)(dist & 0xFF);
                        if (ctrlLen == 7)
                        {
                            output[op++] = (byte)(lEncoded - 7);
                        }

                        // Continuar loop; no actualizar hash intermedio para simplicidad
                        continue;
                    }
                    else
                    {
                        // Literal: solo avanzar y contar; se flushea en bloques de 32
                        ip++;
                        lit++;
                        if (lit == 32)
                            FlushLiteral();
                    }
                }

                // Copiar literales restantes (incluye los <3 al final)
                // Añadir cualquier byte de cola sobrante (<3) a literales
                while (ip < input.Length)
                {
                    ip++;
                    lit++;
                    if (lit == 32)
                        FlushLiteral();
                }
                FlushLiteral();

                if (op >= input.Length)
                {
                    // No mejora
                    return input;
                }
                // Verificar que decompresión coincide
                var result = new byte[op];
                Buffer.BlockCopy(output, 0, result, 0, op);
                try
                {
                    var roundtrip = DecompressLzfPack(result, 0, result.Length, input.Length);
                    if (roundtrip != null && roundtrip.Length == input.Length)
                    {
                        // Comparar contenido
                        for (int i = 0; i < input.Length; i++)
                        {
                            if (roundtrip[i] != input[i])
                                return input; // fallo verificación
                        }
                        return result;
                    }
                    return input;
                }
                catch
                {
                    return input;
                }
            }
            catch { return null; }
        }

        // Descompresión LZF en empaquetador para validar (equivalente a IL en runtime)
        private static byte[] DecompressLzfPack(byte[] data, int offset, int length, int expectedSize)
        {
            var output = new byte[expectedSize];
            int inPos = offset;
            int inEnd = offset + length;
            int outPos = 0;
            while (inPos < inEnd)
            {
                int ctrl = data[inPos++];
                if (ctrl < 32)
                {
                    int lit = ctrl + 1;
                    Buffer.BlockCopy(data, inPos, output, outPos, lit);
                    inPos += lit;
                    outPos += lit;
                }
                else
                {
                    int len = ctrl >> 5;
                    int refPos = outPos - (((ctrl & 31) << 8) + 1);
                    refPos -= data[inPos++];
                    if (len == 7)
                        len += data[inPos++];
                    len += 2;
                    for (int k = 0; k < len; k++)
                        output[outPos++] = output[refPos++];
                }
            }
            return output;
        }

        private static IMethodDescriptor GetCorLibMethod(
         ModuleDefinition moduleDefinition,
         string ns,
         string typename,
         string methodName,
         params string[] parametersFullName)
        {
            var importer = new ReferenceImporter(moduleDefinition);
            var typeRef = new TypeReference(moduleDefinition.CorLibTypeFactory.CorLibScope, ns, typename);

            var resolvedReference = importer.ImportType(typeRef).Resolve();

            if (resolvedReference == null) return null;

            foreach (var method in resolvedReference.Methods)
            {
                if (method.Name != methodName) continue;

                string[] typeNames = method.Parameters.Select(p => p.ParameterType.FullName).ToArray();

                if (!StringEquals(parametersFullName, typeNames)) continue;

                return method;
            }

            return null;

            bool StringEquals(IReadOnlyCollection<string> a, IReadOnlyList<string> b)
            {
                if (a.Count != b.Count) return false;
                return !a.Where((t, x) => t != b[x]).Any();
            }
        }

        private static MethodDefinition CreateLzfDecompressMethod(
            ModuleDefinition module,
            ReferenceImporter importer)
        {
            var byteArray = module.CorLibTypeFactory.Byte.MakeSzArrayType();
            var intSig = module.CorLibTypeFactory.Int32;
            var ret = importer.ImportTypeSignature(byteArray);
            var sig = MethodSignature.CreateStatic(ret,
                importer.ImportTypeSignature(byteArray),
                importer.ImportTypeSignature(intSig),
                importer.ImportTypeSignature(intSig),
                importer.ImportTypeSignature(intSig));

            var method = new MethodDefinition("DecompressLzf", MethodAttributes.Private | MethodAttributes.Static, sig);
            method.CilMethodBody = new CilMethodBody(method);
            var il = method.CilMethodBody.Instructions;
            var body = method.CilMethodBody;

            // Locals
            var outBytes = new CilLocalVariable(importer.ImportTypeSignature(byteArray));
            var inPos = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var inEnd = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var outPos = new CilLocalVariable(importer.ImportTypeSignature(intSig));
            var ctrl = new CilLocalVariable(module.CorLibTypeFactory.Int32);
            var len = new CilLocalVariable(module.CorLibTypeFactory.Int32);
            var refPos = new CilLocalVariable(module.CorLibTypeFactory.Int32);
            var k = new CilLocalVariable(module.CorLibTypeFactory.Int32);
            body.LocalVariables.Add(outBytes);
            body.LocalVariables.Add(inPos);
            body.LocalVariables.Add(inEnd);
            body.LocalVariables.Add(outPos);
            body.LocalVariables.Add(ctrl);
            body.LocalVariables.Add(len);
            body.LocalVariables.Add(refPos);
            body.LocalVariables.Add(k);

            // outBytes = new byte[expectedSize]
            il.Add(new CilInstruction(CilOpCodes.Ldarg_3));
            il.Add(new CilInstruction(CilOpCodes.Newarr, module.CorLibTypeFactory.Byte.Type));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outBytes));

            // inPos = offset; inEnd = offset + length; outPos = 0
            il.Add(new CilInstruction(CilOpCodes.Ldarg_1));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldarg_1));
            il.Add(new CilInstruction(CilOpCodes.Ldarg_2));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inEnd));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outPos));

            var loop = new CilInstruction(CilOpCodes.Nop);
            var loopRef = new CilInstructionLabel(loop);
            var loopBody = new CilInstruction(CilOpCodes.Nop);
            var loopBodyRef = new CilInstructionLabel(loopBody);
            var done = new CilInstruction(CilOpCodes.Nop);
            var doneRef = new CilInstructionLabel(done);

            // while (inPos < inEnd)
            il.Add(new CilInstruction(CilOpCodes.Br, loopRef));
            il.Add(loopBody);

            // ctrl = input[inPos++]
            il.Add(new CilInstruction(CilOpCodes.Ldarg_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Stloc, ctrl));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inPos));

            // if (ctrl < 32) literal run
            var backrefPath = new CilInstruction(CilOpCodes.Nop);
            var backrefPathRef = new CilInstructionLabel(backrefPath);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, ctrl));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 32));
            il.Add(new CilInstruction(CilOpCodes.Bge, backrefPathRef));
            // literal length = ctrl + 1
            il.Add(new CilInstruction(CilOpCodes.Ldloc, ctrl));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, len));
            // Array.Copy(input, inPos, outBytes, outPos, len)
            var arrayCopy = GetCorLibMethod(module, "System", nameof(Array), nameof(Array.Copy), "System.Array", "System.Int32", "System.Array", "System.Int32", "System.Int32");
            il.Add(new CilInstruction(CilOpCodes.Ldarg_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytes));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Call, importer.ImportMethod(arrayCopy)));
            // inPos += len; outPos += len
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Br, loopRef));

            // backref path
            il.Add(backrefPath);
            // len = ctrl >> 5
            il.Add(new CilInstruction(CilOpCodes.Ldloc, ctrl));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 5));
            il.Add(new CilInstruction(CilOpCodes.Shr));
            il.Add(new CilInstruction(CilOpCodes.Stloc, len));
            // refPos = outPos - (((ctrl & 0x1F) << 8) + 1)
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, ctrl));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 0x1F));
            il.Add(new CilInstruction(CilOpCodes.And));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 8));
            il.Add(new CilInstruction(CilOpCodes.Shl));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Stloc, refPos));
            // refPos -= input[inPos++]
            il.Add(new CilInstruction(CilOpCodes.Ldloc, refPos));
            il.Add(new CilInstruction(CilOpCodes.Ldarg_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Sub));
            il.Add(new CilInstruction(CilOpCodes.Stloc, refPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inPos));
            // if (len == 7) len += input[inPos++]
            var afterLenExt = new CilInstruction(CilOpCodes.Nop);
            var afterLenExtRef = new CilInstructionLabel(afterLenExt);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4, 7));
            il.Add(new CilInstruction(CilOpCodes.Bne_Un, afterLenExtRef));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Ldarg_0));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, len));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, inPos));
            il.Add(afterLenExt);
            // len += 2
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_2));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, len));
            // for (k=0; k<len; k++) outBytes[outPos++] = outBytes[refPos++]
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_0));
            il.Add(new CilInstruction(CilOpCodes.Stloc, k));
            var kCheck = new CilInstruction(CilOpCodes.Nop);
            var kCheckRef = new CilInstructionLabel(kCheck);
            var kBody = new CilInstruction(CilOpCodes.Nop);
            var kBodyRef = new CilInstructionLabel(kBody);
            il.Add(new CilInstruction(CilOpCodes.Br, kCheckRef));
            il.Add(kBody);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytes));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytes));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, refPos));
            il.Add(new CilInstruction(CilOpCodes.Ldelem_U1));
            il.Add(new CilInstruction(CilOpCodes.Stelem_I1));
            // outPos++
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outPos));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, outPos));
            // refPos++
            il.Add(new CilInstruction(CilOpCodes.Ldloc, refPos));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, refPos));
            // k++
            il.Add(new CilInstruction(CilOpCodes.Ldloc, k));
            il.Add(new CilInstruction(CilOpCodes.Ldc_I4_1));
            il.Add(new CilInstruction(CilOpCodes.Add));
            il.Add(new CilInstruction(CilOpCodes.Stloc, k));
            il.Add(kCheck);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, k));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, len));
            il.Add(new CilInstruction(CilOpCodes.Blt, kBodyRef));
            il.Add(new CilInstruction(CilOpCodes.Br, loopRef));

            // loop condition
            il.Add(loop);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inPos));
            il.Add(new CilInstruction(CilOpCodes.Ldloc, inEnd));
            il.Add(new CilInstruction(CilOpCodes.Blt, loopBodyRef));

            // return outBytes
            il.Add(done);
            il.Add(new CilInstruction(CilOpCodes.Ldloc, outBytes));
            il.Add(new CilInstruction(CilOpCodes.Ret));

            return method;
        }
    }
}