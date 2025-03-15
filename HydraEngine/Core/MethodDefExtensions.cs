using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.Core
{
    public static class MethodDefExtensions
    {

        public static bool HasClosureReferences(this MethodDef method)
        {
            if (!method.HasBody)
                return false;

            foreach (var instr in method.Body.Instructions)
            {
                if (instr.Operand is IMethod mRef)
                {
                    var declType = mRef.DeclaringType;
                    if (declType != null)
                    {
                        if (declType.Name.Contains("<>") || declType.Name.Contains("DisplayClass"))
                            return true;
                    }
                }
                else if (instr.Operand is IField fRef)
                {
                    var declType = fRef.DeclaringType;
                    if (declType != null)
                    {
                        if (declType.Name.Contains("<>") || declType.Name.Contains("DisplayClass"))
                            return true;
                    }
                }
                else if (instr.Operand is ITypeDefOrRef tRef)
                {
                    if (tRef.Name.Contains("<>") || tRef.Name.Contains("DisplayClass"))
                        return true;
                }
            }
            return false;
        }

        public static MethodDef Clone(this MethodDef method, bool copyBody = true)
        {
            // Crear nueva instancia con mismo nombre y firma
            var cloned = new MethodDefUser(
                method.Name,
                method.MethodSig?.Clone(), // Clonar la firma del método
                method.ImplAttributes,
                method.Attributes);

            // Copiar parámetros
            if (method.ParamDefs.Count > 0)
            {
                foreach (var param in method.ParamDefs)
                {
                    cloned.ParamDefs.Add(new ParamDefUser(param.Name, param.Sequence, param.Attributes));
                }
            }

            // Copiar cuerpo del método
            if (copyBody && method.HasBody)
            {
                cloned.Body = CloneMethodBody(method.Body, cloned);
            }

            // Copiar atributos personalizados
            foreach (var ca in method.CustomAttributes)
            {
                cloned.CustomAttributes.Add(new CustomAttribute(ca.Constructor, ca.RawData));
            }

            // Copiar otras propiedades
            cloned.Access = method.Access;
            cloned.ImplMap = method.ImplMap != null ? CloneImplMap(method.ImplMap) : null;
            cloned.ReturnType = method.ReturnType;

            return cloned;
        }

        private static CilBody CloneMethodBody(CilBody original, MethodDef newMethod)
        {
            var newBody = new CilBody(original.InitLocals, new List<Instruction>(), original.ExceptionHandlers, original.Variables);

            // Clonar variables locales
            foreach (var variable in original.Variables)
            {
                newBody.Variables.Add(new Local(variable.Type));
            }

            // Clonar instrucciones
            var instrMap = new Dictionary<Instruction, Instruction>();
            foreach (var instr in original.Instructions)
            {
                var newInstr = new Instruction(instr.OpCode, instr.Operand)
                {
                    SequencePoint = instr.SequencePoint
                };
                instrMap[instr] = newInstr;
                newBody.Instructions.Add(newInstr);
            }

            // Actualizar operandos (branches, etc.)
            foreach (var instr in newBody.Instructions)
            {
                switch (instr.Operand)
                {
                    case Instruction target:
                        instr.Operand = instrMap[target];
                        break;
                    case IList<Instruction> targets:
                        instr.Operand = targets.Select(t => instrMap[t]).ToList();
                        break;
                }
            }

            // Clonar manejadores de excepciones
            foreach (var eh in original.ExceptionHandlers.ToArray())
            {
                newBody.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                {
                    TryStart = instrMap[eh.TryStart],
                    TryEnd = instrMap[eh.TryEnd],
                    HandlerStart = instrMap[eh.HandlerStart],
                    HandlerEnd = instrMap[eh.HandlerEnd],
                    CatchType = eh.CatchType,
                    FilterStart = eh.FilterStart != null ? instrMap[eh.FilterStart] : null
                });
            }

            return newBody;
        }

        private static ImplMap CloneImplMap(ImplMap original)
        {
            return new ImplMapUser(original.Module, original.Name, original.Attributes);
        }
    }
}
