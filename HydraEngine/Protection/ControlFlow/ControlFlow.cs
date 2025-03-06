using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using HydraEngine.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.ControlFlow
{
    public class ControlFlow : Models.Protection
    {
        public ControlFlow() : base("Protection.CtrlFlow.ControlFlow", "Renamer Phase", "Description for Renamer Phase") { }

        public bool StrongMode { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                MethodDef methodDef = Module.GlobalType.FindOrCreateStaticConstructor();
                foreach (TypeDef type in Module.GetTypes())
                {
                    if (type.Namespace == "Costura")
                    {
                        continue;
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.HasBody && method.Body.HasInstructions && method.ReturnType != null && !method.MethodHasL2FAttribute() && methodDef != method)
                        {
                            IMethod operand = Module.Import(typeof(Debug).GetMethod("Assert", new Type[1] { typeof(bool) }));
                            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Call, operand));
                            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4_1));
                            if (StrongMode)
                            {
                                PhaseControlFlow(method, Module);
                            }
                            else
                            {
                                PhasePerfControlFlow(method, Module);
                            }
                        }
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

