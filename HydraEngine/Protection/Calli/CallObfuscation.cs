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
                "Reescritura call/callvirt → calli con tabla de punteros y modo de análisis progresivo.")
        {
            ManualReload = true;
        }

        /// <summary>
        /// Si es true, ejecuta un modo progresivo:
        /// 1) Intenta proteger todo el ensamblado.
        /// 2) Si falla al escribir, prueba por tipo (clase) acumulando los que funcionen.
        /// 3) Si un tipo falla, prueba método por método del tipo, identifica los problemáticos y los agrega a la blacklist.
        /// 4) Repite la compilación saltando los métodos en la blacklist.
        /// </summary>
        public bool AnalisisMode { get; set; } = true;

        /// <summary>
        /// Blacklist acumulada de métodos (clave estable de método) que deben ignorarse al transformar.
        /// </summary>
        public HashSet<string> BlacklistedMethods { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Último flujo ensamblado correcto en memoria.
        /// </summary>
        private MemoryStream _lastGoodStream;

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                if (!AnalisisMode)
                {
                    // Modo normal: un solo intento.
                    if (TryBuildWith(moduledef, allowedCallers: null, extraBlacklist: null, out var built, out var error))
                    {
                        TempModule = built ?? throw new Exception("MemoryStream is null");
                        return true;
                    }

                    this.Errors = error ?? new Exception("Fallo al escribir el ensamblado (modo normal).");
                    return false;
                }

                // =============================
                // MODO DE ANÁLISIS PROGRESIVO
                // =============================

                // 1) Intento completo
                if (TryBuildWith(moduledef, allowedCallers: null, extraBlacklist: BlacklistedMethods, out var fullBuilt, out var fullError))
                {
                    TempModule = fullBuilt ?? throw new Exception("MemoryStream is null");
                    return true;
                }

                Console.WriteLine("[AnalisisMode] Falló compilar todo el módulo; iniciando análisis por tipo...");

                // 2) Mapear métodos elegibles por tipo (clase)
                var byType = GetEligibleMethodKeysByType(moduledef);

                // Conjunto de métodos aceptados acumulados (callers en los que aplicaremos la transformación)
                var acceptedCallers = new HashSet<string>(StringComparer.Ordinal);

                // 3) Probar por tipo (si todo el tipo pasa, se agregan todos sus métodos; si no, vamos por método)
                foreach (var kv in byType)
                {
                    var typeName = kv.Key;
                    var typeMethods = kv.Value; // lista de methodKeys

                    // Intento rápido: agregar todos los métodos del tipo a los ya aceptados
                    var tryAll = new HashSet<string>(acceptedCallers, StringComparer.Ordinal);
                    foreach (var mk in typeMethods)
                        tryAll.Add(mk);

                    if (TryBuildWith(moduledef, tryAll, BlacklistedMethods, out var builtAllType, out var errAllType))
                    {
                        // Todo el tipo ok → se consolidan
                        acceptedCallers = tryAll;
                        _lastGoodStream = builtAllType;
                        Console.WriteLine($"[AnalisisMode] Tipo OK (completo): {typeName} (métodos: {typeMethods.Count})");
                        continue;
                    }

                    Console.WriteLine($"[AnalisisMode] Tipo FALLÓ (completo): {typeName} → intentando por método...");

                    // 4) Por método (dentro del tipo): agrega si pasa, si no → blacklistea
                    foreach (var methodKey in typeMethods)
                    {
                        // Ya blacklisteado previamente, saltar
                        if (BlacklistedMethods.Contains(methodKey))
                            continue;

                        var tryOne = new HashSet<string>(acceptedCallers, StringComparer.Ordinal) { methodKey };

                        if (TryBuildWith(moduledef, tryOne, BlacklistedMethods, out var builtOne, out var errOne))
                        {
                            // Método OK, consolidar
                            acceptedCallers = tryOne;
                            _lastGoodStream = builtOne;
                            Console.WriteLine($"[AnalisisMode] Método OK: {methodKey}");
                        }
                        else
                        {
                            // Método problemático -> blacklist
                            BlacklistedMethods.Add(methodKey);
                            Console.WriteLine($"[AnalisisMode] Método PROBLEMÁTICO → blacklist: {methodKey}");
                        }
                    }
                }

                // 5) Intento final con todos los aceptados y saltando blacklist
                if (TryBuildWith(moduledef, acceptedCallers, BlacklistedMethods, out var finalBuilt, out var finalErr))
                {
                    TempModule = finalBuilt ?? _lastGoodStream ?? throw new Exception("MemoryStream es null al finalizar.");
                    return true;
                }

                // Si igualmente falla, devolver el último bueno si existe
                if (_lastGoodStream != null)
                {
                    TempModule = _lastGoodStream;
                    Console.WriteLine("[AnalisisMode] Usando el último build válido disponible (parcial).");
                    return true;
                }

                this.Errors = finalErr ?? fullError ?? new Exception("Fallo al escribir el ensamblado incluso tras análisis progresivo.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CallObfuscation] Error: {ex.Message}");
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
        // NÚCLEO DE TRANSFORMACIÓN
        // =============================

        /// <summary>
        /// Aplica la transformación (con filtros opcionales) y escribe a MemoryStream.
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

                // Ejecutar transformación en un módulo recién cargado
                ApplyCalliTransform(moduleDefinition, allowedCallers, extraBlacklist);

                // Escribir
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
                // ModuleDefinition no implementa IDisposable, GC se encarga.
            }
        }

        /// <summary>
        /// Aplica la reescritura call/callvirt → calli con tabla de punteros
        /// restringiendo a los "callers" permitidos y saltando blacklist.
        /// </summary>
        private void ApplyCalliTransform(
            ModuleDefinition moduleDefinition,
            HashSet<string> allowedCallers,
            HashSet<string> blacklist)
        {
            // IMPORTANTE: El .cctor del módulo será usado para inicializar la tabla de punteros.
            var globalConstructor = moduleDefinition.GetOrCreateModuleConstructor();

            var functionPointerArray = CreateFunctionPointerArray(moduleDefinition);
            moduleDefinition.GetOrCreateModuleType().Fields.Add(functionPointerArray);

            var importer = new ReferenceImporter(moduleDefinition);

            if (globalConstructor.CilMethodBody == null)
                globalConstructor.CilMethodBody = new CilMethodBody(globalConstructor);

            // Limpiar posibles Ret previos
            if (globalConstructor.CilMethodBody.Instructions.Any(i => i.OpCode.Code is CilCode.Ret))
            {
                foreach (var ret in globalConstructor.CilMethodBody.Instructions
                             .Where(i => i.OpCode.Code is CilCode.Ret).ToArray())
                    globalConstructor.CilMethodBody.Instructions.Remove(ret);
            }

            // Corlib helpers
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
                throw new InvalidOperationException("No se encontraron métodos corlib requeridos para la inicialización.");

            var functionPointerType = getFunctionPointer.DeclaringType
                                      ?? throw new InvalidOperationException("DeclaringType de GetFunctionPointer es null.");

            var loadAddress = new CilLocalVariable(importer.ImportTypeSignature(functionPointerType.ToTypeSignature()));
            globalConstructor.CilMethodBody.LocalVariables.Add(loadAddress);

            var identifierCache = new Dictionary<IMethodDescriptor, int>();
            int currentIndex = 0;

            // Enumerar métodos "caller" candidatos
            var methods = moduleDefinition
                .GetAllTypes()
                .SelectMany(t => t.Methods)
                .Where(m => m?.CilMethodBody != null)
                .Where(IsEligibleCallerMethod) // filtro de seguridad
                .ToArray();

            foreach (var method in methods)
            {
                var methodKey = GetMethodKey(method);

                // Filtrado por lista permitida / blacklist
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

                    // Excluir MethodSpecification (genéricos) o MethodDefinition directo
                    if (methodDescriptor is MethodSpecification) continue;
                    if (methodDescriptor is MethodDefinition) continue;

                    // Excluir TypeSpecification en el declaring type y firmas con sentinel
                    if (methodDescriptor.DeclaringType is TypeSpecification) continue;
                    if (methodDescriptor.Signature != null && methodDescriptor.Signature.IncludeSentinel) continue;

                    // Excluir delegados, value-types con instancia, etc.
                    var resolvedType = methodDescriptor.DeclaringType?.Resolve();
                    if (resolvedType == null || resolvedType.IsDelegate) continue;
                    if (resolvedType.IsValueType && methodDescriptor.Signature.HasThis) continue;

                    // Prefijos prohibidos: constrained / tailcall
                    bool hasForbiddenPrefix = false;
                    if (i - 1 >= 0)
                    {
                        var prevInstr = instructions[i - 1];
                        if (prevInstr.OpCode == CilOpCodes.Constrained || prevInstr.OpCode == CilOpCodes.Tailcall)
                            hasForbiddenPrefix = true;
                    }
                    if (hasForbiddenPrefix) continue;

                    // Asignar índice en la tabla para este destino si no existe
                    if (!identifierCache.ContainsKey(methodDescriptor))
                    {
                        identifierCache[methodDescriptor] = currentIndex++;

                        // global .cctor: FPTable[idx] = GetFunctionPointer( ResolveMethod(mdToken) )
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

                    // Importar firma para calli
                    var importedSig = importer.ImportMethodSignature(methodDescriptor.Signature);

                    // Secuencia calli que sustituye a call/callvirt
                    var calliList = new List<CilInstruction>
                    {
                        new CilInstruction(CilOpCodes.Ldsfld, functionPointerArray)
                    };
                    calliList.AddRange(MutateI4(identifierCache[methodDescriptor]));
                    calliList.Add(new CilInstruction(CilOpCodes.Ldelem_I));
                    calliList.Add(new CilInstruction(CilOpCodes.Calli, importedSig.MakeStandAloneSignature()));

                    var calliExpression = calliList.ToArray();

                    // Insertar y neutralizar instrucción original
                    instructions.InsertRange(i, calliExpression);
                    instruction.OpCode = CilOpCodes.Nop;
                    instruction.Operand = null;
                    i += calliExpression.Length;
                }

                instructions.OptimizeMacros();
            }

            // Inicialización de la tabla en .cctor
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

            // Ignorar generados por el compilador / anónimos / state machines, etc.
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

            // Ignorar métodos con EH complejas (reduce riesgo)
            if (method.CilMethodBody.ExceptionHandlers.Count > 0)
                return false;

            return true;
        }

        private static string GetMethodKey(MethodDefinition m)
        {
            // Clave estable por nombre completo del tipo + nombre + firma
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