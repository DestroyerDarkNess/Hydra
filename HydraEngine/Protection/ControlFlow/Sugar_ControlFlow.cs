using dnlib.DotNet;
using SugarGuard.Protector.Protections.ControlFlow;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.ControlFlow
{
    public class Sugar_ControlFlow : Models.Protection
    {
        public Sugar_ControlFlow() : base("Protection.CtrlFlow.Sugar_ControlFlow", "Renamer Phase", "Description for Renamer Phase") { }

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

                    foreach (var method in mtt.Methods.ToArray())
                    {
                        if (method.IsConstructor) continue;
                        if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType) continue;

                        if (method.HasGenericParameters) continue;
                        if (method.IsPinvokeImpl) continue;
                        if (method.IsUnmanagedExport) continue;

                        SugarControlFlow.Execute(method);
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

    }
}
