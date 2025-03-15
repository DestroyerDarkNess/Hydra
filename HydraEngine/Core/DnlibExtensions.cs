using dnlib.DotNet;

namespace HydraEngine.Core
{
    public static class DnlibExtensions
    {
        public static bool HasByRefParameters(this MethodDef method)
        {
            if (method.Parameters.Count == 0)
                return false;

            foreach (var param in method.Parameters)
            {
                var paramType = param.Type.RemoveModifiers();
                if (paramType.IsByRef)
                    return true;
            }
            return false;
        }

        private static TypeSig RemoveModifiers(this TypeSig type)
        {
            while (type is ModifierSig || type is PinnedSig)
            {
                type = type.Next;
            }
            return type;
        }
    }
}
