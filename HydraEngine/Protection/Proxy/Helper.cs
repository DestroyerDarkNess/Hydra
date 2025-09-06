using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HydraEngine.Protection.Renamer.RenamerPhase;

namespace HydraEngine.Protection.Proxy
{
    internal class Helper
    {
        public static string tag { get; set; } = "HailHydra";
        public static int Length { get; set; } = 20;
        public static string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static string[] m_reservedWords =  { "addhandler", "addressof", "alias", "and", "andalso", "ansi", "append", "as",
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

        public static RenameMode Mode { get; set; } = RenameMode.Ascii;

        public enum RenameMode
        {
            Ascii,
            Key,
            Normal,
            Invisible
        }

        private static Random Random = new Random();

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

        private static string GenerateInvisibleString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = InvisibleChars[Random.Next(InvisibleChars.Length)];
            }
            return new string(result);
        }

        private static string RandomString(int length, string chars)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private static string GetRandomName()
        {
            return NormalNameStrings[Random.Next(NormalNameStrings.Length)];
        }

        private static string GetRandomReservedWords()
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

        private static string GenerateProblematicString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ProblematicChars[Random.Next(ProblematicChars.Length)];
            }
            return new string(result);
        }

        public static string GenerateString(RenameMode mode)
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

        public static string InvisibleName
        {
            get
            {
                return string.Format("<{0}>{1}", tag, GenerateName());
            }
        }

        public static string GenerateName()
        {
            return GenerateString(Mode);
        }

        // Token: 0x06000040 RID: 64 RVA: 0x000056F4 File Offset: 0x000038F4
        public MethodDef GenerateMethod(TypeDef declaringType, object targetMethod, bool hasThis = false, bool isVoid = false)
        {
            MemberRef memberRef = (MemberRef)targetMethod;
            MethodDef methodDef = new MethodDefUser(InvisibleName, MethodSig.CreateStatic(memberRef.ReturnType), MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static);
            methodDef.Body = new CilBody();
            if (hasThis)
            {
                methodDef.MethodSig.Params.Add(declaringType.Module.Import(declaringType.ToTypeSig(true)));
            }
            foreach (TypeSig item in memberRef.MethodSig.Params)
            {
                methodDef.MethodSig.Params.Add(item);
            }
            methodDef.Parameters.UpdateParameterTypes();
            foreach (Parameter parameter in methodDef.Parameters)
            {
                methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, parameter));
            }
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Call, memberRef));
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return methodDef;
        }

        // Token: 0x06000041 RID: 65 RVA: 0x00005854 File Offset: 0x00003A54
        public MethodDef GenerateMethod(IMethod targetMethod, MethodDef md)
        {
            MethodDef methodDef = new MethodDefUser(InvisibleName, MethodSig.CreateStatic(md.Module.Import(targetMethod.DeclaringType.ToTypeSig(true))), MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static);
            methodDef.ImplAttributes = MethodImplAttributes.IL;
            methodDef.IsHideBySig = true;
            methodDef.Body = new CilBody();
            for (int i = 0; i < targetMethod.MethodSig.Params.Count; i++)
            {
                methodDef.ParamDefs.Add(new ParamDefUser(InvisibleName, (ushort)(i + 1)));
                methodDef.MethodSig.Params.Add(targetMethod.MethodSig.Params[i]);
            }
            methodDef.Parameters.UpdateParameterTypes();
            for (int j = 0; j < methodDef.Parameters.Count; j++)
            {
                Parameter operand = methodDef.Parameters[j];
                methodDef.Body.Instructions.Add(new Instruction(OpCodes.Ldarg, operand));
            }
            methodDef.Body.Instructions.Add(new Instruction(OpCodes.Newobj, targetMethod));
            methodDef.Body.Instructions.Add(new Instruction(OpCodes.Ret));
            return methodDef;
        }

        // Token: 0x06000042 RID: 66 RVA: 0x000059A4 File Offset: 0x00003BA4
        public MethodDef GenerateMethod(FieldDef targetField, MethodDef md)
        {
            MethodDef methodDef = new MethodDefUser(InvisibleName, MethodSig.CreateStatic(md.Module.Import(targetField.FieldType)), MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static);
            methodDef.Body = new CilBody();
            TypeDef declaringType = md.DeclaringType;
            methodDef.MethodSig.Params.Add(md.Module.Import(declaringType).ToTypeSig(true));
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, targetField));
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            md.DeclaringType.Methods.Add(methodDef);
            return methodDef;
        }
    }
}