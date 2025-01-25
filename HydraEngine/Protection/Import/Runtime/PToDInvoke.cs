using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace HydraEngine.Protection.Import.Runtime
{
    public static class PToDInvoke
    {

        public static T CreateAPI<T>(string wLib, string mName)
        {
            System.Windows.Forms.MessageBox.Show(wLib, "Called: " + mName);
            AssemblyBuilder ASMB = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName), AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder MODB = ASMB.DefineDynamicModule(System.Reflection.MethodBase.GetCurrentMethod().Name);
            TypeBuilder TB = MODB.DefineType(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.TypeAttributes.Public);
            System.Reflection.MethodInfo MI = typeof(T).GetMethods()[0];
            List<Type> LP = new List<Type>();

            foreach (System.Reflection.ParameterInfo pI in MI.GetParameters())
                LP.Add(pI.ParameterType);

            MethodBuilder MB = TB.DefinePInvokeMethod(mName,
                                                      wLib,
                                                      System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.PinvokeImpl,
                                                      System.Reflection.CallingConventions.Standard,
                                                      MI.ReturnType, LP.ToArray(),
                                                      System.Runtime.InteropServices.CallingConvention.Winapi,
                                                      CharSet.Ansi);

            MB.SetImplementationFlags(MB.GetMethodImplementationFlags() | System.Reflection.MethodImplAttributes.PreserveSig);

            return (T)((object)Delegate.CreateDelegate(typeof(T), TB.CreateType().GetMethod(mName)));
        }

    }
}
