using AsmResolver;
using AsmResolver.DotNet;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class AsmResolver_Renamer : Models.Protection
    {
        public RenameMode Mode { get; set; } = RenameMode.Ascii;

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

        private const int MaxChunkSize = 10000; // Tamaño de segmento razonable

        private string GenerateProblematicLongString(long length)
        {
            var stringBuilder = new StringBuilder((int)length);

            for (long i = 0; i < length; i += MaxChunkSize)
            {
                int chunkSize = (int)Math.Min(MaxChunkSize, length - i);
                char[] chunk = new char[chunkSize];

                for (int j = 0; j < chunkSize; j++)
                {
                    chunk[j] = ProblematicChars[Random.Next(ProblematicChars.Length)];
                }

                stringBuilder.Append(chunk);
            }

            return stringBuilder.ToString();
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


        public bool Resources { get; set; } = false;
        public bool Methods { get; set; } = false;
        public bool Parameters { get; set; } = false;
        public bool Properties { get; set; } = false;
        public bool Fields { get; set; } = false;
        public bool Events { get; set; } = false;
        public bool ClassName { get; set; } = false;
        public bool Namespace { get; set; } = false;
        public bool NamespaceEmpty { get; set; } = false;
        public bool ModuleRenaming { get; set; } = false;
        public bool ModuleInvisible { get; set; } = false;

        public bool UnsafeRenamer { get; set; } = false;

        public AsmResolver_Renamer() : base("Protection.Renamer.AsmResolver", "Renamer Phase", "Description for Renamer Phase") { ManualReload = true; }

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                var Module = ModuleDefinition.FromFile(moduledef);

                if (ModuleRenaming)
                {
                    if (ModuleInvisible == false)
                    {
                        Module.Name = tag + GenerateString(Mode);

                        Module.EncId = Guid.NewGuid();
                        Module.EncBaseId = Guid.NewGuid();

                        Module.Assembly.CustomAttributes.Clear();
                        Module.Mvid = Guid.NewGuid();
                        Module.Assembly.Name = tag + GenerateString(Mode);
                        Module.Assembly.Version = new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9));
                    }
                    else
                    {
                        Module.Name = GenerateString(RenameMode.Invisible);

                        Module.EncId = Guid.NewGuid();
                        Module.EncBaseId = Guid.NewGuid();

                        Module.Assembly.CustomAttributes.Clear();
                        Module.Mvid = Guid.NewGuid();
                        Module.Assembly.Name = GenerateString(RenameMode.Invisible);
                        Module.Assembly.Version = new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9));
                    }

                }

                foreach (var type in Module.GetAllTypes())
                {

                    foreach (var method in type.Methods)
                    {

                        if (Module.ManagedEntryPointMethod == null && type.IsPublic && (method.IsPublic || type.IsInterface))
                            continue; // shouldn't rename

                        if (!method.IsRuntimeSpecialName)
                        {
                            // handle abstract

                            if (method.IsConstructor)
                                continue;

                            if (method.IsAbstract || method.IsVirtual || (type.BaseType != null && type.BaseType.Resolve() != null && type.BaseType.Resolve().IsInterface && type.BaseType.Resolve().Methods.Select(x => x.Name).Contains(method.Name)))
                                continue; // fml

                            if (!Methods) continue;

                            if (UnsafeRenamer || Asm_Analyzer.CanRename(method, type))
                            {
                                method.Name = tag + GenerateString(Mode);
                            }

                        }

                        if (Parameters)
                        {
                            foreach (var arg in method.ParameterDefinitions)
                            {
                                if (UnsafeRenamer || Asm_Analyzer.CanRename(type, arg))
                                {
                                    arg.Name = "<" + tag + GenerateString(Mode) + ">;";
                                }
                            }
                        }


                    }

                    if (Properties)
                    {
                        foreach (var properties in type.Properties)
                        {
                            if (Module.ManagedEntryPointMethod == null && properties.IsSpecialName && type.IsPublic)
                                continue; // shouldn't rename

                            if (properties.IsRuntimeSpecialName) continue;

                            if (UnsafeRenamer || Asm_Analyzer.CanRename(type, properties))
                            {
                                properties.Name = "<" + tag + GenerateString(Mode) + ">;";
                            }
                        }
                    }

                    if (Fields)
                    {
                        foreach (var field in type.Fields)
                        {
                            if (Module.ManagedEntryPointMethod == null && field.IsPublic && type.IsPublic)
                                continue; // shouldn't rename

                            if (field.IsRuntimeSpecialName) continue;

                            if (UnsafeRenamer || Asm_Analyzer.CanRename(type, field))
                            {
                                if (Asm_Analyzer.CanRename(type)) field.Name = "<" + tag + GenerateString(Mode) + ">;";
                            }

                        }
                    }

                    if (Events)
                    {
                        foreach (var Event in type.Events)
                        {
                            if (Module.ManagedEntryPointMethod == null && Event.IsSpecialName && type.IsPublic)
                                continue; // shouldn't rename

                            if (Event.IsRuntimeSpecialName) continue;

                            if (UnsafeRenamer || Asm_Analyzer.CanRename(Event))
                            {
                                Event.Name = "<" + tag + GenerateString(Mode) + ">;";
                            }

                        }
                    }

                    if (UnsafeRenamer || Asm_Analyzer.CanRename(type)) //
                    {
                        //if (type.Namespace.Value.EndsWith(".My.Resources")) continue;
                        //if (type.Namespace.Value.Contains(".My")) continue;

                        if (Module.ManagedEntryPointMethod == null && type.IsPublic)
                        { // idk how to unfuck this but it should work

                        }
                        else
                        { // should rename

                            if (Namespace)
                            {
                                if (NamespaceEmpty)
                                {
                                    type.Namespace = "";
                                }
                                else
                                {
                                    type.Namespace = GenerateString(Mode);
                                }
                            }

                            if (ClassName)
                            {
                                string ClassNewName = tag + GenerateString(Mode);

                                if (!type.IsModuleType && !type.IsRuntimeSpecialName)
                                {
                                    if (type.BaseType != null && type.BaseType.FullName.ToLower().Contains("form"))
                                    {
                                        foreach (ManifestResource src in Module.Resources)
                                        {
                                            if (src.Name.Contains(type.Name + ".resources"))
                                            {
                                                src.Name = type.Namespace.Value + "." + ClassNewName + ".resources";
                                            }
                                        }
                                    }
                                    type.Name = ClassNewName;
                                }
                            }
                        }
                    }
                }

                if (Resources)
                {
                    foreach (var res in Module.Resources)
                    {
                        if (res.IsPublic)
                        {
                            if (res.Name.Contains(".Properties")) continue;
                            res.Name = tag + GenerateString(Mode);
                        }
                    }
                }

                MemoryStream OuputAssembly = new MemoryStream();
                Module.Write(OuputAssembly);

                TempModule = OuputAssembly;

                if (TempModule == null) throw new Exception("MemoryStream is null");

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string TempRenamer = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(TempRenamer); } catch (Exception Ex) { this.Errors = Ex; }

            return Execute(TempRenamer);
        }
    }


    internal class Asm_Analyzer
    {
        public static bool CanRename(TypeDefinition type)
        {
            if (Utf8String.IsNullOrEmpty(type.Namespace))
            {
                return false;
            }

            if (Utf8String.IsNullOrEmpty(type.Name))
            {
                return false;
            }

            if (type.IsSpecialName != false || type.IsRuntimeSpecialName != false) return false;

            if (type.Name.Value.StartsWith("<") || type.Name == "GeneratedInternalTypeHelper" || type.Name == "Resources" || type.Name == "MySettings" || type.Name == "Settings")
                return false;
            //if (type.IsAbstract)
            //    return false; 
            if (type.FullName == "TrinityAttribute")
            {
                return false;
            }
            if (type.Namespace == "Costura")
            {
                return false;
            }
            if (type.Name.Value.StartsWith("<"))
            {
                return false;
            }
            //if (type.IsGlobalModuleType)
            //{
            //    return false;
            //}
            if (type.IsModuleType)
            {
                return false;
            }
            if (type.IsInterface)
            {
                return false;
            }
            if (type.IsForwarder)
            {
                return false;
            }
            if (type.IsSerializable)
            {
                return false;
            }
            if (type.IsEnum)
            {
                return false;
            }
            if (type.IsRuntimeSpecialName)
            {
                return false;
            }
            if (type.IsSpecialName)
            {
                return false;
            }
            if (type.IsRuntimeSpecialName)
            {
                return false;
            }
            if (type.IsNestedFamilyOrAssembly)
            {
                return false;
            }
            if (type.IsNestedFamilyAndAssembly)
            {
                return false;
            }
            return true;
        }

        public static bool CanRename(EventDefinition e)
        {
            if (e.IsSpecialName)
            {
                return false;
            }
            if (e.IsRuntimeSpecialName)
            {
                return false;
            }
            return true;
        }

        public static bool CanRename(TypeDefinition type, PropertyDefinition p)
        {

            if (p == null)
            {
                return false;
            }
            if (p.DeclaringType.Implements("System.ComponentModel.INotifyPropertyChanged"))
            {
                return false;
            }
            if (!Utf8String.IsNullOrEmpty(type.Namespace))
            {
                if (type.Namespace.Value.Contains(".Properties"))
                {
                    return false;
                }
            }
            if (p.DeclaringType.Name.Value.Contains("AnonymousType"))
            {
                return false;
            }
            if (p.IsRuntimeSpecialName)
            {
                return false;
            }
            //if (p.IsEmpty)
            //{
            //    return false;
            //}
            if (p.IsSpecialName)
            {
                return false;
            }
            return true;
        }

        public static bool CanRename(TypeDefinition type, FieldDefinition field)
        {
            if (field.DeclaringType.IsSerializable && !field.IsNotSerialized)
            {
                return false;
            }
            if (field.DeclaringType.BaseType.Name.Contains("Delegate"))
            {
                return false;
            }
            if (field.Name.Value.StartsWith("<"))
            {
                return false;
            }
            if (field.IsLiteral && field.DeclaringType.IsEnum)
            {
                return false;
            }
            if (field.IsFamilyOrAssembly)
            {
                return false;
            }
            if (field.IsSpecialName)
            {
                return false;
            }
            if (field.IsRuntimeSpecialName)
            {
                return false;
            }
            if (field.IsFamily)
            {
                return false;
            }
            if (field.DeclaringType.IsEnum)
            {
                return false;
            }
            if (field.DeclaringType.BaseType.Name.Contains("Delegate"))
            {
                return false;
            }
            return true;
        }

        public static bool CanRename(MethodDefinition method, TypeDefinition type)
        {
            //if (method.DeclaringType.IsComImport() && !method.HasAttribute("System.Runtime.InteropServices.DispIdAttribute"))
            //{
            //    return false;
            //} 
            if (method.HasMethodBody == false || method.CilMethodBody == null) return false;

            if (method.CilMethodBody.Instructions.Count == 0) return false;

            if (method.DeclaringType.BaseType != null && method.DeclaringType.BaseType.Name.Contains("Delegate"))
            {
                return false;
            }
            if (method.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (method.DeclaringType.FullName == "System.Windows.Forms.Binding" && method.Name.Value == ".ctor")
            {
                return false;
            }
            if (method.DeclaringType.FullName == "System.Windows.Forms.ControlBindingsCollection")
            {
                return false;
            }
            if (method.DeclaringType.FullName == "System.Windows.Forms.BindingsCollection")
            {
                return false;
            }
            if (method.DeclaringType.FullName == "System.Windows.Forms.DataGridViewColumn")
            {
                return false;
            }
            if (method.Name == "Invoke")
            {
                return false;
            }
            if (method.IsSetMethod || method.IsGetMethod)
            {
                return false;
            }
            if (method.IsSpecialName)
            {
                return false;
            }
            if (method.IsFamilyAndAssembly)
            {
                return false;
            }
            if (method.IsFamily)
            {
                return false;
            }
            if (method.IsRuntime)
            {
                return false;
            }
            if (method.IsRuntimeSpecialName)
            {
                return false;
            }
            if (method.IsConstructor)
            {
                return false;
            }
            if (method.IsNative)
            {
                return false;
            }
            if (method.IsPInvokeImpl || method.IsUnmanagedExport || method.IsUnmanagedExport)
            {
                return false;
            }
            if (method == null)
            {
                return false;
            }
            if (method.Name.Value.StartsWith("<"))
            {
                return false;
            }
            //if (method.Overrides.Count > 0)
            //{
            //    return false;
            //} 
            if (method.IsConstructor)
            {
                return false;
            }
            if (method.DeclaringType.IsModuleType)
            {
                return false;
            }
            if (method.DeclaringType.IsForwarder)
            {
                return false;
            }
            if (method.IsVirtual)
            {
                return false;
            }
            if (method.ImplementationMap != null)
            {
                return false;
            }
            return true;
        }

        public static bool CanRename(TypeDefinition type, ParameterDefinition p)
        {
            if (type.FullName == "<Module>")
            {
                return false;
            }
            //if (p.IsHiddenThisParameter)
            //{
            //    return false;
            //}
            if (p.Name == string.Empty)
            {
                return false;
            }
            return true;
        }
    }

}
