using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.Services;
using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    internal class RPHelper
    {
        public RandomGenerator Random
        {
            get;
            set;
        }

        public NameService NameService
        {
            get;
            set;
        }

        public MethodDef GenerateMethod(TypeDef declaringType, object targetMethod, bool hasThis = false)
        {
            MemberRef methodReference = (MemberRef)targetMethod;
            MethodDef methodDefinition = new MethodDefUser(NameService.NewName(Random.NextHexString()), MethodSig.CreateStatic((methodReference).ReturnType), MethodAttributes.FamANDAssem | MethodAttributes.Public | MethodAttributes.Static);
            methodDefinition.Body = new CilBody();

            if (hasThis)
                methodDefinition.MethodSig.Params.Add(declaringType.Module.Import(declaringType.ToTypeSig()));

            foreach (TypeSig current in methodReference.MethodSig.Params)
                methodDefinition.MethodSig.Params.Add(current);

            methodDefinition.Parameters.UpdateParameterTypes();

            foreach (var current in methodDefinition.Parameters)
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, current));

            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, methodReference));

            return methodDefinition;
        }
    }
}
