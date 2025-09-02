using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
    /// <summary>
    /// Protection.Inline.MethodToInline
    /// Inlina llamadas a métodos "simples" del mismo módulo:
    /// - sin EH, sin pinvoke, no abstract, no virtual, no genéricos abiertos
    /// - tamaño <= MaxMethodSize, sin 'starg'
    /// Reescribe:
    /// - args + 'this' → nuevos locals en caller (stloc antes de la llamada)
    /// - locals del callee → nuevos locals en caller
    /// - 'ret' → epílogo único con (opcional) local de retorno
    /// - ramas internas/targets se remapean al clon
    /// </summary>
    public class MethodToInline : Models.Protection
    {
        public MethodToInline()
            : base("Protection.Inline.MethodToInline", "IL Rewrite Phase",
                   "Inline small/leaf methods to reduce calls and semantics.")
        { }

        /// <summary> Tamaño máximo (instrucciones IL) del callee para ser elegible. </summary>
        public int MaxMethodSize { get; set; } = 999;

        /// <summary> Evitar inlining de métodos 'virtual' o llamados con 'callvirt'. </summary>
        public bool ForbidVirtual { get; set; } = true;

        /// <summary> Evitar inlining si el callee contiene 'call' (solo leaf). </summary>
        public bool LeafOnly { get; set; } = false;

        /// <summary> Si true, imprime cada site inlinado. </summary>
        public bool Verbose { get; set; } = false;

        // --- Métricas de ejecución ---
        private int _candidates;

        private int _callSitesVisited;
        private int _inlineAttempts;
        private int _inlineSuccesses;
        private readonly HashSet<MethodDef> _callersTouched = new HashSet<MethodDef>(MethodDefEqualityComparer.Instance);
        private readonly HashSet<MethodDef> _calleesInlined = new HashSet<MethodDef>(MethodDefEqualityComparer.Instance);

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            // Reset métricas
            _candidates = 0;
            _callSitesVisited = 0;
            _inlineAttempts = 0;
            _inlineSuccesses = 0;
            _callersTouched.Clear();
            _calleesInlined.Clear();

            try
            {
                // 1) Recolectar candidatos a ser inlinados (callees)
                var inlineable = new HashSet<MethodDef>(
                    Module.GetTypes()
                          .SelectMany(t => t.Methods)
                          .Where(m => CanInlineCallee(m))
                );

                _candidates = inlineable.Count;

                if (inlineable.Count == 0)
                {
                    Console.WriteLine("[MethodToInline] No se encontraron candidatos a inlining.");
                    return true;
                }

                // 2) Recorrer call sites y reemplazar
                foreach (var type in Module.Types)
                {
                    foreach (var caller in type.Methods.Where(m => m.HasBody && m.Body.HasInstructions))
                    {
                        var instrs = caller.Body.Instructions;
                        for (int i = 0; i < instrs.Count; i++)
                        {
                            var ins = instrs[i];
                            if (ins.OpCode.Code != Code.Call && ins.OpCode.Code != Code.Callvirt)
                                continue;

                            _callSitesVisited++;

                            if (!(ins.Operand is IMethod im)) continue;
                            var callee = im.ResolveMethodDef();
                            if (callee == null) continue;
                            if (!inlineable.Contains(callee)) continue;

                            // Si 'callvirt' y ForbidVirtual está activo, saltar.
                            if (ForbidVirtual && ins.OpCode.Code == Code.Callvirt)
                                continue;

                            // Evitar self-inline/recursión obvia
                            if (callee == caller) continue;

                            _inlineAttempts++;

                            if (TryInline(Module, caller, i, callee, out var consumed, out var produced))
                            {
                                // Ajustes de pila/ramas
                                caller.Body.SimplifyMacros(caller.Parameters);
                                caller.Body.Instructions.SimplifyBranches();
                                caller.Body.Instructions.OptimizeBranches();

                                _inlineSuccesses++;
                                _callersTouched.Add(caller);
                                _calleesInlined.Add(callee);

                                if (Verbose)
                                {
                                    Console.WriteLine($"[MethodToInline] Inlined: {callee.FullName} -> {caller.FullName} (site #{i}, +{produced - consumed} IL)");
                                }

                                // Avanzar el índice hasta después del bloque inyectado
                                i += (produced - consumed);
                            }
                        }
                    }
                }

                Console.WriteLine(
                    "[MethodToInline] Candidatos: {0} | CallSites visitados: {1} | Intentos: {2} | Inlined: {3} | Callers únicos tocados: {4} | Callees únicos inlinados: {5}",
                    _candidates, _callSitesVisited, _inlineAttempts, _inlineSuccesses, _callersTouched.Count, _calleesInlined.Count
                );

                return true;
            }
            catch (Exception ex)
            {
                this.Errors = ex;
                Console.WriteLine("[MethodToInline] ERROR: " + ex.Message);
                return false;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        public override Task<bool> Execute(string assembly) =>
            throw new NotImplementedException();

        // --------------------------
        // Elegibilidad del callee
        // --------------------------
        private bool CanInlineCallee(MethodDef m)
        {
            if (m == null) return false;
            if (!m.HasBody || !m.Body.HasInstructions) return false;
            if (m.IsConstructor || m.IsStaticConstructor) return false;
            if (m.IsAbstract || m.IsPinvokeImpl) return false;
            if (m.Body.HasExceptionHandlers) return false;
            if (m.DeclaringType?.IsInterface == true) return false;

            if (ForbidVirtual && (m.IsVirtual || m.IsNewSlot))
                return false;

            if (m.MethodSig == null) return false;
            if (m.MethodSig.GenParamCount > 0) return false; // no genéricos abiertos

            var count = m.Body.Instructions.Count;
            if (count == 0 || count > MaxMethodSize) return false;

            // Compatible con C# 7.3 (|| explícitos)
            bool hasStarg = m.Body.Instructions.Any(ii => ii.OpCode.Code == Code.Starg || ii.OpCode.Code == Code.Starg_S);
            if (hasStarg) return false;

            if (LeafOnly && m.Body.Instructions.Any(ii => ii.OpCode.Code == Code.Call || ii.OpCode.Code == Code.Callvirt))
                return false;

            return true;
        }

        // -------------------------------------
        // Inlining en un call site específico
        // -------------------------------------
        private bool TryInline(ModuleDefMD module, MethodDef caller, int callIndex, MethodDef callee, out int consumedIns, out int producedIns)
        {
            consumedIns = 1; producedIns = 0;

            var callIns = caller.Body.Instructions[callIndex];

            // Preparar el "contexto" del callee
            var calleeBody = callee.Body;
            if (calleeBody == null || !calleeBody.HasInstructions)
                return false;

            // 1) Preparar locals para args en caller (incl. 'this' si aplica)
            int argCount = callee.MethodSig.Params.Count;
            bool hasThis = callee.HasThis;
            int totalArgs = argCount + (hasThis ? 1 : 0);

            var argLocals = new Local[totalArgs];
            int argLocalCount = 0;

            if (hasThis)
            {
                var thisType = callee.DeclaringType.ToTypeSig();
                var l = new Local(thisType) { Name = $"__inl_this_{callee.Name}" };
                caller.Body.Variables.Add(l);
                argLocals[argLocalCount++] = l;
            }

            for (int p = 0; p < argCount; p++)
            {
                var pt = callee.MethodSig.Params[p];
                var l = new Local(pt) { Name = $"__inl_arg{p}_{callee.Name}" };
                caller.Body.Variables.Add(l);
                argLocals[argLocalCount++] = l;
            }

            // 2) Guardar args desde la pila → locals (orden inverso)
            var before = new List<Instruction>();
            for (int idx = totalArgs - 1; idx >= 0; idx--)
                before.Add(Instruction.Create(OpCodes.Stloc, argLocals[idx]));

            // 3) Clonar locals del callee a nuevos locals del caller
            var localMap = new Dictionary<Local, Local>();
            foreach (var loc in calleeBody.Variables)
            {
                var nl = new Local(loc.Type) { Name = $"__inl_loc_{callee.Name}_{loc.Index}" };
                caller.Body.Variables.Add(nl);
                localMap[loc] = nl;
            }

            // 4) Ret unificado
            bool returnsVoid = callee.MethodSig.RetType.ElementType == ElementType.Void;
            Local retLocal = null;
            if (!returnsVoid)
            {
                retLocal = new Local(callee.MethodSig.RetType) { Name = $"__inl_ret_{callee.Name}" };
                caller.Body.Variables.Add(retLocal);
            }

            var endLabel = Instruction.Create(OpCodes.Nop);

            // 5) Clonar instrucciones
            var oldToNew = new Dictionary<Instruction, Instruction>();
            var cloned = new List<Instruction>();

            foreach (var oi in calleeBody.Instructions)
            {
                var ni = Instruction.Create(OpCodes.Nop);
                cloned.Add(ni);
                oldToNew[oi] = ni;
            }

            for (int i = 0; i < calleeBody.Instructions.Count; i++)
            {
                var oi = calleeBody.Instructions[i];
                var ni = oldToNew[oi];

                switch (oi.OpCode.Code)
                {
                    // ------- Args (ldarg_*) -------
                    case Code.Ldarg_0:
                    case Code.Ldarg_1:
                    case Code.Ldarg_2:
                    case Code.Ldarg_3:
                        {
                            int idx = oi.OpCode.Code - Code.Ldarg_0;
                            ni.OpCode = OpCodes.Ldloc;
                            ni.Operand = argLocals[idx];
                            break;
                        }
                    case Code.Ldarg_S:
                    case Code.Ldarg:
                        {
                            int idx = GetArgIndexFromOperand(oi.Operand);
                            ni.OpCode = OpCodes.Ldloc;
                            ni.Operand = argLocals[idx];
                            break;
                        }
                    case Code.Starg:
                    case Code.Starg_S:
                        return false;

                    // ------- Locals -------
                    case Code.Ldloc_0:
                    case Code.Ldloc_1:
                    case Code.Ldloc_2:
                    case Code.Ldloc_3:
                        {
                            int idx = oi.OpCode.Code - Code.Ldloc_0;
                            var oldLocal = calleeBody.Variables[idx];
                            ni.OpCode = OpCodes.Ldloc;
                            ni.Operand = localMap[oldLocal];
                            break;
                        }
                    case Code.Stloc_0:
                    case Code.Stloc_1:
                    case Code.Stloc_2:
                    case Code.Stloc_3:
                        {
                            int idx = oi.OpCode.Code - Code.Stloc_0;
                            var oldLocal = calleeBody.Variables[idx];
                            ni.OpCode = OpCodes.Stloc;
                            ni.Operand = localMap[oldLocal];
                            break;
                        }
                    case Code.Ldloca_S:
                    case Code.Ldloca:
                        {
                            var oldLocal = (Local)oi.Operand;
                            ni.OpCode = OpCodes.Ldloca;
                            ni.Operand = localMap[oldLocal];
                            break;
                        }
                    case Code.Ldloc_S:
                    case Code.Ldloc:
                        {
                            var oldLocal = (Local)oi.Operand;
                            ni.OpCode = OpCodes.Ldloc;
                            ni.Operand = localMap[oldLocal];
                            break;
                        }
                    case Code.Stloc_S:
                    case Code.Stloc:
                        {
                            var oldLocal = (Local)oi.Operand;
                            ni.OpCode = OpCodes.Stloc;
                            ni.Operand = localMap[oldLocal];
                            break;
                        }

                    // ------- Ret → epílogo -------
                    case Code.Ret:
                        {
                            if (returnsVoid)
                            {
                                ni.OpCode = OpCodes.Br;
                                ni.Operand = endLabel;
                            }
                            else
                            {
                                ni.OpCode = OpCodes.Stloc;
                                ni.Operand = retLocal;
                                var br = Instruction.Create(OpCodes.Br, endLabel);
                                cloned.Insert(i + 1, br);
                            }
                            break;
                        }

                    // ------- Otros / ramas -------
                    default:
                        {
                            ni.OpCode = oi.OpCode;
                            ni.Operand = oi.Operand;
                            break;
                        }
                }
            }

            // Remap de targets
            for (int i = 0; i < calleeBody.Instructions.Count; i++)
            {
                var oi = calleeBody.Instructions[i];
                var ni = oldToNew[oi];

                if (ni.Operand is Instruction oT && oldToNew.TryGetValue(oT, out var nT))
                    ni.Operand = nT;
                else if (ni.Operand is Instruction[] oArr)
                {
                    var nArr = new Instruction[oArr.Length];
                    for (int k = 0; k < oArr.Length; k++)
                        nArr[k] = oldToNew[oArr[k]];
                    ni.Operand = nArr;
                }
            }

            // 6) Epílogo
            var after = new List<Instruction> { endLabel };
            if (!returnsVoid)
                after.Add(Instruction.Create(OpCodes.Ldloc, retLocal));

            // 7) Emitir: before + cloned + after; borrar call
            var callerIL = caller.Body.Instructions;

            foreach (var b in before)
                callerIL.Insert(callIndex++, b);

            foreach (var c in cloned)
                callerIL.Insert(callIndex++, c);

            foreach (var a in after)
                callerIL.Insert(callIndex++, a);

            callerIL.RemoveAt(callIndex); // elimina la call original

            consumedIns = 1;
            producedIns = before.Count + cloned.Count + after.Count;

            return true;
        }

        private static int GetArgIndexFromOperand(object operand)
        {
            if (operand is Parameter p)
                return p.Index; // dnlib: Index incluye 'this' como 0 si HasThis
            if (operand is int i)
                return i;
            throw new NotSupportedException("Operand de ldarg/starg no reconocido.");
        }

        // Comparador para HashSet<MethodDef> por MDToken (estable)
        private sealed class MethodDefEqualityComparer : IEqualityComparer<MethodDef>
        {
            public static readonly MethodDefEqualityComparer Instance = new MethodDefEqualityComparer();

            public bool Equals(MethodDef x, MethodDef y) => ReferenceEquals(x, y) || (x?.MDToken == y?.MDToken);

            public int GetHashCode(MethodDef obj) => obj?.MDToken.GetHashCode() ?? 0;
        }
    }
}