using dnlib.DotNet;

using EXGuard.Core.RT;
using EXGuard.Core.CFG;
using EXGuard.Core.VMIL;
using EXGuard.Core.VMIR;
using EXGuard.Core.ILAST;

namespace EXGuard.Core
{
	public class MethodVirtualizer {
		public MethodVirtualizer(VMRuntime runtime) {
			Runtime = runtime;
		}

		protected VMRuntime Runtime { get; private set; }
		protected MethodDef Method { get; private set; }
		protected ScopeBlock RootScope { get; private set; }
		protected IRContext IRContext { get; private set; }

		public ScopeBlock Run(MethodDef method, MDToken mdToken) {
			try {
				Method = method;

				Init();
				BuildILAST();
				TransformILAST();
				BuildVMIR();
				TransformVMIR();
				BuildVMIL();
				TransformVMIL();
				Deinitialize(mdToken);

				var scope = RootScope;
				RootScope = null;
				Method = null;

				return scope;
			}
			catch {
                var scope = RootScope;
                RootScope = null;
                Method = null;

                return scope;
            }
		}

		protected virtual void Init() {
			RootScope = BlockParser.Parse(Method, Method.Body);
			IRContext = new IRContext(Method, Method.Body);
		}

		protected virtual void BuildILAST() {
			ILASTBuilder.BuildAST(Method, Method.Body, RootScope);
		}

		protected virtual void TransformILAST() {
			var transformer = new ILASTTransformer(Method, RootScope, Runtime);
			transformer.Transform();
		}

		protected virtual void BuildVMIR() {
			var translator = new IRTranslator(IRContext, Runtime);
			translator.Translate(RootScope);
		}

		protected virtual void TransformVMIR() {
			var transformer = new IRTransformer(RootScope, IRContext, Runtime);
			transformer.Transform();
		}

		protected virtual void BuildVMIL() {
			var translator = new ILTranslator(Runtime);
			translator.Translate(RootScope);
		}

		protected virtual void TransformVMIL() {
			var transformer = new ILTransformer(Method, RootScope, Runtime);
			transformer.Transform();
		}

		protected virtual void Deinitialize(MDToken mdToken) {
			IRContext = null;

			Runtime.AddMethod(Method, RootScope);
			Runtime.ExportMethod(Method, mdToken);
		}
	}
}