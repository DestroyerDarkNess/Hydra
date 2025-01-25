using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CodeOptimizer
{
    internal static class Extensions
    {
        public static IEnumerable<IDnlibDef> FindDefinitions(this ModuleDef module)
        {
            yield return module;
            foreach (TypeDef type in module.GetTypes())
            {
                yield return type;
                foreach (MethodDef method in type.Methods)
                {
                    yield return method;
                }
                IEnumerator<MethodDef> enumerator2 = null;
                foreach (FieldDef field in type.Fields)
                {
                    yield return field;
                }
                IEnumerator<FieldDef> enumerator3 = null;
                foreach (PropertyDef prop in type.Properties)
                {
                    yield return prop;
                }
                IEnumerator<PropertyDef> enumerator4 = null;
                foreach (EventDef evt in type.Events)
                {
                    yield return evt;
                }
                IEnumerator<EventDef> enumerator5 = null;
                //type = null;
            }
            IEnumerator<TypeDef> enumerator = null;
            yield break;
            yield break;
        }

        public static Instruction CreateLoadInstructionInsteadOfLoadAddress(this Instruction instruction, Instruction _ilProcessor)
        {
            Instruction result = null;
            if (instruction.OpCode == OpCodes.Ldloca)
            {
                result = new Instruction(OpCodes.Ldloc, (Local)instruction.Operand);
            }
            else if (instruction.OpCode == OpCodes.Ldarga)
            {
                result = new Instruction(OpCodes.Ldloc, (Local)instruction.Operand);
            }
            return result;
        }
        public static bool IsTypePublic(this TypeDef type)
        {
            while (type.IsPublic || type.IsNestedFamily || type.IsNestedFamilyAndAssembly || type.IsNestedFamilyOrAssembly || type.IsNestedPublic || type.IsPublic)
            {
                type = type.DeclaringType;
                if (type == null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
