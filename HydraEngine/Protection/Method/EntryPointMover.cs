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
    public class EntryPointMover : Models.Protection
    {
        public EntryPointMover() : base("Protection.Method.EntryPointMover", "Renamer Phase", "Description for Renamer Phase") { }
       
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

             HydraEngine.Core.InjectHelper.MoveMethod(module.EntryPoint);

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
