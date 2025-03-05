using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using OpCode = dnlib.DotNet.Emit.OpCode;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using OperandType = dnlib.DotNet.Emit.OperandType;
using ROpCode = System.Reflection.Emit.OpCode;
using ROpCodes = System.Reflection.Emit.OpCodes;

namespace HydraEngine.Protection.Method
{
    public class IL2Dynamic : Models.Protection
    {
        public IL2Dynamic() : base("Protection.Method.IL2Dynamic", "Dynamic Cctor", "Description for Renamer Phase") { }

        public string tag { get; set; } = "HailHydra";
        public int Length { get; set; } = 20;
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private string[] m_reservedWords =  { "addhandler", "addressof", "alias", "and", "andalso", "ansi", "append", "as",
            "assembly", "auto", "binary", "boolean", "byref", "byte", "byval", "call", "case", "catch", "cbool", "cbyte",
            "cchar", "cdate", "cdec", "cdbl", "char", "cint", "class", "clng", "cobj", "compare", "const", "continue",
            "cbyte", "cshort", "csng", "cstr", "ctype", "cuint", "culng", "cushort", "date", "decimal", "declare", "default",
            "delegate", "dim", "directcast", "do", "double", "each", "else", "elseif", "end", "endif", "enum", "erase", "error",
            "event", "explicit", "exit", "false", "finally", "for", "friend", "function", "get", "gettype", "global", "gosub",
            "goto", "handles", "if", "implements", "imports", "in", "inherits", "input", "integer", "interface", "internal", "is",
            "isnot", "let", "lib", "like", "lock", "long", "loop", "me", "mid", "mod", "module", "notinheritable", "mustinherit",
            "mustoverride", "my", "mybase", "myclass", "namespace", "narrowing", "new", "next", "not", "nothing", "notinheritable",
            "notoverridable", "object", "of", "off", "on", "operator", "option", "optional", "or", "orelse", "output", "overloads",
            "overridable", "overrides", "paramarray", "preserve", "partial", "private", "property", "protected", "public", "raiseevent",
            "random", "read", "readonly", "redim", "rem", "removehandler", "resume", "return", "sbyte", "seek", "select", "set", "shadows",
            "shared", "short", "single", "static", "step", "stop", "strict", "string", "structure", "sub", "synclock", "then", "throw", "to",
            "true", "try", "trycast", "typeof", "variant", "wend", "uinteger", "ulong", "until", "ushort", "using", "void", "when", "while",
            "widening", "with", "withevents", "writeonly", "xor", "region"};

        public RenameMode Mode { get; set; } = RenameMode.Ascii;

        private Dictionary<string, string> Names = new Dictionary<string, string>();

        private Random Random = new Random();


        public enum RenameMode
        {
            Ascii,
            Key,
            Normal,
            Invisible
        }

        private static readonly string[] NormalNameStrings =
        {
        "HasPermission", "HasPermissions", "GetPermissions", "GetOpenWindows", "EnumWindows", "GetWindowText",
        "GetWindowTextLength", "IsWindowVisible", "GetShellWindow", "Awake", "FixedUpdate", "add_OnRockedInitialized",
        "remove_OnRockedInitialized", "Awake", "Initialize", "Translate", "Reload", "<Initialize>b__13_0", "Initialize",
        "FixedUpdate", "Start", "checkTimerRestart", "QueueOnMainThread", "QueueOnMainThread", "RunAsync", "RunAction",
        "Awake", "FixedUpdate", "IsUri", "GetTypes", "GetTypesFromParentClass", "GetTypesFromParentClass",
        "GetTypesFromInterface", "GetTypesFromInterface", "get_Timeout", "set_Timeout", "GetWebRequest",
        "get_SteamID64", "set_SteamID64", "get_SteamID", "set_SteamID", "get_OnlineState", "set_OnlineState",
        "get_StateMessage", "set_StateMessage", "get_PrivacyState", "set_PrivacyState", "get_VisibilityState",
        "set_VisibilityState", "get_AvatarIcon", "set_AvatarIcon", "get_AvatarMedium", "set_AvatarMedium",
        "get_AvatarFull", "set_AvatarFull", "get_IsVacBanned", "set_IsVacBanned", "get_TradeBanState",
        "set_TradeBanState", "get_IsLimitedAccount", "set_IsLimitedAccount", "get_CustomURL", "set_CustomURL",
        "get_MemberSince", "set_MemberSince", "get_HoursPlayedLastTwoWeeks", "set_HoursPlayedLastTwoWeeks",
        "get_Headline", "set_Headline", "get_Location", "set_Location", "get_RealName", "set_RealName", "get_Summary",
        "set_Summary", "get_MostPlayedGames", "set_MostPlayedGames", "get_Groups", "set_Groups", "Reload",
        "ParseString", "ParseDateTime", "ParseDouble", "ParseUInt16", "ParseUInt32", "ParseUInt64", "ParseBool",
        "ParseUri", "IsValidCSteamID", "LoadDefaults", "LoadDefaults", "get_Clients", "Awake", "handleConnection",
        "FixedUpdate", "Broadcast", "OnDestroy", "Read", "Send", "<Awake>b__8_0", "get_InstanceID", "set_InstanceID",
        "get_ConnectedTime", "set_ConnectedTime", "Send", "Read", "Close", "get_Address", "get_Instance",
        "set_Instance", "Save", "Load", "Unload", "Load", "Save", "Load", "get_Configuration", "LoadPlugin",
        "<.ctor>b__3_0", "<LoadPlugin>b__4_0", "add_OnPluginUnloading", "remove_OnPluginUnloading",
        "add_OnPluginLoading", "remove_OnPluginLoading", "get_Translations", "get_State", "get_Assembly",
        "set_Assembly", "get_Directory", "set_Directory", "get_Name", "set_Name", "get_DefaultTranslations",
        "IsDependencyLoaded", "ExecuteDependencyCode", "Translate", "ReloadPlugin", "LoadPlugin", "UnloadPlugin",
        "OnEnable", "OnDisable", "Load", "Unload", "TryAddComponent", "TryRemoveComponent", "add_OnPluginsLoaded",
        "remove_OnPluginsLoaded", "get_Plugins", "GetPlugins", "GetPlugin", "GetPlugin", "Awake", "Start",
        "GetMainTypeFromAssembly", "loadPlugins", "unloadPlugins", "Reload", "GetAssembliesFromDirectory",
        "LoadAssembliesFromDirectory", "<Awake>b__12_0", "GetGroupsByIds", "GetParentGroups", "HasPermission",
        "GetGroup", "RemovePlayerFromGroup", "AddPlayerToGroup", "DeleteGroup", "SaveGroup", "AddGroup", "GetGroups",
        "GetPermissions", "GetPermissions", "<GetGroups>b__11_3", "Start", "FixedUpdate", "Reload", "HasPermission",
        "GetGroups", "GetPermissions", "GetPermissions", "AddPlayerToGroup", "RemovePlayerFromGroup", "GetGroup",
        "SaveGroup", "AddGroup", "DeleteGroup", "DeleteGroup", "<FixedUpdate>b__4_0", "Enqueue", "_Logger_DoWork",
        "processLog", "Log", "Log", "var_dump", "LogWarning", "LogError", "LogError", "Log", "LogException",
        "ProcessInternalLog", "logRCON", "writeToConsole", "ProcessLog", "ExternalLog", "Invoke", "_invoke",
        "TryInvoke", "get_Aliases", "get_AllowedCaller", "get_Help", "get_Name", "get_Permissions", "get_Syntax",
        "Execute", "get_Aliases", "get_AllowedCaller", "get_Help", "get_Name", "get_Permissions", "get_Syntax",
        "Execute", "get_Aliases", "get_AllowedCaller", "get_Help", "get_Name", "get_Permissions", "get_Syntax",
        "Execute", "get_Name", "set_Name", "get_Name", "set_Name", "get_Name", "get_Help", "get_Syntax",
        "get_AllowedCaller", "get_Commands", "set_Commands", "add_OnExecuteCommand", "remove_OnExecuteCommand",
        "Reload", "Awake", "checkCommandMappings", "checkDuplicateCommandMappings", "Plugins_OnPluginsLoaded",
        "GetCommand", "GetCommand", "getCommandIdentity", "getCommandType", "Register", "Register", "Register",
        "DeregisterFromAssembly", "GetCooldown", "SetCooldown", "Execute", "RegisterFromAssembly"
    };

