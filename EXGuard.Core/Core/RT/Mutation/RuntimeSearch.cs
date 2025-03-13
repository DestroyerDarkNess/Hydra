using System;
using System.Reflection;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EXGuard.Core.RT.Mutation
{
    internal class RuntimeSearch: IDisposable
    {
        public ModuleDefMD mscorlib = ModuleDefMD.Load(typeof(Module).Module);

        private VMRuntime VMRT;
        private ModuleDef RTMD;

        #region Searched
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public TypeDef RuntimeFieldHandle;

        public TypeDef FieldInfo;
        public MethodDef FieldInfo_GetFieldFromHandle_1;
        public MethodDef FieldInfo_get_FieldHandle;

        public TypeDef RTConstantsProtection;
        public MethodDef RTConstantsProtection_Initialize;
        public MethodDef RTConstantsProtection_Get;

        public TypeDef VMInstance;
        public MethodDef VMInstance_Invoke;
        public FieldDef STATIC_VMInstance;

        public TypeDef JITRuntime;
        public MethodDef JITRuntime_Ctor;
        public MethodDef JITRuntime_Initialize;

        public TypeDef VMData;
        public MethodDef VMData_Ctor;

        public TypeDef VMFuncSig;
        public MethodDef VMFuncSig_Ctor;

        public TypeDef TypedRef;
        public MethodDef TypedRef_Ctor;

        public TypeDef VMEntry;
        public MethodDef VMEntry_Ctor;
        public MethodDef VMEntry_EntryInitialize;
        public List<object> VMEntry_Invoke = STATIC_VMEntry_Invoke;
        public static List<object> STATIC_VMEntry_Invoke = null;

        public TypeDef Utils;
        public MethodDef Utils_Decrypt;
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        public RuntimeSearch(ModuleDef rt, VMRuntime runtime)
        {
            RTMD = rt;
            VMRT = runtime;
        }

        public RuntimeSearch Search()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            RuntimeFieldHandle = mscorlib.Find(RTMap.RuntimeFieldHandle, true);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            FieldInfo = mscorlib.Find(RTMap.FieldInfo, true);
            FieldInfo_get_FieldHandle = FieldInfo.FindMethod(RTMap.FieldInfo_get_FieldHandle);

            foreach (var gffHnd in FieldInfo.FindMethods(RTMap.FieldInfo_GetFieldFromHandle_1))
                if (gffHnd.Parameters.Count == 1)
                    FieldInfo_GetFieldFromHandle_1 = gffHnd;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            RTConstantsProtection = RTMD.Find(RTMap.RTConstantsProtection, true);
            RTConstantsProtection_Initialize = RTConstantsProtection.FindMethod(RTMap.RTConstantsProtection_Initialize);
            RTConstantsProtection_Get = RTConstantsProtection.FindMethod(RTMap.RTConstantsProtection_Get);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            VMInstance = RTMD.Find(RTMap.VMInstance, true);
            VMInstance_Invoke = VMInstance.FindMethod(RTMap.VMInstance_Invoke);
            STATIC_VMInstance = VMInstance.FindField(RTMap.STATIC_VMInstance);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            VMEntry = RTMD.Find(RTMap.VMEntry, true);
            VMEntry_EntryInitialize = VMEntry.FindMethod(RTMap.VMEntry_EntryInitialize);

            foreach (var entry_ctor in VMEntry.FindMethods(RTMap.AnyCtor))
                VMEntry_Ctor = entry_ctor;

            Create_VMEntry_Invoke(RTMD, VMInstance, VMInstance_Invoke, STATIC_VMInstance);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            VMData = RTMD.Find(RTMap.VMData, true);

            foreach (var vmd_ctor in VMData.FindMethods(RTMap.AnyCtor))
                VMData_Ctor = vmd_ctor;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            VMFuncSig = RTMD.Find(RTMap.VMFuncSig, true);

            foreach (var vsig_ctor in VMFuncSig.FindMethods(RTMap.AnyCtor))
                VMFuncSig_Ctor = vsig_ctor;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            TypedRef = RTMD.Find(RTMap.TypedRef, true);

            foreach (var tref_ctor in TypedRef.FindMethods(RTMap.AnyCtor))
                TypedRef_Ctor = tref_ctor;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            JITRuntime = RTMD.Find(RTMap.JITRuntime, true);
            JITRuntime_Initialize = JITRuntime.FindMethod(RTMap.JITRuntime_Initialize);

            foreach (var jrt in JITRuntime.FindMethods(RTMap.AnyCtor))
                JITRuntime_Ctor = jrt;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Utils = RTMD.Find(RTMap.Utils, true);
            Utils_Decrypt = Utils.FindMethod(RTMap.Utils_Decrypt);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            return this;
        }

        private static void Create_VMEntry_Invoke(ModuleDef module, TypeDef type, MethodDef method, FieldDef field)
        {
            if (STATIC_VMEntry_Invoke != null)
                if (STATIC_VMEntry_Invoke.Count != 0)
                    return;

            STATIC_VMEntry_Invoke = new List<object>();

            var ret = new TypeDefUser(UTF8String.Empty, "proxy_delegate_",
                module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"))
            {
                Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.Sealed
            };

            var ctor = new MethodDefUser(".ctor",
                MethodSig.CreateInstance(
                    module.CorLibTypes.Void,
                    module.CorLibTypes.Object,
                    module.CorLibTypes.IntPtr))
            {
                Attributes = (dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.HideBySig |
                             dnlib.DotNet.MethodAttributes.RTSpecialName | dnlib.DotNet.MethodAttributes.SpecialName),
                ImplAttributes = dnlib.DotNet.MethodImplAttributes.Runtime
            };
            ret.Methods.Add(ctor);

            var invoke = new MethodDefUser("Invoke", method.MethodSig.Clone())
            {
                MethodSig = { HasThis = true },
                Attributes = dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.Virtual |
                             dnlib.DotNet.MethodAttributes.NewSlot,
                ImplAttributes = dnlib.DotNet.MethodImplAttributes.Runtime
            };
            ret.Methods.Add(invoke);


            var delField = new FieldDefUser("proxy_delegate_field", new FieldSig(ret.ToTypeSig()),
                dnlib.DotNet.FieldAttributes.Static | dnlib.DotNet.FieldAttributes.Public);
            ret.Fields.Add(delField);

            module.Types.Add(ret);

            var delegate_cctor = ret.FindOrCreateStaticConstructor();
            delegate_cctor.Body = new CilBody();

            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, delField));

            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, method));
            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, delField));

            delegate_cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            delegate_cctor.Body.Instructions.Insert(delegate_cctor.Body.Instructions.Count - 5,
                OpCodes.Brtrue_S.ToInstruction(delegate_cctor.Body.Instructions[delegate_cctor.Body.Instructions.Count - 1]));

            STATIC_VMEntry_Invoke.Add(ret); // Delegate Type
            STATIC_VMEntry_Invoke.Add(ctor); // Delegate Type .ctor
            STATIC_VMEntry_Invoke.Add(delegate_cctor); // Delegate Type .cctor
            STATIC_VMEntry_Invoke.Add(invoke); // Delegate Type Invoke Method
            STATIC_VMEntry_Invoke.Add(delField); // Delegate Type Field
        }

        public void Dispose()
        {
            mscorlib.Dispose();

            VMEntry_Invoke.Clear();
            STATIC_VMEntry_Invoke.Clear();
        }
    }
}
