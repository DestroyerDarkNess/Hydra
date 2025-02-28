using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class ResourceEncryption : Models.Protection
    {


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

        public ResourceEncryption() : base("Protection.Renamer.ResourceEncryption", "Renamer Phase", "Description for Renamer Phase") { }

        public bool UnsafeMutation { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                string mname = Randomizer.GenerateRandomString() + ".resources";
                int key = Core.Utils.RandomTinyInt32();
                ModuleDefMD moduleDefMD = ModuleDefMD.Load(typeof(Runtime.ResRuntime).Module);
                TypeDef typeDef = moduleDefMD.ResolveTypeDef(MDToken.ToRID(typeof(Runtime.ResRuntime).MetadataToken));
                IEnumerable<IDnlibDef> source = InjectHelper.Inject(typeDef, Module.GlobalType, Module);
                MethodDef init = (MethodDef)source.Single((IDnlibDef method) => method.Name == "Initialize");
                MethodDef ctor = Module.GlobalType.FindOrCreateStaticConstructor();
                var mutation = new MutationHelper();
                mutation.InjectKey<string>(init, 15, mname);
                mutation.InjectKey<int>(init, 0, key);
                //Helpers.Mutations.MutationHelper.InjectString(init, "h", mname);
                //Helpers.Mutations.MutationHelper.InjectKey(init, 10, key);
                ctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(init));
                foreach (var mem in source)
                {
                    if (mem is MethodDef method)
                    {
                        if (method.HasImplMap)
                            continue;
                        if (method.DeclaringType.IsDelegate)
                            continue;
                    }
                    mem.Name = Randomizer.GenerateRandomString();
                }
                string asmName = Randomizer.GenerateRandomString();
                var assembly = new AssemblyDefUser(asmName, new Version(0, 0));
                assembly.Modules.Add(new ModuleDefUser(asmName + ".dummy"));
                ModuleDef dlmodule = assembly.ManifestModule;
                assembly.ManifestModule.Kind = ModuleKind.Dll;
                var asmRef = new AssemblyRefUser(dlmodule.Assembly);
                for (int i = 0; i < Module.Resources.Count; i++)
                {
                    if (Module.Resources[i] is EmbeddedResource)
                    {
                        Module.Resources[i].Attributes = ManifestResourceAttributes.Private;
                        dlmodule.Resources.Add((Module.Resources[i] as EmbeddedResource));
                        Module.Resources.Add(new AssemblyLinkedResource(Module.Resources[i].Name, asmRef, Module.Resources[i].Attributes));
                        Module.Resources.RemoveAt(i);
                        i--;
                    }
                }
                byte[] moduleBuff;
                using (var ms = new MemoryStream())
                {
                    var options = new ModuleWriterOptions(dlmodule);
                    options.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                    options.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly;
                    options.ModuleKind = ModuleKind.Dll;
                    dlmodule.Write(ms, options);
                    var compressed = _7zip.QuickLZ.CompressBytes(ms.ToArray());
                    moduleBuff = Encrypt(compressed, key);
                }
                Module.Resources.Add(new EmbeddedResource(mname, moduleBuff, ManifestResourceAttributes.Private));
                var methods = new HashSet<MethodDef>
            {
                init
            };

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        private static byte[] Encrypt(byte[] plainBytes, int key)
        {
            var aes = Rijndael.Create();
            aes.Key = SHA256.Create().ComputeHash(BitConverter.GetBytes(key));
            aes.IV = new byte[16];
            aes.Mode = CipherMode.CBC;
            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return cipherBytes;
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
    public class MutationHelper : IDisposable
    {
        string m_mtFullName;
        public string MutationType
        {
            get { return m_mtFullName; }
            set { m_mtFullName = value; }
        }
        public MutationHelper() : this(typeof(MutationClass).FullName) { }
        public MutationHelper(string mtFullName)
        {
            m_mtFullName = mtFullName;
        }
        private static void SetInstrForInjectKey(Instruction instr, Type type, object value)
        {
            instr.OpCode = GetOpCode(type);
            instr.Operand = GetOperand(type, value);
        }
        private static OpCode GetOpCode(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return OpCodes.Ldc_I4;
                case TypeCode.SByte:
                    return OpCodes.Ldc_I4_S;
                case TypeCode.Byte:
                    return OpCodes.Ldc_I4;
                case TypeCode.Int32:
                    return OpCodes.Ldc_I4;
                case TypeCode.UInt32:
                    return OpCodes.Ldc_I4;
                case TypeCode.Int64:
                    return OpCodes.Ldc_I8;
                case TypeCode.UInt64:
                    return OpCodes.Ldc_I8;
                case TypeCode.Single:
                    return OpCodes.Ldc_R4;
                case TypeCode.Double:
                    return OpCodes.Ldc_R8;
                case TypeCode.String:
                    return OpCodes.Ldstr;
                default:
                    throw new SystemException("Unreachable code reached.");
            }
        }
        private static object GetOperand(Type type, object value)
        {
            if (type == typeof(bool))
            {
                return (bool)value ? 1 : 0;
            }
            return value;
        }
        public void InjectKey<T>(MethodDef method, int keyId, T key)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
                throw new ArgumentException();

            var instrs = method.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Call && instrs[i].Operand is IMethod keyMD)
                {
                    if (keyMD.DeclaringType.FullName == m_mtFullName &&
                        keyMD.Name == "Key")
                    {
                        var keyMDId = method.Body.Instructions[i - 1].GetLdcI4Value();
                        if (keyMDId == keyId)
                        {
                            if (typeof(T).IsAssignableFrom(Type.GetType(keyMD.FullName.Split(' ')[0])))
                            {
                                method.Body.Instructions.RemoveAt(i);

                                SetInstrForInjectKey(instrs[i - 1], typeof(T), key);
                            }
                            else
                                throw new ArgumentException("The specified type does not match the type to be injected.");
                        }
                    }
                }
            }
        }
        public void InjectKeys<T>(MethodDef method, int[] keyIds, T[] keys)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
            {
                throw new ArgumentException();
            }
            var instrs = method.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Call && instrs[i].Operand is IMethod keyMD)
                {
                    if (keyMD.DeclaringType.FullName == m_mtFullName &&
                        keyMD.Name == "Key")
                    {
                        var keyMDId = method.Body.Instructions[i - 1].GetLdcI4Value();
                        if (keyMDId == 0 || Array.IndexOf(keyIds, keyMDId) != -1)
                        {
                            if (typeof(T).IsAssignableFrom(Type.GetType(keyMD.FullName.Split(' ')[0])))
                            {
                                method.Body.Instructions.RemoveAt(i);
                                SetInstrForInjectKey(instrs[i - 1], typeof(T), keys[keyMDId]);
                            }
                            else
                            {
                                throw new ArgumentException("The specified type does not match the type to be injected.");
                            }
                        }
                    }
                }
            }
        }
        public bool GetInstrLocationIndex(MethodDef method, bool removeCall, out int index)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
                throw new ArgumentException();
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instr = method.Body.Instructions[i];
                if (instr.OpCode == OpCodes.Call)
                {
                    var md = instr.Operand as IMethod;
                    if (md.DeclaringType.FullName == m_mtFullName && md.Name == "LocationIndex")
                    {
                        index = i;
                        if (removeCall)
                            method.Body.Instructions.RemoveAt(i);

                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        public void Dispose()
        {
            m_mtFullName = null;
            GC.SuppressFinalize(this);
        }
    }
}
