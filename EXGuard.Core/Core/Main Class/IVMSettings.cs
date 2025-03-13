using dnlib.DotNet;

namespace EXGuard.Core
{
	public interface IVMSettings {
		bool IsVirtualized(MethodDef method);
	}
}