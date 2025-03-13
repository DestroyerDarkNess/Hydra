using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT;
using EXGuard.Core.JIT;
using EXGuard.Core.VMIL;
using EXGuard.Core.Services;
using EXGuard.Core.Helpers;
using EXGuard.Core.RT.Mutation;

using EXGuard.Runtime;

namespace EXGuard.Core
{
    public class Virtualizer : IVMSettings {
        ModuleDef EXECModule;
        MethodVirtualizer MDVirtualizer;

		HashSet<MethodDef> methodList = new HashSet<MethodDef>();
		HashSet<ModuleDef> processed = new HashSet<ModuleDef>();

        public VMRuntime Runtime
		{
			get;
			private set;
		}

        public Virtualizer(ModuleDef module, string newRtName)
        {
            var RuntimeModule = ModuleDefMD.Load(typeof(VMEntry).Module);
            
            RuntimeModule.Assembly.Name = newRtName;
            RuntimeModule.Name = string.Empty;

            if (Path.GetExtension(newRtName) == ".dll")
                RuntimeModule.Assembly.Name = Path.GetFileNameWithoutExtension(newRtName);

            RuntimeModule.AssemblyReferencesAdder();
            module.AssemblyReferencesAdder();

            EXECModule = module;
            Runtime = new VMRuntime(this, RuntimeModule);
            MDVirtualizer = new MethodVirtualizer(Runtime);

            #region Reset MutationHelper
            //////////////////////////////////////////////////////////////////////////////////////////////
            MutationHelper.Field2IntIndex = MutationHelper.Original_Field2IntIndex;
            MutationHelper.Field2LongIndex = MutationHelper.Original_Field2LongIndex;
            MutationHelper.Field2ULongIndex = MutationHelper.Original_Field2ULongIndex;
            MutationHelper.Field2LdstrIndex = MutationHelper.Original_Field2LdstrIndex;

            RTMap.Mutation = "Mutation";
            RTMap.Mutation_Placeholder = "Placeholder";
            RTMap.Mutation_LocationIndex = "LocationIndex";
            RTMap.Mutation_Value_T = "Value";
            RTMap.Mutation_Value_T_Arg0 = "Value";
            RTMap.Mutation_Crypt = "Crypt";
            //////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region Set First VMSettings
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            Runtime.RTSearch = new RuntimeSearch(Runtime.RTModule, Runtime).Search(); // Search RTMap Methods
           
			Runtime.RTMutator.MutateRuntime(); // Mutate Runtime
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion
        }

        public void AddMethod(MethodDef method) {
			if (!method.HasBody)
				return;

            if (method.HasGenericParameters)
                return;

            methodList.Add(method);
        }

        public void JIT(ModuleDef module, ModuleWriterOptions options, out JITContext ctx)
        {
            ctx = new JITContext();
            var targets = new HashSet<MethodDef>();

            #region Search Virtualized Methods
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < module.Types.Count; i++)
            {
                for (int c = 0; c < module.Types[i].Methods.Count; c++)
                {
                    var methodDef = module.Types[i].Methods[c];

                    if (GetMethods_FullNames().Contains(methodDef.FullName)) // Get Virted Method
                    {
                        targets.Add(methodDef);
                    }
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            ctx.Runtime = Runtime;
            ctx.Targets = targets;

            var antitamper = new JITWriter(ctx, module);
            antitamper.HandleRun(options);
        }

        public IEnumerable<MethodDef> GetMethods() {
			return methodList;
		}

        public void ResolveMethods()
        {
            var list = new HashSet<MethodDef>();

            #region Search Virtualized Methods
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < EXECModule.Types.Count; i++)
            {
                for (int c = 0; c < EXECModule.Types[i].Methods.Count; c++)
                {
                    var methodDef = EXECModule.Types[i].Methods[c];

                    if (GetMethods_FullNames().Contains(methodDef.FullName)) // Get Virted Method
                        list.Add(methodDef);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            methodList = list;
        }

        public IEnumerable<string> GetMethods_FullNames()
        {
            var fnlist = new HashSet<string>();
            foreach (var method in methodList)
                fnlist.Add(method.FullName);

            return fnlist;
        }

        public void ProcessMethods(ModuleWriterBase writer) {
			if (processed.Contains(EXECModule))
				throw new InvalidOperationException("Module already processed.");

			var targets = methodList.Where(method => method.Module == EXECModule).ToList();
            
            for (int i = 0; i < targets.Count; i++) {
				var method = targets[i];
                ProcessMethod(method, writer.Metadata.GetToken(method));
            }

			processed.Add(EXECModule);
		}

        public void CommitModule(Metadata rtmtd)
        {
            var methods = methodList.Where(method => method.Module == EXECModule).ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                PostProcessMethod(method);
            }

            Runtime.RTMutator.CommitModule(EXECModule, rtmtd);
        }

		void ProcessMethod(MethodDef method, MDToken mdToken) {
			MDVirtualizer.Run(method, mdToken);
        }

		void PostProcessMethod(MethodDef method) {
			var scope = Runtime.LookupMethod(method);

            var ilTransformer = new ILPostTransformer(method, scope, Runtime);
			ilTransformer.Transform();
        }

		bool IVMSettings.IsVirtualized(MethodDef method) {
			return methodList.Contains(method);
		}

        public void Clear()
        {
            Runtime.RTSearch.Dispose();
            Runtime.ResetData();

            processed.Clear();
            methodList.Clear();

            EXECModule = null;
            MDVirtualizer = null;
        }
	}
}