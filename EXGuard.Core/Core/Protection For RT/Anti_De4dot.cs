using System;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EXGuard.Core.RTProtections
{
	public class Anti_De4dot
	{
		private static ModuleDef publicmodule;

		private static void confuserex(string message)
		{
            TypeRef attrRef = publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
            var attrType = new TypeDefUser(string.Empty, "ConfusedByAttribute", attrRef);
            publicmodule.Types.Add(attrType);

            var ctor = new MethodDefUser(
                ".ctor",
                MethodSig.CreateInstance(publicmodule.CorLibTypes.Void, publicmodule.CorLibTypes.String),
                MethodImplAttributes.Managed,
                MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ctor.Body = new CilBody();
            ctor.Body.MaxStack = 1;
            ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(publicmodule, ".ctor", MethodSig.CreateInstance(publicmodule.CorLibTypes.Void), attrRef)));
            ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            attrType.Methods.Add(ctor);

            var attr = new CustomAttribute(ctor);
            attr.ConstructorArguments.Add(new CAArgument(publicmodule.CorLibTypes.String, message));

            publicmodule.CustomAttributes.Add(attr);
        }

		private static void babel(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "BabelObfuscatorAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void dotfuscator(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "DotfuscatorAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void ninerays(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "NineRays.Obfuscator.Evaluation", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void mango(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "();\t", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void bithelmet(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "EMyPID_8234_", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void crypto(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void yano(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "YanoAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void dnguard(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "ZYXDNGuarder", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void goliath(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "ObfuscatedByGoliath", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void agile(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "SecureTeam.Attributes.ObfuscatedByAgileDotNetAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}

		private static void smartassembly(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "SmartAssembly.Attributes.PoweredByAttribute", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}
		private static void xenocode(string message)
		{
			TypeRef typeRef = Anti_De4dot.publicmodule.CorLibTypes.GetTypeRef("System", "Attribute");
			TypeDefUser typeDefUser = new TypeDefUser(string.Empty, "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode", typeRef);
			Anti_De4dot.publicmodule.Types.Add(typeDefUser);
			MethodDefUser methodDefUser = new MethodDefUser(".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void, Anti_De4dot.publicmodule.CorLibTypes.String), MethodImplAttributes.IL, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			methodDefUser.Body = new CilBody();
			methodDefUser.Body.MaxStack = 1;
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(Anti_De4dot.publicmodule, ".ctor", MethodSig.CreateInstance(Anti_De4dot.publicmodule.CorLibTypes.Void), typeRef)));
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			typeDefUser.Methods.Add(methodDefUser);
		}
		
		public static void Execute(ModuleDef md, string message)
		{
			Anti_De4dot.publicmodule = md;
			
			Anti_De4dot.xenocode(message);
			Anti_De4dot.smartassembly(message);
			Anti_De4dot.agile(message);
			Anti_De4dot.goliath(message);
			Anti_De4dot.yano(message);
			Anti_De4dot.crypto(message);
			Anti_De4dot.confuserex(message);
			Anti_De4dot.babel(message);
			Anti_De4dot.dotfuscator(message);
			Anti_De4dot.ninerays(message);
			Anti_De4dot.bithelmet(message);
			Anti_De4dot.mango(message);
			Anti_De4dot.dnguard(message);
		}
	}
}
