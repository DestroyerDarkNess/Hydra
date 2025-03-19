﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Renamer;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public static class AntiInvokeRuntime
    {
        public static void Initialize()
        {
            if (Assembly.GetExecutingAssembly() != Assembly.GetCallingAssembly())
            {
                Process.GetCurrentProcess().Kill();
                Environment.FailFast(null);
            }
        }

    }

    public class AntiInvoke : Models.Protection
    {
        public AntiInvoke() : base("Runtimes.Anti.AntiInvoke", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(InvokeDetector).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(InvokeDetector).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Initialize");
                foreach (Instruction Instruction in init.Body.Instructions.Where((Instruction I) => I.OpCode == OpCodes.Ldstr))
                {
                    if (Instruction.Operand.ToString() == "message")
                        Instruction.Operand = this.ExitMethod;
                }
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                foreach (var md in module.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    module.GlobalType.Remove(md);
                    break;
                }

                ForAll(module);

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }

        }

        private void ForAll(ModuleDefMD module)
        {
            ModuleDefMD moduleDefMD = ModuleDefMD.Load(typeof(AntiInvokeRuntime).Module);
            TypeDef typeDef = moduleDefMD.ResolveTypeDef(MDToken.ToRID(typeof(AntiInvokeRuntime).MetadataToken));

            foreach (TypeDef type in module.Types.ToArray())
            {
                if (!AnalyzerPhase.CanRename(type)) continue;

                IEnumerable<IDnlibDef> source = InjectHelper.Inject(typeDef, type, module);
                MethodDef cctor = module.GlobalType.FindOrCreateStaticConstructor();
                MethodDef init = (MethodDef)source.Single((IDnlibDef method) => method.Name == "Initialize");
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

                MethodDef type_cctor = type.FindOrCreateStaticConstructor();
                MethodDef type_init = (MethodDef)source.Single((IDnlibDef method) => method.Name == "Initialize");
                type_cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, type_init));

                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                //bool Dynamic = new IL2Dynamic().ConvertToDynamic(init, module);
            }

        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
