using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    public static class AntiILDasm_Inject
    {
        public static void Execute(ModuleDef module)
        {
			TypeRef SuppressIldasmAttribute = module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");
			MemberRefUser SuppressIldasmAttribute_ctor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), SuppressIldasmAttribute);
			CustomAttribute SuppressIldasmAttribute_item = new CustomAttribute(SuppressIldasmAttribute_ctor);
			module.Assembly.CustomAttributes.Add(SuppressIldasmAttribute_item);
			module.CustomAttributes.Add(SuppressIldasmAttribute_item);
		}
    }
}
