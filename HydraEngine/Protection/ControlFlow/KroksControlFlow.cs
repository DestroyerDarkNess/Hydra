using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.ControlFlow
{
    public class KroksControlFlow : Models.Protection
    {
        public KroksControlFlow() : base("Protection.CtrlFlow.KroksControlFlow", "Renamer Phase", "Description for Renamer Phase") { }

        public bool StrongMode { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                int repeat = 1;

                if (StrongMode) repeat = 2;

                foreach (var mtt in Module.Types.ToArray())
                {
                    if (mtt == Module.GlobalType) continue;

                    foreach (var method in mtt.Methods.ToArray())
                    {
                        if (method.IsConstructor) continue;
                        if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType) continue;

                        if (method.HasGenericParameters) continue;
                        if (method.IsPinvokeImpl) continue;
                        if (method.IsUnmanagedExport) continue;

                        EXGuard.Core.RTProtections.KroksCFlow.KroksControlFlow.Execute(method, repeat);
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

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        public static void PhasePerfControlFlow(MethodDef method, ModuleDefMD context)
        {
            CilBody body = method.Body;
            body.SimplifyBranches();
            BlockParser.ScopeBlock scopeBlock = BlockParser.ParseBody(body);
            new SwitchMangler2().Mangle(body, scopeBlock, context, method, method.ReturnType);
            body.Instructions.Clear();
            scopeBlock.ToBody(body);
            if (body.PdbMethod != null)
            {
                body.PdbMethod = new PdbMethod
                {
                    Scope = new PdbScope
                    {
                        Start = body.Instructions.First(),
                        End = body.Instructions.Last()
                    }
                };
            }
            method.CustomDebugInfos.RemoveWhere((PdbCustomDebugInfo cdi) => cdi is PdbStateMachineHoistedLocalScopesCustomDebugInfo);
            foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
            {
                int num = body.Instructions.IndexOf(exceptionHandler.TryEnd) + 1;
                exceptionHandler.TryEnd = ((num < body.Instructions.Count) ? body.Instructions[num] : null);
                num = body.Instructions.IndexOf(exceptionHandler.HandlerEnd) + 1;
                exceptionHandler.HandlerEnd = ((num < body.Instructions.Count) ? body.Instructions[num] : null);
            }
        }

        public static void PhaseControlFlow(MethodDef method, ModuleDefMD context)
        {
            CilBody body = method.Body;
            body.SimplifyBranches();
            BlockParser.ScopeBlock scopeBlock = BlockParser.ParseBody(body);
            new SwitchMangler().Mangle(body, scopeBlock, context, method, method.ReturnType);
            body.Instructions.Clear();
            scopeBlock.ToBody(body);
            if (body.PdbMethod != null)
            {
                body.PdbMethod = new PdbMethod
                {
                    Scope = new PdbScope
                    {
                        Start = body.Instructions.First(),
                        End = body.Instructions.Last()
                    }
                };
            }
            method.CustomDebugInfos.RemoveWhere((PdbCustomDebugInfo cdi) => cdi is PdbStateMachineHoistedLocalScopesCustomDebugInfo);
            foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
            {
                int num = body.Instructions.IndexOf(exceptionHandler.TryEnd) + 1;
                exceptionHandler.TryEnd = ((num < body.Instructions.Count) ? body.Instructions[num] : null);
                num = body.Instructions.IndexOf(exceptionHandler.HandlerEnd) + 1;
                exceptionHandler.HandlerEnd = ((num < body.Instructions.Count) ? body.Instructions[num] : null);
            }
        }

    }
}
