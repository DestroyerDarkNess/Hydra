using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Linq;

namespace HydraEngine.Protection.Method
{
    public class MethodMover
    {

        public static TypeDef MoveMethodILToStaticClass(MethodDef originalMethod, ModuleDefMD module)
        {
            if (originalMethod == null || !originalMethod.HasBody)
                return null;

            var container = GetOrCreateStaticContainerClass(module);

            try
            {
                if (container != null)
                    container.Name = Randomizer.GenerateRandomString();

                var originalSig = originalMethod.MethodSig;
                var retType = originalSig.RetType;
                var paramTypes = originalSig.Params;
                bool isInstanceMethod = originalSig.HasThis;


                var newSig = MethodSig.CreateStatic(retType, paramTypes.ToArray());
                newSig.HasThis = false;

                if (isInstanceMethod && module != null)
                {
                    var declaringTypeSig = module.Import(originalMethod.DeclaringType).ToTypeSig();
                    newSig.Params.Insert(0, declaringTypeSig);
                }

                var newMethodName = originalMethod.Name; //"MovedIL_" + originalMethod.Name + "_" + Guid.NewGuid().ToString("N");
                var newMethod = new MethodDefUser(
                    newMethodName,
                    newSig,
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Static | MethodAttributes.Public
                );


                newMethod.Body = originalMethod.Body;

                if (container != null)
                    container.Methods.Add(newMethod);

                originalMethod.Body = new CilBody();
                var il = originalMethod.Body.Instructions;

                int paramCount = originalMethod.Parameters.Count;

                for (int i = 0; i < paramCount; i++)
                {
                    if (i == 0) il.Add(OpCodes.Ldarg_0.ToInstruction());
                    else if (i == 1) il.Add(OpCodes.Ldarg_1.ToInstruction());
                    else if (i == 2) il.Add(OpCodes.Ldarg_2.ToInstruction());
                    else if (i == 3) il.Add(OpCodes.Ldarg_3.ToInstruction());
                    else il.Add(OpCodes.Ldarg.ToInstruction((ushort)i));
                }

                var methodRef = newMethod;

                il.Add(Instruction.Create(OpCodes.Call, methodRef));
                il.Add(Instruction.Create(OpCodes.Ret));
                return container;
            }
            catch { module.Types.Remove(container); return null; }
        }

        public static TypeDef MoveMethodILToStaticDelegate(MethodDef originalMethod, ModuleDefMD module)
        {
            if (originalMethod == null || !originalMethod.HasBody)
                return null;

            var container = GetOrCreateStaticContainerDelegate(module, originalMethod.MethodSig);

            try
            {

                if (container != null)
                    container.Name = Randomizer.GenerateRandomString();

                var originalSig = originalMethod.MethodSig;
                var retType = originalSig.RetType;
                var paramTypes = originalSig.Params;
                bool isInstanceMethod = originalSig.HasThis;


                var newSig = MethodSig.CreateStatic(retType, paramTypes.ToArray());
                newSig.HasThis = false;

                if (isInstanceMethod && module != null)
                {
                    var declaringTypeSig = module.Import(originalMethod.DeclaringType).ToTypeSig();
                    newSig.Params.Insert(0, declaringTypeSig);
                }

                var newMethodName = originalMethod.Name; // "MovedIL_" + originalMethod.Name + "_" + Guid.NewGuid().ToString("N");
                var newMethod = new MethodDefUser(
                    newMethodName,
                    newSig,
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Static | MethodAttributes.Public
                );


                newMethod.Body = originalMethod.Body;

                if (container != null)
                    container.Methods.Add(newMethod);

                CilBody newOriginalBody = new CilBody();
                var il = newOriginalBody.Instructions;

                int paramCount = originalMethod.Parameters.Count;

                for (int i = 0; i < paramCount; i++)
                {
                    if (i == 0) il.Add(OpCodes.Ldarg_0.ToInstruction());
                    else if (i == 1) il.Add(OpCodes.Ldarg_1.ToInstruction());
                    else if (i == 2) il.Add(OpCodes.Ldarg_2.ToInstruction());
                    else if (i == 3) il.Add(OpCodes.Ldarg_3.ToInstruction());
                    else il.Add(OpCodes.Ldarg.ToInstruction((ushort)i));
                }

                originalMethod.Body = newOriginalBody;

                var methodRef = newMethod;

                il.Add(Instruction.Create(OpCodes.Call, methodRef));
                il.Add(Instruction.Create(OpCodes.Ret));
                return container;
            }
            catch { module.Types.Remove(container); return null; }
        }

