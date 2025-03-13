using dnlib.DotNet;

namespace EXGuard.Core.RTProtections.Constants
{
	public class DecoderInfo {
		public MethodDef Method {
			get;
			set;
		}

		public DecoderDesc DecoderDesc {
			get;
			set;
		}
	}
}