        private static readonly char[] InvisibleChars = new char[]
   {
        '\u200B', // Zero Width Space
        '\u200C', // Zero Width Non-Joiner
        '\u200D', // Zero Width Joiner
        '\u2060', // Word Joiner
        '\uFEFF'  // Zero Width No-Break Space
   };

        private string GenerateInvisibleString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = InvisibleChars[Random.Next(InvisibleChars.Length)];
            }
            return new string(result);
        }

        private string RandomString(int length, string chars)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private string GetRandomName()
        {
            return NormalNameStrings[Random.Next(NormalNameStrings.Length)];
        }

        private string GetRandomReservedWords()
        {
            return m_reservedWords[Random.Next(m_reservedWords.Length)];
        }

        public static readonly char[] ProblematicChars = new char[]
  {
        '\u202A', // LEFT-TO-RIGHT EMBEDDING
        '\u202B', // RIGHT-TO-LEFT EMBEDDING
        '\u202C', // POP DIRECTIONAL FORMATTING
        '\u202D', // LEFT-TO-RIGHT OVERRIDE
        '\u202E'  // RIGHT-TO-LEFT OVERRIDE
  };

        private string GenerateProblematicString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ProblematicChars[Random.Next(ProblematicChars.Length)];
            }
            return new string(result);
        }

        public string GenerateString(RenameMode mode)
        {
            switch (mode)
            {
                case RenameMode.Ascii:
                    return RandomString(Random.Next(3, Length), BaseChars);
                case RenameMode.Key:
                    return GenerateProblematicString(Random.Next(3, Length)) + GetRandomReservedWords();// RandomString(16, BaseChars);
                case RenameMode.Normal:
                    return GetRandomName();
                case RenameMode.Invisible:
                    return GenerateInvisibleString(Random.Next(3, Length));
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                foreach (var t in Module.Types)
                {
                    foreach (var method in t.Methods)
                    {
                        if (method.DeclaringType.IsGlobalModuleType) continue;

                        if (!method.HasBody) continue;

                        if (!method.Body.HasInstructions) continue;

                        CtorCallProtection(method);

                        if (method == Module.GlobalType.FindOrCreateStaticConstructor())
                        {
                            ConvertToDynamic(method, Module);
                        }
                    }
                }
                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public void CtorCallProtection(MethodDef method)
        {

            var instr = method.Body.Instructions;

            for (int i = 0; i < instr.Count; i++)
            {
                if (instr[i].OpCode == OpCodes.Call)
                {
                    // Verificar que i > 0 para evitar índice negativo
                    if (i == 0)
                        continue;

                    // Validar que el operando no sea nulo y contenga "void"
                    var operandString = instr[i].Operand?.ToString() ?? "";
                    if (operandString.ToLower().Contains("void") && instr[i - 1].IsLdarg())
                    {
                        Local new_local = new Local(method.Module.CorLibTypes.Int32);
                        method.Body.Variables.Add(new_local);

                        // Insertar nuevas instrucciones
                        instr.Insert(i - 1, OpCodes.Ldc_I4.ToInstruction(Random.Next()));
                        instr.Insert(i, OpCodes.Stloc_S.ToInstruction(new_local));
                        instr.Insert(i + 1, OpCodes.Ldloc_S.ToInstruction(new_local));
                        instr.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(Random.Next()));

                        // Ajustar índices después de las inserciones
                        instr.Insert(i + 3, OpCodes.Ldarg_0.ToInstruction());
                        instr.Insert(i + 4, OpCodes.Nop.ToInstruction());
                        instr.Insert(i + 6, OpCodes.Nop.ToInstruction());

                        // Insertar saltos condicionales
                        instr.Insert(i + 3, new Instruction(OpCodes.Bne_Un_S, instr[i + 4]));
                        instr.Insert(i + 5, new Instruction(OpCodes.Br_S, instr[i + 8]));
                        instr.Insert(i + 8, new Instruction(OpCodes.Br_S, instr[i + 9]));

                        // Ajustar el índice 'i' debido a las inserciones
                        i += 9; // Incrementar 'i' para evitar reprocesar las nuevas instrucciones
                    }
                }
            }
        }

        public bool ConvertToDynamic(MethodDef method, ModuleDef module)
        {
            try
            {
                AssemblyDef ctx = module.Assembly;
                Utils.LoadOpCodes();
                Utils2.LoadOpCodes();

                TypeDef type = method.DeclaringType;

                Instruction[] oldInstructions = method.Body.Instructions.ToArray();
                Instruction[] instructions = null;
                Local local = new Local(ctx.ManifestModule.Import(typeof(List<Type>)).ToTypeSig());
                Local local2 = new Local(ctx.ManifestModule.Import(typeof(DynamicMethod)).ToTypeSig());
                Local local3 = new Local(ctx.ManifestModule.Import(typeof(ILGenerator)).ToTypeSig());
                Local local4 = new Local(ctx.ManifestModule.Import(typeof(Label[])).ToTypeSig());
                TypeSig ReturnType = method.ReturnType;
                Local[] oldLocals = method.Body.Variables.ToArray();
                List<Local> outLocals = new List<Local>();
                if (method.Name != ".ctor")
                    if (method.HasParamDefs)
                        instructions = BuildInstruction(method.Body.Instructions.ToArray(), type, method, method.ParamDefs[0].DeclaringMethod.MethodSig.Params, method.ReturnType.ToTypeDefOrRef(), method.Parameters.ToArray(), type, local, local2, local3, local4, oldLocals, oldInstructions, ctx, false, out outLocals, ReturnType);
                    else
                        instructions = BuildInstruction(method.Body.Instructions.ToArray(), type, method, null, method.ReturnType.ToTypeDefOrRef(), method.Parameters.ToArray(), type, local, local2, local3, local4, oldLocals, oldInstructions, ctx, false, out outLocals, ReturnType);
                else
                    if (method.HasParamDefs)
                    instructions = BuildInstruction(method.Body.Instructions.ToArray(), type, method, method.ParamDefs[0].DeclaringMethod.MethodSig.Params, method.ReturnType.ToTypeDefOrRef(), method.Parameters.ToArray(), type, local, local2, local3, local4, oldLocals, oldInstructions, ctx, true, out outLocals, ReturnType);
                else
                    instructions = BuildInstruction(method.Body.Instructions.ToArray(), type, method, null, method.ReturnType.ToTypeDefOrRef(), method.Parameters.ToArray(), type, local, local2, local3, local4, oldLocals, oldInstructions, ctx, true, out outLocals, ReturnType);
                method.Body.Instructions.Clear();
                method.Body.Variables.Add(local);
                method.Body.Variables.Add(local2);
                method.Body.Variables.Add(local3);
                method.Body.Variables.Add(local4);
                foreach (Local locals in outLocals)
                    method.Body.Variables.Add(locals);
                foreach (Instruction inst in instructions)
                {
                    method.Body.Instructions.Add(inst);
                }
                return true;
            }
            catch { return false; }

        }
        static Dictionary<int, int> counterList = new Dictionary<int, int>();
        public Instruction[] BuildInstruction(Instruction[] toBuild, TypeDef typeDef, MethodDef method, IList<TypeSig> Param, ITypeDefOrRef type, IList<Parameter> pp, TypeDef typeM, Local local, Local local2, Local local3, Local local4, Local[] oldLocals, Instruction[] oldInstructions, AssemblyDef ctx, bool ISConstructorMethod, out List<Local> outLocals, TypeSig returnType)
        {
            List<Instruction> lista = new List<Instruction>();
            List<Local> variables = new List<Local>();
            lista.Add(OpCodes.Nop.ToInstruction());
            lista.Add(OpCodes.Ldc_I4.ToInstruction(9999));
            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Label))));
            lista.Add(OpCodes.Stloc_S.ToInstruction(local4));

            lista.Add(OpCodes.Newobj.ToInstruction(ctx.ManifestModule.Import(typeof(List<Type>).GetConstructor(new Type[0]))));
            lista.Add(OpCodes.Stloc_S.ToInstruction(local));
            if (pp.ToArray().Count() != 0)
                if (pp[0] != null)
                {
                    lista.Add(OpCodes.Ldloc_S.ToInstruction(local));
                    lista.Add(OpCodes.Ldtoken.ToInstruction(pp[0].Type.ToTypeDefOrRef()));
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(List<Type>).GetMethod("Add", new Type[] { typeof(Type) }))));
                }
            if (Param != null)
            {
                foreach (TypeSig p in Param)
                {
                    lista.Add(OpCodes.Ldloc_S.ToInstruction(local));
                    lista.Add(OpCodes.Ldtoken.ToInstruction(p.ToTypeDefOrRef()));
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(List<Type>).GetMethod("Add", new Type[] { typeof(Type) }))));
                }
            }
            lista.Add(OpCodes.Ldstr.ToInstruction(GenerateString(Mode)));
            lista.Add(OpCodes.Ldtoken.ToInstruction(type));
            lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
            lista.Add(OpCodes.Ldloc_S.ToInstruction(local));
            lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(List<Type>).GetMethod("ToArray", new Type[0]))));
            lista.Add(OpCodes.Ldtoken.ToInstruction(typeM));
            lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
            lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("get_Module"))));
            lista.Add(OpCodes.Ldc_I4_1.ToInstruction());
            lista.Add(OpCodes.Newobj.ToInstruction(ctx.ManifestModule.Import(typeof(DynamicMethod).GetConstructor(new Type[] { typeof(string), typeof(Type), typeof(Type[]), typeof(Module), typeof(bool) }))));
            lista.Add(OpCodes.Stloc_S.ToInstruction(local2));
            lista.Add(OpCodes.Ldloc_S.ToInstruction(local2));
            lista.Add(Instruction.Create(OpCodes.Callvirt, ctx.ManifestModule.Import(typeof(DynamicMethod).GetMethod("GetILGenerator", new Type[0]))));
            lista.Add(OpCodes.Stloc_S.ToInstruction(local3));
            if (ISConstructorMethod)
            {
                addLocal(new Local(ctx.ManifestModule.Import(typeDef).ToTypeSig()), local3, ref lista, ctx, ref variables);
            }
            if (oldLocals.Count() != 0)
            {
                foreach (Local nlocal in oldLocals)
                    addLocal(nlocal, local3, ref lista, ctx, ref variables);
                //lista.RemoveAt(lista.Count - 1);
            }
            List<Instruction> brTargets = new List<Instruction>();
            foreach (Instruction instruct in oldInstructions)
            {
                if (instruct.OpCode.OperandType == OperandType.InlineBrTarget || instruct.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    brTargets.Add(instruct);
                    lista.Add(OpCodes.Ldloc_S.ToInstruction(local4));
                    lista.Add(OpCodes.Ldc_I4.ToInstruction((int)((Instruction)instruct.Operand).Offset));
                    lista.Add(OpCodes.Ldloc_S.ToInstruction(local3));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("DefineLabel", new Type[0]))));
                    lista.Add(OpCodes.Stelem.ToInstruction(ctx.ManifestModule.Import(typeof(Label))));
                }
            }
            LocalsCount = 0;
            foreach (Instruction instruct in oldInstructions)
            {

                if (instruct.Operand != null)
                    ConvertInstructionWithOperand(instruct, local3, ref lista, variables, brTargets, ctx);
                else
                    ConvertInstruction(instruct, local3, ref lista, ctx);
            }
            lista.UpdateInstructionOffsets();
            var xD = new List<Instruction>();
            var xD2 = new List<Instruction>();
            var xD3 = new List<int>();
            var xD4 = new List<int>();
            foreach (Instruction instruct in lista)
                if (instruct.OpCode == OpCodes.Ldsfld)
                    xD.Add(instruct);

            foreach (Instruction instruct in oldInstructions)
                if (instruct.OpCode.OperandType == OperandType.InlineBrTarget || instruct.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    xD2.Add(instruct);
                    Instruction tmp = ((Instruction)(((Instruction)instruct).Operand));
                    int eee = 0;
                    for (int i = 0; i < oldInstructions.Count(); i++)
                    {
                        if (oldInstructions[i].OpCode == tmp.OpCode)
                        {
                            eee++;
                            if (oldInstructions[i] == tmp)
                            {

                                xD3.Add(eee); break;
                            }
                        }
                    }
                    tmp = instruct;
                    int eeex = 0;
                    for (int i = 0; i < oldInstructions.Count(); i++)
                    {
                        if (oldInstructions[i].OpCode == tmp.OpCode)
                        {
                            eeex++;
                            if (oldInstructions[i] == tmp)
                            {

                                xD4.Add(eeex); break;
                            }
                        }
                    }

                }
            int v = 0;
            int chave = 0;
            int subChave = 0;
            string preventLocalIntCharge = "";
            int localInt = 0;

            for (int e = 0; e < xD2.Count; e++)
            {
                for (int iJ = 0; iJ < lista.Count; iJ++)
                {
                    if (lista[iJ].OpCode != OpCodes.Ldsfld) continue;
                    {
                        if (chave != 0)
                        {
                            chave--;
                            continue;
                        }
                        var tmp = ((Instruction)(((Instruction)xD2[e]).Operand)).ToString().Substring(9).ToLower();
                        var tmp2 = lista[iJ].ToString().Replace("System.Reflection.Emit.OpCode System.Reflection.Emit.OpCodes::", "").ToLower().Replace("_", ".").Substring(16);
                        if (tmp == tmp2)
                        {
                            if (preventLocalIntCharge != tmp)
                                localInt = 0;
                            localInt++;
                            preventLocalIntCharge = tmp;
                            //if (method.Name == "Dispose" && tmp2 == "nop" && localInt == 1) continue;
                            if (localInt == xD3[v])
                            {
                                localInt = 0;
                                subChave++;
                                chave = subChave;
                                v++;
                                int getIndex = iJ;

                                lista.Insert(getIndex - 1, OpCodes.Ldloc_S.ToInstruction(local3));
                                //+2
                                lista.Insert(getIndex, OpCodes.Ldloc_S.ToInstruction(local4));
                                //+2

                                //xD.Add(xxx++, (int)((Instruction)oldInstructions[e].Operand).Offset);
                                lista.Insert(getIndex + 1, OpCodes.Ldc_I4.ToInstruction(((int)((Instruction)xD2[e].Operand).Offset)));
                                //oldInstructions = oldInstructions.RemoveAt(e);
                                //oldInstructions = oldInstructions.RemoveAt(e);
                                //+2
                                lista.Insert(getIndex + 2, OpCodes.Ldelem.ToInstruction(ctx.ManifestModule.Import(typeof(Label))));
                                //+2
                                lista.Insert(getIndex + 3, OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("MarkLabel", new Type[] { typeof(Label) }))));
                                iJ += 3;

                                goto il_finale;
                            }
                        }
                        else
                        {
                            chave = subChave;
                        }

                    }
                il_finale:;
                }


            }
            v = 0;
            chave = 0;
            subChave = 0;
            preventLocalIntCharge = "";
            localInt = 0;
            for (int e = 0; e < xD2.Count; e++)
            {
                for (int iJ = 0; iJ < lista.Count; iJ++)
                {
                    if (lista[iJ].OpCode != OpCodes.Ldsfld) continue;
                    {
                        if (chave != 0)
                        {
                            chave--;
                            continue;
                        }
                        var tmp = xD2[e].OpCode.ToString().ToLower();
                        var tmp2 = lista[iJ].ToString().Replace("System.Reflection.Emit.OpCode System.Reflection.Emit.OpCodes::", "").ToLower().Replace("_", ".").Substring(16);
                        if (tmp == tmp2)
                        {
                            if (preventLocalIntCharge != tmp)
                                localInt = 0;
                            localInt++;
                            preventLocalIntCharge = tmp;
                            //if (method.Name == "Dispose" && tmp2 == "nop" && localInt == 1) continue;
                            if (localInt == xD4[v])
                            {
                                localInt = 0;
                                subChave++;
                                chave = subChave;
                                v++;
                                int getIndex = iJ;

                                lista.Insert(getIndex + 1, OpCodes.Ldloc_S.ToInstruction(local4));
                                lista.Insert(getIndex + 2, OpCodes.Ldc_I4.ToInstruction(((int)((Instruction)xD2[e].Operand).Offset)));
                                lista.Insert(getIndex + 3, OpCodes.Ldelem.ToInstruction(ctx.ManifestModule.Import(typeof(Label))));
                                lista.Insert(getIndex + 4, OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(Label) }))));
                                iJ += 3;

                                goto il_finale;
                            }
                        }
                        else
                        {
                            chave = subChave;
                        }

                    }
                il_finale:;
                }


            }

            lista.Add(OpCodes.Ldloc_S.ToInstruction(local2));
            lista.Add(OpCodes.Ldnull.ToInstruction());
            if (Param != null)
                lista.Add(OpCodes.Ldc_I4.ToInstruction(Param.Count + 1));
            else if (pp.ToArray().Count() != 0)
                lista.Add(OpCodes.Ldc_I4.ToInstruction(pp.ToArray().Count()));
            else
                lista.Add(OpCodes.Ldc_I4.ToInstruction(0));
            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(object))));
            if (Param != null)
            {
                int x = 0;
                lista.Add(OpCodes.Dup.ToInstruction());
                foreach (Parameter p in pp)
                {
                    lista.Add(OpCodes.Ldc_I4.ToInstruction(x));
                    lista.Add(OpCodes.Ldarg_S.ToInstruction(p));
                    lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                    lista.Add(OpCodes.Dup.ToInstruction());
                    x++;
                }
                lista.RemoveAt(lista.Count - 1);
            }
            else if (pp.ToArray().Count() != 0)
            {
                int x = 0;
                lista.Add(OpCodes.Dup.ToInstruction());
                foreach (Parameter p in pp)
                {
                    lista.Add(OpCodes.Ldc_I4.ToInstruction(x));
                    lista.Add(OpCodes.Ldarg_S.ToInstruction(p));
                    lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                    lista.Add(OpCodes.Dup.ToInstruction());
                    x++;
                }
                lista.RemoveAt(lista.Count - 1);
            }
            lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }))));
            if (returnType.TypeName != "Void")
                lista.Add(OpCodes.Unbox_Any.ToInstruction(returnType.ToTypeDefOrRef()));
            else
                lista.Add(OpCodes.Pop.ToInstruction());
            lista.Add(OpCodes.Ret.ToInstruction());
            outLocals = variables;
            return lista.ToArray();
        }
        static int LocalsCount = 0;
        public void ConvertInstructionWithOperand(Instruction instruct, Local push, ref List<Instruction> lista, List<Local> variables, List<Instruction> brTargets, AssemblyDef ctx)
        {
            lista.Add(OpCodes.Ldloc_S.ToInstruction(push));
            char[] Opcode = Utils.ConvertOpCode(instruct.OpCode).Name.ToCharArray();
            Opcode[0] = Convert.ToChar(Opcode[0].ToString().Replace(Opcode[0].ToString(), Opcode[0].ToString().ToUpper()));
            string f = new string(Opcode);
            string a = "";
            if (f.Contains("."))
            {
                a = f.Substring(f.IndexOf('.')).ToUpper();
                f = f.Replace(a.ToLower(), a);
            }
            var final = typeof(ROpCodes).GetField(f.Replace(".", "_"), BindingFlags.Public | BindingFlags.Static);
            lista.Add(OpCodes.Ldsfld.ToInstruction(ctx.ManifestModule.Import(final)));

            var obj = instruct.Operand;
            if (obj is ConstructorInfo)
            {
                //il.Emit(Utils.ConvertOpCode(instr.OpCode), (ConstructorInfo)obj);
            }
            else if (obj is MethodDef)
            {
                if (obj.ToString().Contains(".ctor"))
                {
                    lista.Add(OpCodes.Ldtoken.ToInstruction(((MethodDef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                    lista.Add(OpCodes.Ldc_I4.ToInstruction(0));
                    lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(ConstructorInfo) }))));
                    return;
                }
                if (instruct.OpCode == OpCodes.Ldftn)
                {
                    lista.Add(OpCodes.Ldtoken.ToInstruction(((MethodDef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                    lista.Add(OpCodes.Ldstr.ToInstruction((((MethodDef)obj).Name)));
                    lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    int xx = 0;
                    int yy = 0;

                    foreach (TypeSig sig in ((MethodBaseSig)((MethodDef)obj).Signature).Params)
                    {
                        if (xx == 0)
                        {
                            lista.Add(OpCodes.Ldc_I4.ToInstruction(((MethodBaseSig)((MethodDef)obj).Signature).Params.Count));
                            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                            lista.Add(OpCodes.Dup.ToInstruction());
                            xx++;
                        }
                        lista.Add(OpCodes.Ldc_I4.ToInstruction(yy));
                        lista.Add(OpCodes.Ldtoken.ToInstruction(sig.ToTypeDefOrRef()));
                        lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                        lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                        lista.Add(OpCodes.Dup.ToInstruction());
                        yy++;
                    }
                    lista.RemoveAt(lista.Count - 1);
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                    return;
                }
                lista.Add(OpCodes.Ldtoken.ToInstruction(((MethodDef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                lista.Add(OpCodes.Ldstr.ToInstruction((((MethodDef)obj).Name)));
                lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                lista.Add(OpCodes.Ldnull.ToInstruction());
                int x = 0;
                int y = 0;
                if (((MethodBaseSig)((MethodDef)obj).Signature).Params.Count >= 1)
                {
                    foreach (TypeSig sig in ((MethodBaseSig)((MethodDef)obj).Signature).Params)
                    {

                        if (x == 0)
                        {
                            lista.Add(OpCodes.Ldc_I4.ToInstruction(((MethodBaseSig)((MethodDef)obj).Signature).Params.Count));
                            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                            lista.Add(OpCodes.Dup.ToInstruction());
                            x++;
                        }
                        lista.Add(OpCodes.Ldc_I4.ToInstruction(y));
                        lista.Add(OpCodes.Ldtoken.ToInstruction(sig.ToTypeDefOrRef()));
                        lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                        lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                        lista.Add(OpCodes.Dup.ToInstruction());
                        y++;
                    }
                    lista.RemoveAt(lista.Count - 1);
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                }
                else
                {
                    //lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                    //lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Ldc_I4_0.ToInstruction());
                    lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                    //il.Emit(Utils.ConvertOpCode(instr.OpCode), (MethodInfo)obj);
                }
                return;
            }
            else if (obj is string)
            {
                lista.Add(OpCodes.Ldstr.ToInstruction(obj.ToString()));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(string) }))));
                return;
            }
            else if (obj is TypeDef)
            {
                return;
            }
            else if (obj is ConstructorInfo)
            {
                //lista.Add(OpCodes.Ldtoken.ToInstruction(((MethodDef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                //lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                //int x = 0;
                //int y = 0;
                //if (((MethodBaseSig)((MethodDef)obj).Signature).Params.Count >= 1)
                //{
                //    foreach (TypeSig sig in ((MethodBaseSig)((MethodDef)obj).Signature).Params)
                //    {

                //        if (x == 0)
                //        {
                //            lista.Add(OpCodes.Ldc_I4.ToInstruction(((MethodBaseSig)((MethodDef)obj).Signature).Params.Count));
                //            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                //            lista.Add(OpCodes.Dup.ToInstruction());
                //            x++;
                //        }
                //        lista.Add(OpCodes.Ldc_I4.ToInstruction(y));
                //        lista.Add(OpCodes.Ldtoken.ToInstruction(sig.ToTypeDefOrRef()));
                //        lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                //        lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                //        lista.Add(OpCodes.Dup.ToInstruction());
                //        y++;
                //    }
                //    lista.RemoveAt(lista.Count - 1);
                //    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) }))));
                //    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(ConstructorInfo) }))));
                //}
                //else
                //{
                //    lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                //    lista.Add(OpCodes.Ldnull.ToInstruction());
                //    lista.Add(OpCodes.Ldc_I4_0.ToInstruction());
                //    lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                //    lista.Add(OpCodes.Ldnull.ToInstruction());
                //    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) }))));
                //    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                //}
            }
            else if (obj is int)
            {
                lista.Add(OpCodes.Ldc_I4.ToInstruction(int.Parse(obj.ToString())));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(int) }))));
                return;
            }
            else if (instruct.OpCode == OpCodes.Ldc_I4_S)
            {
                lista.Add(OpCodes.Ldc_I4_S.ToInstruction(sbyte.Parse(obj.ToString())));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(sbyte) }))));
                return;
            }
            else if (obj is double)
            {
                lista.Add(OpCodes.Ldc_R8.ToInstruction(double.Parse(obj.ToString().Replace(".", ","))));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(double) }))));
                return;
            }
            else if (obj is float)
            {
                lista.Add(OpCodes.Ldc_R4.ToInstruction(float.Parse(obj.ToString().Replace(".", ","))));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(float) }))));
                return;
            }
            else if (obj is dnlib.DotNet.MemberRef)
            {
                if (instruct.OpCode == OpCodes.Ldftn)
                    return;
                if (obj.ToString().Contains(".ctor"))
                {
                    lista.Add(OpCodes.Ldtoken.ToInstruction(((MemberRef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));

                    int xx = 0;
                    int yy = 0;
                    if (((MethodBaseSig)((MemberRef)obj).Signature).Params.Count >= 1)
                    {
                        foreach (TypeSig sig in ((MethodBaseSig)((MemberRef)obj).Signature).Params)
                        {

                            if (xx == 0)
                            {
                                lista.Add(OpCodes.Ldc_I4.ToInstruction(((MethodBaseSig)((MemberRef)obj).Signature).Params.Count));
                                lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                                lista.Add(OpCodes.Dup.ToInstruction());
                                xx++;
                            }
                            lista.Add(OpCodes.Ldc_I4.ToInstruction(yy));
                            lista.Add(OpCodes.Ldtoken.ToInstruction(sig.ToTypeDefOrRef()));
                            lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                            lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                            lista.Add(OpCodes.Dup.ToInstruction());
                            yy++;
                        }
                        lista.RemoveAt(lista.Count - 1);
                        lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) }))));
                        lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(ConstructorInfo) }))));
                    }
                    else
                    {
                        lista.Add(OpCodes.Ldc_I4.ToInstruction(0));
                        lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                        lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) }))));
                        lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(ConstructorInfo) }))));
                    }
                    return;
                }
                lista.Add(OpCodes.Ldtoken.ToInstruction(((MemberRef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                lista.Add(OpCodes.Ldstr.ToInstruction((((MemberRef)obj).Name)));
                int x = 0;
                int y = 0;
                if (((MethodBaseSig)((MemberRef)obj).Signature).Params.Count >= 1)
                {
                    foreach (TypeSig sig in ((MethodBaseSig)((MemberRef)obj).Signature).Params)
                    {

                        if (x == 0)
                        {
                            lista.Add(OpCodes.Ldc_I4.ToInstruction(((MethodBaseSig)((MemberRef)obj).Signature).Params.Count));
                            lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                            lista.Add(OpCodes.Dup.ToInstruction());
                            x++;
                        }
                        lista.Add(OpCodes.Ldc_I4.ToInstruction(y));
                        lista.Add(OpCodes.Ldtoken.ToInstruction(sig.ToTypeDefOrRef()));
                        lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                        lista.Add(OpCodes.Stelem_Ref.ToInstruction());
                        lista.Add(OpCodes.Dup.ToInstruction());
                        y++;
                    }
                    lista.RemoveAt(lista.Count - 1);
                    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(Type[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                }
                else
                {
                    lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Ldc_I4_0.ToInstruction());
                    lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                    lista.Add(OpCodes.Ldnull.ToInstruction());
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) }))));
                    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(MethodInfo) }))));
                    //il.Emit(Utils.ConvertOpCode(instr.OpCode), (MethodInfo)obj);
                }
                return;
            }
            else if (obj is FieldDef)
            {
                lista.Add(OpCodes.Ldtoken.ToInstruction(((FieldDef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                lista.Add(OpCodes.Ldstr.ToInstruction((((FieldDef)obj).Name)));
                lista.Add(OpCodes.Ldc_I4.ToInstruction(0x107FF7F));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetField", new Type[] { typeof(string), typeof(BindingFlags) }))));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(FieldInfo) }))));
                return;
            }
            else if (obj is TypeRef)
            {
                //if (obj.ToString().Contains(".ctor"))
                //{
                //    lista.Add(OpCodes.Ldtoken.ToInstruction(((TypeRef)obj).DeclaringType.ToTypeSig().ToTypeDefOrRef()));
                //    lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                //    lista.Add(OpCodes.Ldc_I4.ToInstruction(0));
                //    lista.Add(OpCodes.Newarr.ToInstruction(ctx.ManifestModule.Import(typeof(Type))));
                //    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetConstructor", new Type[] { typeof(Type[]) }))));
                //    lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(ConstructorInfo) }))));
                //    return;
                //}
                lista.Add(OpCodes.Ldtoken.ToInstruction(((TypeRef)obj).ToTypeSig().ToTypeDefOrRef()));
                lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
                lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(Type) }))));
                return;
            }
            else if (obj is Local)
            {
                //if (instruct.OpCode == OpCodes.Stloc_S)
                {
                    try
                    {
                        var res = variables.ToDictionary(x => x, x => x);
                        Local outL;
                        res.TryGetValue(variables[int.Parse((string)((Local)obj).ToString().Replace("V_", ""))], out outL);
                        lista.Add(OpCodes.Ldloc_S.ToInstruction(outL));
                        lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode), typeof(LocalBuilder) }))));
                        return;
                    }
                    catch (Exception ex) { Console.WriteLine(string.Format("{0}::{1} msg: {2}", instruct.OpCode, obj, ex.Message)); }
                }
            }

            else if (instruct.OpCode.OperandType == OperandType.InlineBrTarget || instruct.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                return;

            else if (instruct.OpCode == OpCodes.Nop)
            {
                return;
            }

            lista.RemoveAt(lista.Count - 1);
            lista.RemoveAt(lista.Count - 1);

        }
        public void ConvertInstruction(Instruction instruct, Local push, ref List<Instruction> lista, AssemblyDef ctx)
        {
            lista.Add(OpCodes.Ldloc_S.ToInstruction(push));
            char[] Opcode = Utils.ConvertOpCode(instruct.OpCode).Name.ToCharArray();
            Opcode[0] = Convert.ToChar(Opcode[0].ToString().Replace(Opcode[0].ToString(), Opcode[0].ToString().ToUpper()));
            string f = new string(Opcode);
            string a = "";
            if (f.Contains("."))
            {
                a = f.Substring(f.IndexOf('.')).ToUpper();
                f = f.Replace(a.ToLower(), a);
            }
            var final = typeof(ROpCodes).GetField(f.Replace(".", "_"), BindingFlags.Public | BindingFlags.Static);
            if (final == null)
                Console.WriteLine(f);
            lista.Add(OpCodes.Ldsfld.ToInstruction(ctx.ManifestModule.Import(final)));
            lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("Emit", new Type[] { typeof(ROpCode) }))));
        }
        public void addLocal(Local local, Local push, ref List<Instruction> lista, AssemblyDef ctx, ref List<Local> list)
        {
            lista.Add(OpCodes.Ldloc_S.ToInstruction(push));
            lista.Add(OpCodes.Ldtoken.ToInstruction(local.Type.ToTypeDefOrRef()));
            lista.Add(OpCodes.Call.ToInstruction(ctx.ManifestModule.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) }))));
            lista.Add(OpCodes.Callvirt.ToInstruction(ctx.ManifestModule.Import(typeof(ILGenerator).GetMethod("DeclareLocal", new Type[] { typeof(Type) }))));
            list.Add(new Local(ctx.ManifestModule.Import(typeof(LocalBuilder)).ToTypeSig()));
            lista.Add(OpCodes.Stloc_S.ToInstruction(list[list.Count - 1]));
        }
        public int Emulate(Instruction[] code, AssemblyDef ctx)
        {
            DynamicMethod emulatore = new DynamicMethod(GenerateString(Mode), typeof(void), null);
            ILGenerator il = emulatore.GetILGenerator();
            foreach (Instruction instr in code)
            {
                if (instr.Operand != null)
                {
                    if (instr.OpCode == OpCodes.Ldc_I4)
                        il.Emit(Utils.ConvertOpCode(instr.OpCode), Convert.ToInt32(instr.Operand));
                    else if (instr.OpCode == OpCodes.Ldstr)
                        il.Emit(Utils.ConvertOpCode(OpCodes.Ldstr), Convert.ToString(instr.Operand));
                    else if (instr.OpCode.OperandType == OperandType.InlineTok || instr.OpCode.OperandType == OperandType.InlineType || instr.OpCode.OperandType == OperandType.InlineMethod || instr.OpCode.OperandType == OperandType.InlineField)
                    {
                        Type Resolver = Assembly.LoadWithPartialName("AssemblyData").GetType("AssemblyData.methodsrewriter.Resolver");
                        var Method = Resolver.GetMethod("GetRtObject", new Type[] { typeof(ITokenOperand) });
                        var obj = Method.Invoke("", new object[] { (ITokenOperand)instr.Operand });
                        if (obj is ConstructorInfo)
                            il.Emit(Utils.ConvertOpCode(instr.OpCode), (ConstructorInfo)obj);
                        else if (obj is MethodInfo)
                            il.Emit(Utils.ConvertOpCode(instr.OpCode), (MethodInfo)obj);
                        else if (obj is FieldInfo)
                            il.Emit(Utils.ConvertOpCode(instr.OpCode), (FieldInfo)obj);
                        else if (obj is Type)
                            il.Emit(Utils.ConvertOpCode(instr.OpCode), (Type)obj);


                    }
                }
                else
                    il.Emit(Utils.ConvertOpCode(instr.OpCode));

            }
            emulatore.Invoke(null, new object[0]);
            //Result xdd = (Result)emulatore.CreateDelegate(typeof(Result));
            //int abcc = xdd.Invoke();
            return 0;
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }

    class Utils
    {
        static Dictionary<OpCode, ROpCode> dnlibToReflection = new Dictionary<OpCode, ROpCode>();
        static ROpCode ropcode;
        public static ROpCode ConvertOpCode(OpCode opcode)
        {

            if (dnlibToReflection.TryGetValue(opcode, out ropcode))
                return ropcode;
            return ROpCodes.Nop;
        }
        public static void LoadOpCodes()
        {
            var refDict = new Dictionary<short, ROpCode>(0x100);
            foreach (var f in typeof(ROpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(ROpCode))
                    continue;
                var ropcode = (ROpCode)f.GetValue(null);
                refDict[ropcode.Value] = ropcode;
            }

            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(OpCode))
                    continue;
                var opcode = (OpCode)f.GetValue(null);
                if (!refDict.TryGetValue(opcode.Value, out ropcode))
                    continue;
                dnlibToReflection[opcode] = ropcode;
            }
        }

    }
    class Utils2
    {
        static Dictionary<ROpCode, OpCode> reflectionToDnlib = new Dictionary<ROpCode, OpCode>();
        static OpCode Opcode;
        public static OpCode ConvertOpCode(ROpCode ropcode)
        {

            if (reflectionToDnlib.TryGetValue(ropcode, out Opcode))
                return Opcode;
            return OpCodes.Nop;
        }
        public static void LoadOpCodes()
        {
            var refDict = new Dictionary<short, OpCode>(0x100);
            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(OpCode))
                    continue;
                var opcode = (OpCode)f.GetValue(null);
                refDict[opcode.Value] = opcode;
            }

            foreach (var f in typeof(ROpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(ROpCode))
                    continue;
                var ropcode = (ROpCode)f.GetValue(null);
                if (!refDict.TryGetValue(ropcode.Value, out Opcode))
                    continue;
                reflectionToDnlib[ropcode] = Opcode;
            }
        }


    }
}
