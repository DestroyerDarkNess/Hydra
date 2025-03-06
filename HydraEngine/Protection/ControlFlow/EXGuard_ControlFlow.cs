using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using EXGuard.Core.EXECProtections.CEXCFlow;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.ControlFlow
{
    public class EXGuard_ControlFlow : Models.Protection
    {
        public EXGuard_ControlFlow() : base("Protection.CtrlFlow.EXGuard_ControlFlow", "Renamer Phase", "Description for Renamer Phase") { }

        public bool StrongMode { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                int repeat = 1;

                if (StrongMode) repeat = 2;

                foreach (var mtt in Module.Types.ToArray())
                {
                    if (mtt == Module.GlobalType) continue;

                    foreach (var mtm in mtt.Methods.ToArray())
                    {
                        CEXControlFlow.Execute(mtm, repeat);
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
