using System.Collections.Generic;
using dnlib.DotNet;

namespace HydraEngine.Protection.CodeOptimizer
{
	internal class MethodReferenceEqualityComparer : IEqualityComparer<IMethod>
	{
		public static MethodReferenceEqualityComparer Singleton
		{
			get
			{
				return MethodReferenceEqualityComparer._singleton;
			}
		}
		public bool Equals(IMethod mrefA, IMethod mrefB)
		{
			return mrefA.Equals(mrefB);
		}
		public int GetHashCode(IMethod mref)
		{
			return mref.FullName.GetHashCode();
		}
		private static MethodReferenceEqualityComparer _singleton = new MethodReferenceEqualityComparer();
	}
}
