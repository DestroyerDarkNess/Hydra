namespace EXGuard.Core.RT
{
    internal class RTMap
    {
        public static string RuntimeFieldHandle = "System.RuntimeFieldHandle";

        public static string FieldInfo = "System.Reflection.FieldInfo";
        public static string FieldInfo_GetFieldFromHandle_1 = "GetFieldFromHandle";
        public static string FieldInfo_get_FieldHandle = "get_FieldHandle";

        public static string RTConstantsProtection = "EXGuard.Runtime.RTProtection.Constant";
        public static string RTConstantsProtection_Initialize = "Initialize";
        public static string RTConstantsProtection_Get = "Get";

        public static string VMEntry = "EXGuard.Runtime.VMEntry";
        public static string VMEntry_EntryInitialize = "EntryInitialize";
        public static string VMEntry_Invoke = "Invoke";

        public static string VMInstance = "EXGuard.Runtime.VMInstance";
        public static string VMInstance_Invoke = "Invoke";
        public static string STATIC_VMInstance = "STATIC_Instance";

        public static string VMFuncSig = "EXGuard.Runtime.Data.VMFuncSig";

        public static string JITRuntime = "EXGuard.Runtime.JIT.JITRuntime";
        public static string JITRuntime_Initialize = "Initialize";

        public static string VMData = "EXGuard.Runtime.Data.VMData";

        public static string TypedRef = "EXGuard.Runtime.Execution.TypedRef";

        public static string VMDispatcher = "EXGuard.Runtime.Execution.VMDispatcher";
        public static string VMDispatcher_DoThrow = "DoThrow";
        public static string VMDispatcher_Throw = "Throw";

        public static string Utils = "EXGuard.Runtime.Utils";
        public static string Utils_Decrypt = "Decrypt";

        public static string Mutation = "Mutation";
        public static string Mutation_Placeholder = "Placeholder";
        public static string Mutation_LocationIndex = "LocationIndex";
        public static string Mutation_Value_T = "Value";
        public static string Mutation_Value_T_Arg0 = "Value";
        public static string Mutation_Crypt = "Crypt";

        public static string AnyCtor = ".ctor";
    }
}