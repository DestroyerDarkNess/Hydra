using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{
    public class SUFconfusion : Models.Protection
    {
        public SUFconfusion() : base("Protection.Misc.SUFconfusion", "Fake Obfuscation", "Description for Renamer Phase") { }

        static string[] attrib = { "ObfuscatedByGoliath", "NineRays.Obfuscator.Evaluation", "NetGuard", "dotNetProtector", "YanoAttribute", "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode", "PoweredByAttribute", "DotNetPatcherPackerAttribute", "DotNetPatcherObfuscatorAttribute", "DotfuscatorAttribute", "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute", "BabelObfuscatorAttribute", "BabelAttribute", "AssemblyInfoAttribute", "ZYXDNGuarder", "ConfusedByAttribute", "HydraProtectorAttribute" };

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                var random = new Random();

                foreach (var type in module.Types)
                {
                    if (!Core.Analyzer.CanRename(type)) continue;

                    // Añadir un atributo de la lista al tipo
                    var attributeName = attrib[random.Next(attrib.Length)];
                    var attributeRef = new TypeRefUser(module, "System", attributeName, module.CorLibTypes.AssemblyRef);
                    var attributeCtor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attributeRef);
                    var customAttribute = new CustomAttribute(attributeCtor);
                    type.CustomAttributes.Add(customAttribute);

                    foreach (var meth in type.Methods)
                    {
                        if (meth == null || !meth.HasBody) continue;
                        if (!Core.Analyzer.CanRename(meth)) continue;

                        var body = meth.Body;
                        var target = body.Instructions[0];

                        // Instrucciones no operativas (no-ops)
                        var nopInstruction = Instruction.Create(OpCodes.Nop);
                        body.Instructions.Insert(0, nopInstruction);

                        // Instrucciones redundantes
                        var loadInstruction = Instruction.Create(OpCodes.Ldc_I4, 42); // Cargar un valor en la pila
                        var popInstruction = Instruction.Create(OpCodes.Pop); // Descartar el valor de la pila
                        body.Instructions.Insert(1, loadInstruction);
                        body.Instructions.Insert(2, popInstruction);

                        // Saltos innecesarios
                        var jumpInstruction = Instruction.Create(OpCodes.Br_S, body.Instructions[3]);
                        body.Instructions.Insert(3, jumpInstruction);

                        // Código muerto

                        var deadCodeInstruction = Instruction.Create(OpCodes.Ldstr, "DeadCode");
                        body.Instructions.Insert(4, deadCodeInstruction);
                        body.Instructions.Insert(5, Instruction.Create(OpCodes.Pop));

                        // Actualizar manejadores de excepciones
                        foreach (var handler in body.ExceptionHandlers)
                        {
                            if (handler.TryStart == target)
                            {
                                handler.TryStart = nopInstruction;
                            }
                            else if (handler.HandlerStart == target)
                            {
                                handler.HandlerStart = nopInstruction;
                            }
                            else if (handler.FilterStart == target)
                            {
                                handler.FilterStart = nopInstruction;
                            }
                        }

                        // Añadir un atributo de la lista al método
                        attributeName = attrib[random.Next(attrib.Length)];
                        attributeRef = new TypeRefUser(module, "System", attributeName, module.CorLibTypes.AssemblyRef);
                        attributeCtor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attributeRef);
                        customAttribute = new CustomAttribute(attributeCtor);
                        meth.CustomAttributes.Add(customAttribute);
                    }
                }

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
