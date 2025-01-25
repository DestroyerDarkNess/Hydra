using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Protection.Mutations.Stages;
using HydraEngine.Core;

namespace HydraEngine.Protection.Mutations
{
    public class Mutatorv2 : Models.Protection
    {
        public Mutatorv2() : base("Protection.Mutations.Mutatorv2", "Renamer Phase", "Description for Renamer Phase") { }

        public bool UnsafeMutation { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {
                foreach (TypeDef typeDef in md.Types)
                {
                    if (!Analyzer. CanRename(typeDef)) continue;
                    foreach (MethodDef method in typeDef.Methods)
                    {
                        if (!Analyzer.CanRename(method)) continue;
                        if (method.Body == null) continue;
                        ApplyMutations(method);

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

        //private bool CanRename(MethodDef method)
        //{
        //    return !method.IsConstructor &&
        //           !method.DeclaringType.IsForwarder &&
        //           !method.IsFamily &&
        //           !method.IsStaticConstructor &&
        //           !method.IsRuntimeSpecialName &&
        //           !method.DeclaringType.IsGlobalModuleType &&
        //           !method.Name.Contains("Hydra");
        //}

        //private static bool CanRename(TypeDef type)
        //{
        //    if (type.Namespace.Contains("My")) return false;
        //    return !type.IsGlobalModuleType &&
        //           type.Interfaces.Count == 0 &&
        //           !type.IsSpecialName &&
        //           !type.IsRuntimeSpecialName &&
        //           !type.Name.Contains("Hydra");
        //}


        private bool CanMutateMethod(MethodDef method)
        {
            return method.HasBody && method.Body.HasInstructions;
        }

        private void ApplyMutations(MethodDef methodDef)
        {
           

            if (UnsafeMutation == true)
            {
                new MethodPreparation(methodDef).Execute();
                var intsToInliner = new IntsToInliner(methodDef);
            }

          
            var intsToArray = new IntsToArray(methodDef);
            var intsToStackalloc = new IntsToStackalloc(methodDef);
            var intsToMath = new IntsToMath(methodDef);
            var localsToCustomLocal = new LocalsToCustomLocal(methodDef);
            var intsToRandom = new IntsToRandom(methodDef);

            for (int i = 0; i < methodDef.Body.Instructions.Count; i++)
            {
                if (methodDef.Body.Instructions[i].IsLdcI4() && MutationHelper.CanObfuscate(methodDef.Body.Instructions, i))
                {
                    ApplyRandomMutation(intsToMath, localsToCustomLocal, intsToRandom, intsToStackalloc, ref i);
                }
            }

           
         

        }

        private Random rnd = new Random();
        private void ApplyRandomMutation(
            IntsToMath intsToMath,
            LocalsToCustomLocal localsToCustomLocal,
            IntsToRandom intsToRandom,
            IntsToStackalloc intsToStackalloc,
            ref int index)
        {
            switch (rnd.Next(0, 5))
            {
                case 1:
                    intsToMath.Execute(ref index);
                    break;
                case 2:
                    if (UnsafeMutation == true) { 
                        localsToCustomLocal.Execute(ref index);
                    }
                        break;
                case 3:
                    intsToRandom.Execute(ref index);
                    break;
                case 4:
                    if (UnsafeMutation == true)
                    {
                        intsToStackalloc.Execute(ref index);
                    }
                    break;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
