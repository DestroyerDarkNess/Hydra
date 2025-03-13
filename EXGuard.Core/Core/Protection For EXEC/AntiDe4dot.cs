using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.Services;
using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    public static class AntiDe4dot_Inject
    {
        public static void Execute(ModuleDef module)
        {
			var random = new RandomGenerator();

			InterfaceImpl interfaceM = new InterfaceImplUser(module.GlobalType);

			TypeDef typeDef1 = new TypeDefUser(string.Empty, new NameService().NewName(random.NextString()), module.CorLibTypes.GetTypeRef("System", "Attribute"));
			InterfaceImpl interface1 = new InterfaceImplUser(typeDef1);
			module.Types.Add(typeDef1);
			typeDef1.Interfaces.Add(interface1);
			typeDef1.Interfaces.Add(interfaceM);

			for (int i = 0; i < random.NextInt32(4, 15); i++)
			{
				TypeDef typeDef2 = new TypeDefUser(string.Empty, new NameService().NewName(random.NextString()), module.CorLibTypes.GetTypeRef("System", "Attribute"));
				InterfaceImpl interface2 = new InterfaceImplUser(typeDef2);
				module.Types.Add(typeDef2);
				typeDef2.Interfaces.Add(interface2);
				typeDef2.Interfaces.Add(interfaceM);
				typeDef2.Interfaces.Add(interface1);
			}
		}
    }
}
