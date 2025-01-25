using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Models;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class RenamerPhase : Models.Protection
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
      
        public bool Methods { get; set; } = false;
        public bool Parameters { get; set; } = false;
        public bool Properties { get; set; } = false;
        public bool Fields { get; set; } = false;
        public bool Events { get; set; } = false;
        public bool ClassName { get; set; } = false;
        public bool Namespace { get; set; } = false;
        public bool NamespaceEmpty { get; set; } = false;
        public bool Resources { get; set; } = false;
        public bool ModuleRenaming { get; set; } = false;
        public bool ModuleInvisible { get; set; } = false;

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

        private  string GenerateInvisibleString(int length)
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

        private  string GenerateProblematicString(int length)
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
                    return  GetRandomName();
                case RenameMode.Invisible:
                    return GenerateInvisibleString(Random.Next(3, Length));
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }


        public RenamerPhase() : base("Protection.Renamer", "Renamer Phase", "Description for Renamer Phase") { }


        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                //if (string.IsNullOrWhiteSpace(tag))
                //    tag = "HailHydra";

                if (Resources == true)
                {
                    foreach (Resource resource in Module.Resources)
                    {
                        if (resource.Name.Contains(".Properties")) continue;
                        string newName = string.Format("<{1}>{0}", GenerateString(Mode),  tag);
                        resource.Name = newName;
                    }
                }

                if (Properties == true)
                {
                    ExecutePropertiesRenaming(Module);
                }

                if (Fields == true)
                {
                    ExecuteFieldRenaming(Module);
                }

                if (Events == true)
                {
                    ExecuteEventRenaming(Module);
                }

                if (Methods == true)
                {
                    ExecuteMethodRenaming(Module);
                }

                if (Parameters)
                {
                    foreach (TypeDef type in Module.Types.ToArray())
                    {
                        if (!Analyzer.CanRename(type)) continue;
                        foreach (MethodDef method in type.Methods)
                        {
                            if (!Analyzer.CanRename(method)) continue;
                            foreach (Parameter parameter in method.Parameters)
                            {
                                foreach (GenericParam genParam in type.GenericParameters)
                                {
                                    if (Analyzer.CanRename(type, parameter))
                                        genParam.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);
                                    if (Analyzer.CanRename(type, parameter))
                                        parameter.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);
                                }
                            }
                        }
                    }
                      
                }

                ExecuteClassRenaming(Module);

                //if (Namespace == true)
                //{
                //    ExecuteNamespaceRenaming(Module);
                //}

                if (ModuleRenaming == true)
                {
                    ExecuteModuleRenaming(Module);
                }

              
          

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public void ExecuteEventRenaming(ModuleDefMD module)
        {
            foreach (TypeDef type in module.Types)
            {
                if (!Analyzer.CanRename(type)) continue;
                foreach (EventDef eventDef in type.Events)
                {
                    if (Analyzer.CanRename(eventDef))
                        eventDef.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);
                }
            }

        }


        public bool ApplyCompilerGeneratedAttribute { get; set; } = false;

        List<string> excludedTypes = new List<string> {
            "System.Windows.Application",
            "System.Windows.Window",
            "System.Windows.Controls.UserControl",
            "System.Windows.Input.ICommand",
            // Añade más tipos aquí según sea necesario
        };
        bool IsExcluded(TypeDef type, List<string> excludedTypes)
        {
            return excludedTypes.Contains(type.FullName);
        }

        public void ExecuteClassRenaming(ModuleDefMD module)
        {
            
            foreach (TypeDef type in module.Types)
            {

                if (!Analyzer.CanRename(type)) continue;

                if (ApplyCompilerGeneratedAttribute)
                {
                    var compilerGeneratedAttributeRef = module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");

                    var compilerGeneratedAttribute = new MemberRefUser(module, ".ctor",
                        MethodSig.CreateInstance(module.CorLibTypes.Void),
                        compilerGeneratedAttributeRef);

                    HydraEngine.Core.InjectHelper.AddAttributeToType(type, compilerGeneratedAttribute);
                }

                string formNamespace = type.Namespace;
                string formName = type.Name;

                if (Namespace)
                {
                    if (NamespaceEmpty)  {
                        formNamespace = string.Empty;
                    } else {
                        formNamespace =  GenerateString(Mode);
                    }
                } 

                if (ClassName == true)
                {
                    if (Mode == RenameMode.Invisible)
                    {
                        formName = GenerateString(Mode);
                    }
                    else
                    {
                        formName = string.Format("<{1}>{0}", GenerateString(Mode), tag);
                    }
                }

                if (type.BaseType != null && type.BaseType.FullName.ToLower().Contains("form"))
                {
                    foreach (Resource src in module.Resources)
                    {
                        if (src.Name.Contains(type.Name + ".resources"))
                        {
                            src.Name = formNamespace + "." + formName + ".resources";
                        }
                    }
                }

                type.Namespace = formNamespace;
                type.Name = formName;

                foreach (MethodDef method in type.Methods)
                {
                  
                    if (!Analyzer.CanRename(method)) continue;
                  
                    if (method.Name.Equals("InitializeComponent") && method.HasBody)
                    {
                        foreach (Instruction instruction in method.Body.Instructions)
                        {
                            if (instruction.OpCode.Equals(OpCodes.Ldstr))
                            {
                                string str = (string)instruction.Operand;
                                if (str == type.Name)
                                {
                                    instruction.Operand = formName;
                                    break;
                                }
                            }
                        }
                    }
                }

            }

            //if (Namespace == true) ApplyChangesToResourcesNamespace(module);

        }


        private  void ApplyChangesToResourcesNamespace(ModuleDefMD module)
        {
            foreach (Resource resource in module.Resources)
            {
                if (resource == null) continue;
                foreach (KeyValuePair<string, string> item in Names.Where(item => resource.Name.Contains(item.Key)))
                {
                    resource.Name = resource.Name.Replace(item.Key, item.Value);
                }
            }

            foreach (TypeDef type in module.GetTypes())
            {
                if (type == null) continue;
                foreach (PropertyDef property in type.Properties)
                {
                    if (property == null) continue;
                    if (property.Name != "ResourceManager")
                    {
                        continue;
                    }


                    IList<Instruction> instr = property.GetMethod.Body.Instructions;
                    if (instr == null) continue;
                    for (int i = 0; i < instr.Count - 3; i++)
                    {
                        if (instr[i].OpCode == OpCodes.Ldstr)
                        {
                            foreach (KeyValuePair<string, string> item in Names.Where(item =>
                                         item.Key == instr[i].Operand.ToString()))
                            {
                                instr[i].Operand = item.Value;
                            }
                        }
                    }
                }
            }
        }

     

        public  void ExecuteFieldRenaming(ModuleDefMD module)
        {
            foreach (TypeDef type in module.Types)
            {
                if (!Analyzer.CanRename(type)) continue;
                foreach (FieldDef field in type.Fields)
                {
                    if (!Analyzer.CanRename(type, field)) continue;

                    if (Names.TryGetValue(field.Name, out string nameValue))
                    {
                        field.Name = nameValue;
                    }
                    else
                    {
                        string newName = tag + GenerateString(Mode);
                       
                        Names.Add(field.Name, newName);
                        field.Name = newName;
                    }
                 
                }
              
            }
          
           
        }

        public void ExecuteMethodRenaming(ModuleDefMD module)
        {
            foreach (TypeDef type in module.GetTypes().ToArray())
            {
                if (!Analyzer.CanRename(type)) continue;
                foreach (MethodDef method in type.Methods.ToArray())
                {
                    if (!Analyzer.CanRename(method)) continue;

                    var typeRef = module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
                    var ctor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.Import(typeof(void)).ToTypeSig(true)), typeRef);
                    var item = new CustomAttribute(ctor);
                    method.CustomAttributes.Add(item);

                    method.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);  //tag + GenerateString(Mode);
                    foreach (var parameter in method.Parameters)
                        parameter.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);

                    foreach (var variable in method.Body.Variables)
                        variable.Name = string.Format("<{1}>{0}", GenerateString(Mode),  tag);

                    //// Convertir el método a async
                    //method.Attributes |= MethodAttributes.PrivateScope | MethodAttributes.Virtual;
                    //method.ReturnType = module.CorLibTypes.GetTypeRef("System.Threading.Tasks", "Task").ToTypeSig();
                    //   var taskDelayMethod = module.Import(typeof(System.Threading.Tasks.Task).GetMethod("Delay", new[] { typeof(int) }));

                    //// Modificar las variables locales
                    //var body = method.Body;
                    //var instrs = body.Instructions;

                    //// Modificar las variables locales agregando `await` ficticio
                    //for (int i = 0; i < instrs.Count; i++)
                    //{
                    //    var instr = instrs[i];
                    //    if (instr.OpCode == OpCodes.Stloc || instr.OpCode == OpCodes.Stloc_S)
                    //    {
                    //        // Insertar múltiples `await` ficticios
                    //        for (int j = 0; j < 10; j++)
                    //        {
                    //            instrs.Insert(i + 1, Instruction.Create(OpCodes.Ldc_I4, 1));
                    //            instrs.Insert(i + 2, Instruction.Create(OpCodes.Call, taskDelayMethod));
                    //            instrs.Insert(i + 3, Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Runtime.CompilerServices.TaskAwaiter).GetMethod("GetResult"))));
                    //        }
                    //    }
                    //}

                }
            }
        }

        public  void ExecuteModuleRenaming(ModuleDefMD mod)
        {
            foreach (ModuleDef module in mod.Assembly.Modules)
            {
                bool isWpf = false;
                foreach (AssemblyRef asmRef in module.GetAssemblyRefs())
                {
                    if (asmRef.Name == "WindowsBase" || asmRef.Name == "PresentationCore" ||
                        asmRef.Name == "PresentationFramework" || asmRef.Name == "System.Xaml")
                    {
                        isWpf = true;
                    }
                }
                isWpf = Analyzer.IsWpfModule(mod);
                if (!isWpf)
                {

                    if (ModuleInvisible == false)
                    {
                        module.Name = tag + GenerateString(Mode);

                        module.EncId = Guid.NewGuid();
                        module.EncBaseId = Guid.NewGuid();

                        module.Assembly.CustomAttributes.Clear();
                        module.Mvid = Guid.NewGuid();
                        module.Assembly.Name = tag + GenerateString(Mode);
                        module.Assembly.Version =  new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9),  Random.Next(1, 9));
                    } 
                    else
                    {
                        module.Name = GenerateString( RenameMode.Invisible);

                        module.EncId = Guid.NewGuid();
                        module.EncBaseId = Guid.NewGuid();

                        module.Assembly.CustomAttributes.Clear();
                        module.Mvid = Guid.NewGuid();
                        module.Assembly.Name =  GenerateString(RenameMode.Invisible);
                        module.Assembly.Version = new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9),  Random.Next(1, 9));
                    }
                  
                }
            }
        }

        public  void ExecuteNamespaceRenaming(ModuleDefMD module)
        {
            foreach (TypeDef type in module.GetTypes())
            {
                if (type.IsGlobalModuleType) continue;

                if (type.Namespace == "") continue;

                if (Names.TryGetValue(type.Namespace, out string nameValue))
                {
                    type.Namespace = nameValue;
                }
                else
                {
                    string newName =  GenerateString(Mode);
                    Names.Add(type.Namespace, newName);
                    type.Namespace = newName;
                }
            }

           
        }

        private  void ExecutePropertiesRenaming(ModuleDefMD module)
        {
            foreach (TypeDef type in module.Types)
            {
                //if (!Analyzer.CanRename(type)) continue;
                foreach (PropertyDef property in type.Properties)
                {
                    if (Analyzer.CanRename(type, property))
                    {
                        if (property.FullName.Contains("My.MySettings")) continue;
                        property.Name = tag + GenerateString(Mode);
                    }
                       
                }
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }



    }

}
