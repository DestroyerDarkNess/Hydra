using System.Collections.Generic;

using dnlib.DotNet;

namespace EXGuard.Core
{
	/// <summary>
	/// Replaces generic type/method var with its generic argument
	/// </summary>
	public sealed class GenericArguments
	{
		GenericArgumentsStack typeArgsStack = new GenericArgumentsStack(true);
		GenericArgumentsStack methodArgsStack = new GenericArgumentsStack(false);

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="typeArgs">The generic arguments</param>
		public void PushTypeArgs(IList<TypeSig> typeArgs)
		{
			typeArgsStack.Push(typeArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopTypeArgs()
		{
			return typeArgsStack.Pop();
		}

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="methodArgs">The generic arguments</param>
		public void PushMethodArgs(IList<TypeSig> methodArgs)
		{
			methodArgsStack.Push(methodArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopMethodArgs()
		{
			return methodArgsStack.Pop();
		}

		/// <summary>
		/// Replaces a generic type/method var with its generic argument (if any). If
		/// <paramref name="typeSig"/> isn't a generic type/method var or if it can't
		/// be resolved, it itself is returned. Else the resolved type is returned.
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns>New <see cref="TypeSig"/> which is never <c>null</c> unless
		/// <paramref name="typeSig"/> is <c>null</c></returns>
		public TypeSig Resolve(TypeSig typeSig)
		{
			if (typeSig == null)
				return null;

			var sig = typeSig;

			var genericMVar = sig as GenericMVar;
			if (genericMVar != null)
			{
				var newSig = methodArgsStack.Resolve(genericMVar.Number);
				if (newSig == null || newSig == sig)
					return sig;
				return newSig;
			}

			var genericVar = sig as GenericVar;
			if (genericVar != null)
			{
				var newSig = typeArgsStack.Resolve(genericVar.Number);
				if (newSig == null || newSig == sig)
					return sig;
				return newSig;
			}

			return sig;
		}
	}
}
