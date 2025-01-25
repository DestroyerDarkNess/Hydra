using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HydraEngine.Protection.Renamer.RenamerPhase;

namespace HydraEngine.Protection.Invalid
{
    public static class ByHydraProtector
    {
    }

    public class InvalidMDPhase : Models.Protection
    {
        public string tag { get; set; } = "HailHydra";

        public RenameMode Mode { get; set; } = RenameMode.Key;

        public int Length { get; set; } = 20;
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private Random Random = new Random();

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

        public InvalidMDPhase() : base("Protection.Invalid.InvalidMDPhase", "Renamer Phase", "Description for Renamer Phase") { }


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
                    return GenerateProblematicString(Random.Next(3, Length));// RandomString(16, BaseChars);
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
                // Add a reference to a non-existent type
                var nonExistentType = new TypeRefUser(Module, "HydraNamespace", "HydraExistentType", Module.CorLibTypes.AssemblyRef);
                var nonExistentMethod = new MemberRefUser(Module, "HydraExistentMethod", MethodSig.CreateStatic(Module.CorLibTypes.Void), nonExistentType);

                // Add the fake type and method reference to the module
                Module.Types.Add(new TypeDefUser("InvalidMetadata", nonExistentType));

                // Add a method to the module that references the non-existent method
                var method = new MethodDefUser("HydraMethodWithInvalidRef",
                    MethodSig.CreateStatic(Module.CorLibTypes.Void),
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Public | MethodAttributes.Static);

                var body = new CilBody();
                method.Body = body;
                body.Instructions.Add(OpCodes.Call.ToInstruction(nonExistentMethod));
                body.Instructions.Add(OpCodes.Ret.ToInstruction());

                Module.GlobalType.Methods.Add(method);


                var asm = Module.Assembly;
                var module = asm.ManifestModule;
                module.Mvid = null;
                module.Name = GenerateString(Mode);
                asm.ManifestModule.Import(new FieldDefUser(GenerateString(Mode)));
                foreach (var typeDef in module.Types)
                {
                    TypeDef typeDef2 = new TypeDefUser(GenerateString(Mode));
                    typeDef2.Methods.Add(new MethodDefUser());
                    typeDef2.NestedTypes.Add(new TypeDefUser(GenerateString(Mode)));
                    MethodDef item = new MethodDefUser();
                    typeDef2.Methods.Add(item);
                    typeDef.NestedTypes.Add(typeDef2);
                    typeDef.Events.Add(new EventDefUser());
                    foreach (var meth in typeDef.Methods)
                    {
                        if (meth.Body == null) continue;
                        meth.Body.SimplifyBranches();
                        if (string.Compare(meth.ReturnType.FullName, "System.Void", StringComparison.Ordinal) != 0 || !meth.HasBody ||
                            meth.Body.Instructions.Count == 0) continue;
                        var typeSig = asm.ManifestModule.Import(typeof(int)).ToTypeSig();
                        var local = new Local(typeSig);
                        var typeSig2 = asm.ManifestModule.Import(typeof(bool)).ToTypeSig();
                        var local2 = new Local(typeSig2);
                        meth.Body.Variables.Add(local);
                        meth.Body.Variables.Add(local2);
                        var operand = meth.Body.Instructions[meth.Body.Instructions.Count - 1];
                        var instruction = new Instruction(OpCodes.Ret);
                        var instruction2 = new Instruction(OpCodes.Ldc_I4_1);
                        meth.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4_0));
                        meth.Body.Instructions.Insert(1, new Instruction(OpCodes.Stloc, local));
                        meth.Body.Instructions.Insert(2, new Instruction(OpCodes.Br, instruction2));
                        var instruction3 = new Instruction(OpCodes.Ldloc, local);
                        meth.Body.Instructions.Insert(3, instruction3);
                        meth.Body.Instructions.Insert(4, new Instruction(OpCodes.Ldc_I4_0));
                        meth.Body.Instructions.Insert(5, new Instruction(OpCodes.Ceq));
                        meth.Body.Instructions.Insert(6, new Instruction(OpCodes.Ldc_I4_1));
                        meth.Body.Instructions.Insert(7, new Instruction(OpCodes.Ceq));
                        meth.Body.Instructions.Insert(8, new Instruction(OpCodes.Stloc, local2));
                        meth.Body.Instructions.Insert(9, new Instruction(OpCodes.Ldloc, local2));
                        meth.Body.Instructions.Insert(10, new Instruction(OpCodes.Brtrue, meth.Body.Instructions[10]));
                        meth.Body.Instructions.Insert(11, new Instruction(OpCodes.Ret));
                        meth.Body.Instructions.Insert(12, new Instruction(OpCodes.Calli));
                        meth.Body.Instructions.Insert(13, new Instruction(OpCodes.Sizeof, operand));
                        meth.Body.Instructions.Insert(meth.Body.Instructions.Count, instruction2);
                        meth.Body.Instructions.Insert(meth.Body.Instructions.Count, new Instruction(OpCodes.Stloc, local2));
                        meth.Body.Instructions.Insert(meth.Body.Instructions.Count, new Instruction(OpCodes.Br, instruction3));
                        meth.Body.Instructions.Insert(meth.Body.Instructions.Count, instruction);
                        var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            HandlerStart = meth.Body.Instructions[10],
                            HandlerEnd = meth.Body.Instructions[11],
                            TryEnd = meth.Body.Instructions[14],
                            TryStart = meth.Body.Instructions[12]
                        };
                        if (!meth.Body.HasExceptionHandlers)
                        {
                            meth.Body.ExceptionHandlers.Add(exceptionHandler);
                        }
                        meth.Body.OptimizeBranches();
                        meth.Body.OptimizeMacros();
                    }
                }
                TypeDef typeDef3 = new TypeDefUser(GenerateString(Mode));
                FieldDef item2 = new FieldDefUser(GenerateString(Mode), new FieldSig(module.Import(typeof(ByHydraProtector)).ToTypeSig()));
                typeDef3.Fields.Add(item2);
                typeDef3.BaseType = module.Import(typeof(ByHydraProtector));
                module.Types.Add(typeDef3);
                TypeDef typeDef4 = new TypeDefUser(GenerateString(Mode))
                {
                    IsInterface = true,
                    IsSealed = true
                };
                module.Types.Add(typeDef4);
                module.TablesHeaderVersion = 257;

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
