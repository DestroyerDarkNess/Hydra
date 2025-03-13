using System;
using System.Collections.Generic;

namespace EXGuard.Core.Helpers.System.Runtime.CompilerServices
{
	[CLSCompliant(false)]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]

#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public sealed class TupleElementNamesAttribute : Attribute
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
		public TupleElementNamesAttribute(string[] transformNames)
		{
			if (transformNames == null)
			{
				throw new ArgumentNullException("transformNames");
			}
			this._transformNames = transformNames;
		}

		public IList<string> TransformNames
		{
			get
			{
				return this._transformNames;
			}
		}

		private readonly string[] _transformNames;
	}
}