using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EXGuard.Core.RTProtections.Constants {
	public class InstructionReference {
		/// <summary>
		/// Represents the method in which the instruction lies.
		/// </summary>
		public MethodDef Method {
			get;
			set;
		}

		/// <summary>
		/// The actual encodeable instruction.
		/// </summary>
		public Instruction Instruction {
			get;
			set;
		}
	}
}
