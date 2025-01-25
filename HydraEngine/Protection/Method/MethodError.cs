using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
     public class MethodError : Models.Protection
    {
        public MethodError() : base("Protection.Method.MethodError", "Renamer Phase", "Description for Renamer Phase") { this.CompatibleWithMap = false; }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                foreach (TypeDef type in Module.Types.Where(x => x.HasMethods))
                {
                    if (!Analyzer.CanRename(type)) continue;
                    foreach (MethodDef method in type.Methods.Where(x => x.HasBody))
                    {
                        if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType)  continue;
                        Hide(method);
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

        public  void Hide(MethodDef method)
        {
            if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType)
            {
                method.Body.Instructions.Insert(1, new Instruction(OpCodes.Br_S, method.Body.Instructions[1]));
                method.Body.Instructions.Insert(2, new Instruction(OpCodes.Unaligned, 0));
            }
        }
       
    }
}
