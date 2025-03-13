using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using System.Linq;

using static SugarGuard.Protector.Protections.ControlFlow.BlockParser;

namespace SugarGuard.Protector.Protections.ControlFlow
{
    public class SugarControlFlow
    {
        public SugarControlFlow(MethodDef method)
        {
            if (!method.HasBody || !method.Body.HasInstructions)
                return;

            if (method.ReturnType != null)
            {
                var body = method.Body;
                body.SimplifyBranches();

                ScopeBlock root = ParseBody(body);

                new SwitchMangler().Mangle(body, root, method, method.ReturnType);

                body.Instructions.Clear();
                root.ToBody(body);

                if (body.PdbMethod != null)
                {
                    body.PdbMethod = new PdbMethod()
                    {
                        Scope = new PdbScope()
                        {
                            Start = body.Instructions.First(),
                            End = body.Instructions.Last()
                        }
                    };
                }

                method.CustomDebugInfos.RemoveWhere(cdi => cdi is PdbStateMachineHoistedLocalScopesCustomDebugInfo);

                foreach (ExceptionHandler eh in body.ExceptionHandlers)
                {
                    var index = body.Instructions.IndexOf(eh.TryEnd) + 1;
                    eh.TryEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
                    index = body.Instructions.IndexOf(eh.HandlerEnd) + 1;
                    eh.HandlerEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
                }
            }
        }
    }
}