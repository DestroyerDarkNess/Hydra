using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;

namespace HydraEngine.Protection.Calli
{
    public class CallObfuscation : Models.Protection
    {
        public CallObfuscation()
            : base("Protection.Calli.CallObfuscation",
                "Call Obfuscation",
                "Transforms call/callvirt into calli using a function-pointer table, with progressive analysis mode.")
        {
            ManualReload = true;
        }

        /// <summary>
        /// When true, runs a progressive analysis mode:
        /// 1) Try to protect the whole assembly.
        /// 2) If writing fails, try by type (class), accumulating successful ones.
        /// 3) If a type fails, try method-by-method, mark failing methods in a blacklist.
        /// 4) Retry skipping blacklisted methods to achieve a best-effort working build.
        /// </summary>
        public bool AnalisisMode { get; set; } = false;

        /// <summary>
        /// Accumulated blacklist of method keys to skip during transformation.
        /// </summary>
        public HashSet<string> BlacklistedMethods { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Keeps the last valid build in memory (best effort) during analysis mode.
        /// </summary>
        private MemoryStream _lastGoodStream;

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                if (!AnalisisMode)
                {
                    // Normal mode: single attempt.
                    if (TryBuildWith(moduledef, allowedCallers: null, extraBlacklist: null, out var built, out var error))
                    {
                        TempModule = built ?? throw new Exception("MemoryStream is null");
                        return true;
                    }

                    this.Errors = error ?? new Exception("Failed to write assembly (normal mode).");
                    return false;
                }

                // =============================
                // PROGRESSIVE ANALYSIS MODE
                // =============================

                // 1) Full attempt
                if (TryBuildWith(moduledef, allowedCallers: null, extraBlacklist: BlacklistedMethods, out var fullBuilt, out var fullError))
                {
                    TempModule = fullBuilt ?? throw new Exception("MemoryStream is null");
                    return true;
                }

                Console.WriteLine("[AnalysisMode] Full module build failed; starting per-type analysis...");

                // 2) Map eligible caller methods by type
                var byType = GetEligibleMethodKeysByType(moduledef);

                // Accumulated set of allowed callers that we will transform
                var acceptedCallers = new HashSet<string>(StringComparer.Ordinal);

                // 3) Try by type (fast path: whole type; if it fails, go method-by-method)
                foreach (var kv in byType)
                {
                    var typeName = kv.Key;
                    var typeMethods = kv.Value;

                    var tryAll = new HashSet<string>(acceptedCallers, StringComparer.Ordinal);
                    foreach (var mk in typeMethods)
                        tryAll.Add(mk);

                    if (TryBuildWith(moduledef, tryAll, BlacklistedMethods, out var builtAllType, out var errAllType))
                    {
                        // Whole type OK → consolidate
                        acceptedCallers = tryAll;
                        _lastGoodStream = builtAllType;
                        Console.WriteLine($"[AnalysisMode] Type OK (whole): {typeName} (methods: {typeMethods.Count})");
                        continue;
                    }

                    Console.WriteLine($"[AnalysisMode] Type FAILED (whole): {typeName} → trying per-method...");

                    // 4) Per-method within the type
                    foreach (var methodKey in typeMethods)
                    {
                        if (BlacklistedMethods.Contains(methodKey))
                            continue;

                        var tryOne = new HashSet<string>(acceptedCallers, StringComparer.Ordinal) { methodKey };

                        if (TryBuildWith(moduledef, tryOne, BlacklistedMethods, out var builtOne, out var errOne))
                        {
                            // Method OK → consolidate
                            acceptedCallers = tryOne;
                            _lastGoodStream = builtOne;
                            Console.WriteLine($"[AnalysisMode] Method OK: {methodKey}");
                        }
                        else
                        {
                            // Method problematic → blacklist + RED message
                            BlacklistedMethods.Add(methodKey);
                            WriteLineError($"[AnalysisMode] Method FAILED → blacklisted: {methodKey}");
                        }
                    }
                }

                // 5) Final attempt with all accepted callers and skipping blacklist
                if (TryBuildWith(moduledef, acceptedCallers, BlacklistedMethods, out var finalBuilt, out var finalErr))
                {
                    TempModule = finalBuilt ?? _lastGoodStream ?? throw new Exception("MemoryStream is null at the end.");
                    return true;
                }

                // If still failing, but we have a last good partial build, return that
                if (_lastGoodStream != null)
                {
                    TempModule = _lastGoodStream;
                    Console.WriteLine("[AnalysisMode] Using last valid partial build.");
                    return true;
                }

                this.Errors = finalErr ?? fullError ?? new Exception("Failed to write assembly even after progressive analysis.");
                return false;
            }
            catch (Exception ex)
            {
                WriteLineError($"[CallObfuscation] Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                this.Errors = ex;
                return false;
            }
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(tempPath); } catch (Exception Ex) { this.Errors = Ex; }
            return Execute(tempPath);
        }

