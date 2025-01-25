using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.CodeOptimizer
{
	internal class AutoMethodBodySimplifyOptimize : IDisposable
	{
		public AutoMethodBodySimplifyOptimize(MethodDef methodBody, bool optimizeOnDispose)
		{
			_methodBody = methodBody.Body;
			_optimizeOnDispose = optimizeOnDispose;
			_methodBody.SimplifyMacros(methodBody.Parameters);
		}
		public void Dispose()
		{
			if (_optimizeOnDispose)
			{
				_methodBody.OptimizeMacros();
			}
		}
		private CilBody _methodBody;
		private bool _optimizeOnDispose;
	}
}
