using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Import.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Import
{
    public class PInvokeToDInvoke : Models.Protection
    {
        public PInvokeToDInvoke() : base("Protection.Import.PInvokeToDInvoke", "PInvoke To DInvoke", "Description for Renamer Phase") { }

        private Dictionary<string, MethodDef> Names = new Dictionary<string, MethodDef>();

        public override async Task<bool> Execute(ModuleDefMD module)
        {
          
            var typeModule = ModuleDefMD.Load(typeof(PToDInvoke).Module);
            var cctor = module.GlobalType.FindOrCreateStaticConstructor();
            var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(PToDInvoke).MetadataToken));
            TypeDef ClassPToDInvoke = InjectHelper.Inject(typeDef, module);

            MethodDef CreateAPIMethod = ClassPToDInvoke.Methods.Single(method => method.Name == "CreateAPI");

            var types = new List<TypeDef>(module.Types);
            foreach (TypeDef type in types)
            {
               
                var methods = new List<MethodDef>(type.Methods);
                foreach (MethodDef method in methods)
                {
                   
                    if (method.IsPinvokeImpl)
                    {
                        string ID = method.Name;
                        bool CreateDelegate = true;

                        if (Names.ContainsKey(ID))
                        {
                            if (Names.TryGetValue(ID, out var key))
                            {
                                string Params = string.Join("-", method.Parameters.Select(p => p.Type.GetName()));
                                string ParamsSaved = string.Join("-", key.Parameters.Select(p => p.Type.GetName()));

                                if (Params == ParamsSaved)
                                {
                                    CreateDelegate = false;
                                } 
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        ImplMap pinvokeInfo = method.ImplMap;
                        CreateDInvokeMethod(module, method, pinvokeInfo, CreateAPIMethod, CreateDelegate);

                        if (CreateDelegate) Names.Add(ID, method);

                        type.Remove(method);

                        Console.WriteLine($"Método: {method.Name}");
                        Console.WriteLine($"Modulo DLL: {pinvokeInfo.Module.Name}");
                        Console.WriteLine($"Nombre del Método: {pinvokeInfo.Name}");
                        Console.WriteLine($"Parameters: {string.Join(", ", method.Parameters.Select(p => p.Name))}");
                        Console.WriteLine();
                    }
                }
            }

            return true;
        }

        private void CreateDInvokeMethod(ModuleDefMD module, MethodDef pinvokeMethod, ImplMap pinvokeInfo, MethodDef CallMethod, bool CreateDelegate = true)
        {
            // Obtener el tipo que contiene el método P/Invoke
            TypeDef declaringType = pinvokeMethod.DeclaringType;

            FieldDef fieldToStoreDelegate = null;

            if (CreateDelegate)
            {
                // Crear el delegado
                string delegateName = pinvokeMethod.Name + "Delegate";
                TypeDef delegateType = new TypeDefUser("", delegateName, module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
                module.Types.Add(delegateType);

                // Crear el constructor del delegado
                MethodDef constructor = new MethodDefUser(".ctor",
                    MethodSig.CreateInstance(module.CorLibTypes.Void,
                        module.CorLibTypes.Object,
                        module.CorLibTypes.IntPtr));
                constructor.Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                delegateType.Methods.Add(constructor);

                // Crear el método Invoke del delegado
                MethodSig invokeSig = new MethodSig(CallingConvention.HasThis, pinvokeMethod.MethodSig.GenParamCount, pinvokeMethod.MethodSig.RetType, pinvokeMethod.MethodSig.Params);
                MethodDef invokeMethod = new MethodDefUser("Invoke", invokeSig);
                invokeMethod.Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
                delegateType.Methods.Add(invokeMethod);

                // Crear un campo estático en la clase que contiene el método P/Invoke
                fieldToStoreDelegate = new FieldDefUser(pinvokeMethod.Name + "Field",
                    new FieldSig(module.CorLibTypes.GetTypeRef("System", "Object").ToTypeSig()),
                    FieldAttributes.Public | FieldAttributes.Static);
                declaringType.Fields.Add(fieldToStoreDelegate);
            }
            else
            {
                // Buscar el campo estático existente si no se crea uno nuevo
                fieldToStoreDelegate = declaringType.Fields.FirstOrDefault(f => f.Name == pinvokeMethod.Name + "Field");
            }

            // Crear el método DInvoke
            MethodSig dinvokeSig = new MethodSig(CallingConvention.Default, pinvokeMethod.MethodSig.GenParamCount, pinvokeMethod.MethodSig.RetType, pinvokeMethod.MethodSig.Params);
            MethodDef dinvokeMethod = new MethodDefUser(pinvokeMethod.Name, dinvokeSig)
            {
                Attributes = MethodAttributes.Public | MethodAttributes.Static
            };
            declaringType.Methods.Add(dinvokeMethod);

            // Reemplazar el cuerpo del método DInvoke
            dinvokeMethod.Body = new CilBody();
            var instructions = dinvokeMethod.Body.Instructions;

            instructions.Add(OpCodes.Ldstr.ToInstruction(pinvokeInfo.Module.Name));
            instructions.Add(OpCodes.Ldstr.ToInstruction(pinvokeInfo.Name));
            instructions.Add(OpCodes.Call.ToInstruction(CallMethod));

            // Verificar si el campo existe antes de usarlo
            if (fieldToStoreDelegate != null)
            {
                instructions.Add(OpCodes.Stsfld.ToInstruction(fieldToStoreDelegate));
            }
            else
            {
                //throw new InvalidOperationException("Field for storing delegate was not found or created.");
            }

            instructions.Add(OpCodes.Ret.ToInstruction());
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
