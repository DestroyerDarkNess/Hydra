using System.Collections.Generic;

using dnlib.DotNet;

namespace EXGuard.Core
{
    struct GenericArgumentsStack
    {
        readonly List<IList<TypeSig>> argsStack;
        readonly bool isTypeVar;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isTypeVar"><c>true</c> if it's for generic types, <c>false</c> if generic methods</param>
        public GenericArgumentsStack(bool isTypeVar)
        {
            this.argsStack = new List<IList<TypeSig>>();
            this.isTypeVar = isTypeVar;
        }

        /// <summary>
        /// Pushes generic arguments
        /// </summary>
        /// <param name="args">The generic arguments</param>
        public void Push(IList<TypeSig> args)
        {
            argsStack.Add(args);
        }

        /// <summary>
        /// Pops generic arguments
        /// </summary>
        /// <returns>The popped generic arguments</returns>
        public IList<TypeSig> Pop()
        {
            int index = argsStack.Count - 1;
            var result = argsStack[index];
            argsStack.RemoveAt(index);
            return result;
        }

        /// <summary>
        /// Resolves a generic argument
        /// </summary>
        /// <param name="number">Generic variable number</param>
        /// <returns>A <see cref="TypeSig"/> or <c>null</c> if none was found</returns>
        public TypeSig Resolve(uint number)
        {
            TypeSig result = null;
            for (int i = argsStack.Count - 1; i >= 0; i--)
            {
                var args = argsStack[i];
                if (number >= args.Count)
                    return null;
                var typeSig = args[(int)number];
                var gvar = typeSig as GenericSig;
                if (gvar == null || gvar.IsTypeVar != isTypeVar)
                    return typeSig;
                result = gvar;
                number = gvar.Number;
            }
            return result;
        }
    }
}