        public static TypeDef GetOrCreateStaticContainerDelegate(ModuleDefMD module, MethodSig originalStaticSig)
        {
            if (module == null)
                return null;

            string delegateName = "MyHiddenDelegate_" + Guid.NewGuid().ToString("N");
            var delegateType = new TypeDefUser(
            delegateName,
            module.CorLibTypes.GetTypeRef("System", "MulticastDelegate")
        );
            delegateType.Attributes =
                TypeAttributes.Sealed
                | TypeAttributes.AnsiClass
                | TypeAttributes.AutoClass
                | TypeAttributes.BeforeFieldInit
                | TypeAttributes.NotPublic; // o Public si prefieres
            module.Types.Add(delegateType);

            // 2) .ctor => instance void .ctor(object, IntPtr), runtime managed
            var ctorSig = MethodSig.CreateInstance(
                module.CorLibTypes.Void,
                module.CorLibTypes.Object,
                module.CorLibTypes.IntPtr
            );
            ctorSig.HasThis = true; // Aseguramos que es método de instancia
            var delegateCtor = new MethodDefUser(
                ".ctor",
                ctorSig,
                MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                MethodAttributes.Public
                  | MethodAttributes.HideBySig
                  | MethodAttributes.SpecialName
                  | MethodAttributes.RTSpecialName
            );
            delegateType.Methods.Add(delegateCtor);

            // 3) Invoke => instance [retType] Invoke([params...])
            var retType = originalStaticSig.RetType;
            var paramTypes = originalStaticSig.Params;
            var invokeSig = MethodSig.CreateInstance(retType, paramTypes.ToArray());
            invokeSig.HasThis = true; // Delegados siempre son métodos de instancia
            var delegateInvoke = new MethodDefUser(
                "Invoke",
                invokeSig,
                MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot
                    | MethodAttributes.Virtual
                    | MethodAttributes.Final
            );
            delegateType.Methods.Add(delegateInvoke);

            // 4) BeginInvoke => instance IAsyncResult BeginInvoke([...params], AsyncCallback, object)
            //    Normalmente se construye la misma lista de parámetros + callback + state
            var asyncCallbackType = module.Import(typeof(AsyncCallback)).ToTypeSig();
            var iasyncResultType = module.Import(typeof(IAsyncResult)).ToTypeSig();

            var beginParams = paramTypes.ToList(); // copia los params del Invoke
            beginParams.Add(asyncCallbackType);
            beginParams.Add(module.CorLibTypes.Object);

            var beginInvokeSig = MethodSig.CreateInstance(iasyncResultType, beginParams.ToArray());
            beginInvokeSig.HasThis = true;

            var beginInvoke = new MethodDefUser(
                "BeginInvoke",
                beginInvokeSig,
                MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot
                    | MethodAttributes.Virtual
                    | MethodAttributes.Final
            );
            delegateType.Methods.Add(beginInvoke);

            var endParams = new TypeSig[] { iasyncResultType };
            var endInvokeSig = MethodSig.CreateInstance(retType, endParams);
            endInvokeSig.HasThis = true;

            var endInvoke = new MethodDefUser(
                "EndInvoke",
                endInvokeSig,
                MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot
                    | MethodAttributes.Virtual
                    | MethodAttributes.Final
            );
            delegateType.Methods.Add(endInvoke);

            return delegateType;
        }


        /// <summary>
        /// Creates or returns a static container class named "MovedMethodsContainer" 
        /// in the root of the module, so we can add our new static methods to it.
        /// </summary>
        public static TypeDef GetOrCreateStaticContainerClass(ModuleDefMD module)
        {
            if (module == null)
                return null;

            // Try to find it if we already created it
            var existing = module.Types.FirstOrDefault(t => t.Name == "MovedMethodsContainer");
            if (existing != null)
                return existing;

            // Not found, so create a new top-level type (class)
            var container = new TypeDefUser(
                // You can store it in your own custom namespace 
                // to reduce name collisions
                "HydraEngine_Moved",
                "MovedMethodsContainer",
                module.CorLibTypes.Object.ToTypeDefOrRef()
            )
            {
                // Make it a 'static-like' class (abstract + sealed)
                Attributes = TypeAttributes.NotPublic
                    | TypeAttributes.AutoLayout
                    | TypeAttributes.AnsiClass
                    | TypeAttributes.Class
                    | TypeAttributes.Abstract
                    | TypeAttributes.Sealed
                    | TypeAttributes.BeforeFieldInit
            };

            // Add it to the module
            module.Types.Add(container);

            return container;
        }
    }
}
