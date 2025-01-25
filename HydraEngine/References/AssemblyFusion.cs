using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.References
{
    

    public class AssemblyFusion
    {

        //AssemblyFusion fusion = new AssemblyFusion();
        //fusion.FuseAssemblies(exePath, dllPath);

        public static bool FuseAssemblies(ModuleDefMD Original, ModuleDefMD dllModule)
        {
            if (!IsDotNetAssembly(dllModule))
            {
                Console.WriteLine("The DLL is not a .NET assembly and cannot be fused.");
                return false;
            }

            //if (IsObfuscated(dllModule))
            //{
            //    Console.WriteLine("The DLL is obfuscated and cannot be fused.");
            //    return false;
            //}

            FuseModuleIntoAssembly(Original, dllModule);

            RemoveReference(Original, dllModule);

            return true;
        }

        private static bool IsObfuscated(ModuleDefMD module)
        {
            // Simple heuristic to check if the assembly is obfuscated
            return module.Types.Any(t => t.Name.String.Contains("Confused") || t.Name.String.Contains("Obfus"));
        }

        private static bool IsDotNetAssembly(ModuleDefMD module)
        {
            return module.IsILOnly; // module.CorLibTypes != null;
        }

        private static void FuseModuleIntoAssembly(ModuleDefMD exeModule, ModuleDefMD dllModule)
        {
            foreach (var type in dllModule.Types)
            {
                if (type.FullName != "<Module>")
                {
                    // Import the type from the DLL module to the EXE module
                    var newType = new TypeDefUser(type.Namespace, type.Name);
                    exeModule.Types.Add(newType);
                    Console.WriteLine($"Fusing : {type.FullName}");
                    foreach (var method in type.Methods)
                    {
                        Console.WriteLine($"Method : {method.Name}");
                        var newMethod = new MethodDefUser(method.Name, method.MethodSig, method.ImplAttributes, method.Attributes);
                        newType.Methods.Add(newMethod);

                        // Copy the method body
                        if (method.HasBody)
                        {
                            var body = new CilBody();
                            foreach (var instr in method.Body.Instructions)
                            {
                                body.Instructions.Add(new Instruction(instr.OpCode, instr.Operand));
                            }
                            newMethod.Body = body;
                        }
                    }
                }
            }
        }

        private static void RemoveReference(ModuleDefMD exeModule, ModuleDefMD dllModule)
        {
            // Find the assembly reference to the DLL
            var assemblyRef = exeModule.GetAssemblyRefs().FirstOrDefault(ar => ar.Name == dllModule.Assembly.Name);

            if (assemblyRef != null)
            {
                // Remove the assembly reference
                exeModule.GetAssemblyRefs().ToList().Remove(assemblyRef);

                exeModule.Assembly.Modules.Remove(dllModule);
            }
        }
    }

}