        // =============================
        // BUILD / TRANSFORM CORE
        // =============================

        /// <summary>
        /// Applies transformation (with optional filters) and writes to a MemoryStream.
        /// </summary>
        private bool TryBuildWith(
            string modulePath,
            HashSet<string> allowedCallers,
            HashSet<string> extraBlacklist,
            out MemoryStream builtStream,
            out Exception error)
        {
            builtStream = null;
            error = null;

            ModuleDefinition moduleDefinition = null;

            try
            {
                moduleDefinition = ModuleDefinition.FromFile(modulePath);

                // Apply transformation on a fresh module instance
                ApplyCalliTransform(moduleDefinition, allowedCallers, extraBlacklist);

                // Write
                var ms = new MemoryStream();
                moduleDefinition.Write(ms, new ManagedPEImageBuilder(MetadataBuilderFlags.PreserveAll));
                ms.Position = 0;
                builtStream = ms;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
            finally
            {
                // ModuleDefinition is GC-managed; nothing to dispose explicitly.
            }
        }

        /// <summary>
        /// Rewrites call/callvirt → calli via a function-pointer table in module .cctor.
        /// Respects allowedCallers (if not null) and skips any key present in blacklist.
        /// </summary>
        private void ApplyCalliTransform(
            ModuleDefinition moduleDefinition,
            HashSet<string> allowedCallers,
            HashSet<string> blacklist)
        {
            var globalConstructor = moduleDefinition.GetOrCreateModuleConstructor();

            var functionPointerArray = CreateFunctionPointerArray(moduleDefinition);
            moduleDefinition.GetOrCreateModuleType().Fields.Add(functionPointerArray);

            var importer = new ReferenceImporter(moduleDefinition);

            if (globalConstructor.CilMethodBody == null)
                globalConstructor.CilMethodBody = new CilMethodBody(globalConstructor);

            // Remove any pre-existing 'ret' so we can prepend initialization
            if (globalConstructor.CilMethodBody.Instructions.Any(i => i.OpCode.Code is CilCode.Ret))
            {
                foreach (var ret in globalConstructor.CilMethodBody.Instructions
                             .Where(i => i.OpCode.Code is CilCode.Ret).ToArray())
                    globalConstructor.CilMethodBody.Instructions.Remove(ret);
            }

            // Corlib helpers used during .cctor
            var getTypeFromHandle = GetCorLibMethod(moduleDefinition,
                "System", nameof(Type),
                nameof(Type.GetTypeFromHandle), "System.RuntimeTypeHandle");
            var getModule = GetCorLibMethod(moduleDefinition,
                "System", nameof(Type),
                $"get_{nameof(Type.Module)}", Array.Empty<string>());
            var resolveMethod = GetCorLibMethod(moduleDefinition,
                "System.Reflection", nameof(Module),
                nameof(Module.ResolveMethod), "System.Int32");
            var getMethodHandle = GetCorLibMethod(moduleDefinition,
                "System.Reflection", nameof(MethodBase),
                $"get_{nameof(MethodBase.MethodHandle)}", Array.Empty<string>());
            var getFunctionPointer = GetCorLibMethod(moduleDefinition,
                "System", nameof(RuntimeMethodHandle),
                nameof(RuntimeMethodHandle.GetFunctionPointer), Array.Empty<string>());

            if (getTypeFromHandle == null || getModule == null || resolveMethod == null ||
                getMethodHandle == null || getFunctionPointer == null)
                throw new InvalidOperationException("Required corlib methods for initialization were not found.");

            var functionPointerType = getFunctionPointer.DeclaringType
                                      ?? throw new InvalidOperationException("DeclaringType of GetFunctionPointer is null.");

            var loadAddress = new CilLocalVariable(importer.ImportTypeSignature(functionPointerType.ToTypeSignature()));
            globalConstructor.CilMethodBody.LocalVariables.Add(loadAddress);

            var identifierCache = new Dictionary<IMethodDescriptor, int>();
            int currentIndex = 0;

            // Enumerate eligible caller methods
            var methods = moduleDefinition
                .GetAllTypes()
                .SelectMany(t => t.Methods)
                .Where(m => m?.CilMethodBody != null)
                .Where(IsEligibleCallerMethod)
                .ToArray();

            foreach (var method in methods)
            {
                var methodKey = GetMethodKey(method);

                // Respect blacklist and allowed set
                if (blacklist != null && blacklist.Contains(methodKey))
                    continue;
                if (allowedCallers != null && !allowedCallers.Contains(methodKey))
                    continue;

                var instructions = method.CilMethodBody.Instructions;
                instructions.ExpandMacros();

                for (int i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];

                    if (!(instruction.OpCode == CilOpCodes.Call || instruction.OpCode == CilOpCodes.Callvirt))
                        continue;

                    if (!(instruction.Operand is IMethodDescriptor methodDescriptor))
                        continue;

                    // Skip MethodSpecification (generics) and direct MethodDefinition
                    if (methodDescriptor is MethodSpecification) continue;
                    if (methodDescriptor is MethodDefinition) continue;

                    // Skip TypeSpecification declaring types and signatures with sentinels
                    if (methodDescriptor.DeclaringType is TypeSpecification) continue;
                    if (methodDescriptor.Signature != null && methodDescriptor.Signature.IncludeSentinel) continue;

                    // Skip delegates, and valuetypes with 'this' (instance)
                    var resolvedType = methodDescriptor.DeclaringType?.Resolve();
                    if (resolvedType == null || resolvedType.IsDelegate) continue;
                    if (resolvedType.IsValueType && methodDescriptor.Signature.HasThis) continue;

                    // Forbidden prefixes: constrained / tailcall
                    bool hasForbiddenPrefix = false;
                    if (i - 1 >= 0)
                    {
                        var prevInstr = instructions[i - 1];
                        if (prevInstr.OpCode == CilOpCodes.Constrained || prevInstr.OpCode == CilOpCodes.Tailcall)
                            hasForbiddenPrefix = true;
                    }
                    if (hasForbiddenPrefix) continue;

                    // Assign a table index for this call target if needed
                    if (!identifierCache.ContainsKey(methodDescriptor))
                    {
                        identifierCache[methodDescriptor] = currentIndex++;

                        // .cctor: FPTable[idx] = GetFunctionPointer( ResolveMethod(mdToken) )
                        var arrayStoreExpression = new[]
                        {
                            new CilInstruction(CilOpCodes.Ldsfld, functionPointerArray)
                        }
                        .Concat(MutateI4(identifierCache[methodDescriptor]))
                        .Concat(new[]
                        {
                            new CilInstruction(CilOpCodes.Ldtoken, moduleDefinition.GetModuleType()),
                            new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getTypeFromHandle)),
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getModule))
                        })
                        .Concat(MutateI4(methodDescriptor.MetadataToken.ToInt32()))
                        .Concat(new[]
                        {
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(resolveMethod)),
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getMethodHandle)),
                            new CilInstruction(CilOpCodes.Stloc, loadAddress),
                            new CilInstruction(CilOpCodes.Ldloca, loadAddress),
                            new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getFunctionPointer)),
                            new CilInstruction(CilOpCodes.Stelem_I)
                        });

                        globalConstructor.CilMethodBody.Instructions.AddRange(arrayStoreExpression);
                    }

                    // Import signature for calli
                    var importedSig = importer.ImportMethodSignature(methodDescriptor.Signature);

                    // Replace with calli sequence
                    var calliList = new List<CilInstruction>
                    {
                        new CilInstruction(CilOpCodes.Ldsfld, functionPointerArray)
                    };
                    calliList.AddRange(MutateI4(identifierCache[methodDescriptor]));
                    calliList.Add(new CilInstruction(CilOpCodes.Ldelem_I));
                    calliList.Add(new CilInstruction(CilOpCodes.Calli, importedSig.MakeStandAloneSignature()));

                    var calliExpression = calliList.ToArray();

                    // Insert and neutralize original instruction
                    instructions.InsertRange(i, calliExpression);
                    instruction.OpCode = CilOpCodes.Nop;
                    instruction.Operand = null;
                    i += calliExpression.Length;
                }

                instructions.OptimizeMacros();
            }

            // Initialize the pointer table in .cctor
            globalConstructor.CilMethodBody.Instructions.InsertRange(0,
                MutateI4(currentIndex).ToArray()
                    .Concat(new[] { new CilInstruction(CilOpCodes.Newarr, moduleDefinition.CorLibTypeFactory.IntPtr.Type) })
                    .Concat(new[] { new CilInstruction(CilOpCodes.Stsfld, functionPointerArray) }));

            globalConstructor.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
            globalConstructor.CilMethodBody.Instructions.OptimizeMacros();
            globalConstructor.CilMethodBody.InitializeLocals = true;
        }

        // =============================
        // HELPERS
        // =============================

        private static void WriteLineError(string message)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = old;
        }

        private static FieldDefinition CreateFunctionPointerArray(ModuleDefinition moduleDefinition)
        {
            return new FieldDefinition(
                ((char)new Random().Next('a', 'z')).ToString(),
                FieldAttributes.Assembly | FieldAttributes.Static,
                FieldSignature.CreateStatic(moduleDefinition.CorLibTypeFactory.IntPtr.MakeSzArrayType()));
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

            if (resolvedReference == null)
                return null;

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
                for (int i = 0; i < b.Count; i++)
                    if (!a.ElementAt(i).Equals(b[i], StringComparison.Ordinal))
                        return false;
                return true;
            }
        }

        private static IEnumerable<CilInstruction> MutateI4(int value)
        {
            var expression = new List<CilInstruction>();
            var random = new Random();

            expression.AddRange(Mutate(value));

            foreach (var loadI4 in expression.Where(i => i.IsLdcI4()).ToArray())
            {
                int insertIndex = expression.IndexOf(loadI4);
                expression.InsertRange(insertIndex, Mutate(loadI4.GetLdcI4Constant()));
                expression.Remove(loadI4);
            }

            return expression;

            IEnumerable<CilInstruction> Mutate(int i32Value)
            {
                var instructions = new List<CilInstruction>();
                switch (random.Next(3))
                {
                    case 0:
                        int subI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value - subI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, subI32),
                            new CilInstruction(CilOpCodes.Add)
                        });
                        break;

                    case 1:
                        int addI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value + addI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, addI32),
                            new CilInstruction(CilOpCodes.Sub)
                        });
                        break;

                    case 2:
                        int xorI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value ^ xorI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, xorI32),
                            new CilInstruction(CilOpCodes.Xor)
                        });
                        break;
                }

                return instructions;
            }
        }

        private static bool IsEligibleCallerMethod(MethodDefinition method)
        {
            if (method is null || method.CilMethodBody is null)
                return false;

            var declaringType = method.DeclaringType;
            string typeName = declaringType?.Name?.Value ?? string.Empty;
            string methodName = method.Name?.Value ?? string.Empty;

            // Ignore compiler-generated / anonymous / state machines, etc.
            bool hasCompilerGeneratedAttr =
                method.CustomAttributes.Any(ca => ca.Constructor?.DeclaringType?.FullName ==
                                                  "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                || declaringType?.CustomAttributes.Any(ca => ca.Constructor?.DeclaringType?.FullName ==
                                                             "System.Runtime.CompilerServices.CompilerGeneratedAttribute") == true;
            bool isAnonToString = methodName == "ToString" && typeName.Contains("<>f__AnonymousType");
            bool isStateMachineMoveNext = methodName == "MoveNext" &&
                                          (typeName.Contains("d__") || typeName.Contains("<>c__DisplayClass"));
            if (hasCompilerGeneratedAttr || isAnonToString || isStateMachineMoveNext || typeName.StartsWith("<>"))
                return false;

            // Ignore methods with EH (reduces risk during transformation)
            if (method.CilMethodBody.ExceptionHandlers.Count > 0)
                return false;

            return true;
        }

        private static string GetMethodKey(MethodDefinition m)
        {
            // Stable key = declaring type full name + method name + signature
            var typeFullName = m.DeclaringType?.FullName ?? "<null>";
            var sig = m.Signature?.ToString() ?? "";
            return $"{typeFullName}::{m.Name?.Value}{sig}";
        }

        private static Dictionary<string, List<string>> GetEligibleMethodKeysByType(string modulePath)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            var module = ModuleDefinition.FromFile(modulePath);
            foreach (var t in module.GetAllTypes())
            {
                var list = new List<string>();
                foreach (var m in t.Methods)
                {
                    if (m?.CilMethodBody == null)
                        continue;
                    if (!IsEligibleCallerMethod(m))
                        continue;
                    list.Add(GetMethodKey(m));
                }

                if (list.Count > 0)
                    dict[t.FullName] = list;
            }

            return dict;
        }
    }
}