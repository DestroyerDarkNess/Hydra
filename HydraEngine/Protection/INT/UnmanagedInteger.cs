using AsmResolver.DotNet.Code.Native;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;

namespace HydraEngine.Protection.INT
{
    public class UnmanagedInteger : Models.Protection
    {
        public UnmanagedInteger()
            : base("Protection.Renamer.UnmanagedInteger",
                   "Renamer Phase (Integers)",
                   "Reemplaza cargas de enteros (ldc.i4) por métodos nativos.")
        {
            ManualReload = true;
        }

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                var module = ModuleDefinition.FromFile(moduledef);

                // Asegurarse de que el módulo permita código nativo.
                // Removemos ILOnly y configuramos PEKind y MachineType apropiadamente.
                module.Attributes &= ~DotNetDirectoryFlags.ILOnly;

                bool isx86 = module.MachineType == MachineType.I386;
                if (isx86)
                {
                    module.PEKind = OptionalHeaderMagic.PE32;
                    module.MachineType = MachineType.I386;
                    module.Attributes |= DotNetDirectoryFlags.Bit32Required;
                }
                else
                {
                    module.PEKind = OptionalHeaderMagic.PE32Plus;
                    module.MachineType = MachineType.Amd64;
                }

                // Usamos un diccionario para reusar métodos nativos si se repite el mismo valor entero.
                var encodedIntegers = new Dictionary<int, MethodDefinition>();

                // Recorremos todos los tipos y métodos.
                foreach (var type in module.GetAllTypes().ToArray())
                {
                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method == null)
                            continue;

                        if (!method.HasMethodBody || method.CilMethodBody == null)
                            continue;

                        var instructions = method.CilMethodBody.Instructions;
                        if (instructions.Count == 0)
                            continue;

                        // Recorremos las instrucciones buscando ldc.i4
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            var instr = instructions[i];

                            // Tratamos de extraer el entero según el opcode
                            if (IsLoadIntInstruction(instr.OpCode))
                            {
                                Console.WriteLine($"[+] Encontrado ldc.i4 en {method.FullName} en la instrucción {i}");
                                int intValue = ExtractIntValue(instr);

                                // Creamos/reutilizamos método nativo para devolver este entero.
                                if (!encodedIntegers.TryGetValue(intValue, out var nativeMethod))
                                {
                                    nativeMethod = CreateNativeMethodForInt(intValue, module, isx86);
                                    encodedIntegers.Add(intValue, nativeMethod);
                                }

                                // Reemplazamos la instrucción de carga por una llamada al método nativo.
                                instructions[i] = new CilInstruction(CilOpCodes.Call, nativeMethod);
                            }
                        }
                    }
                }

                // Guardamos a un MemoryStream
                MemoryStream outputAssembly = new MemoryStream();
                module.Write(outputAssembly);

                TempModule = outputAssembly;

                if (TempModule == null) throw new Exception("MemoryStream is null");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                this.Errors = ex;
                return false;
            }
        }


        private static bool IsLoadIntInstruction(CilOpCode opCode)
        {
            // Para cubrir más variantes de ldc.i4, podrías ampliar esta lógica:
            return opCode == CilOpCodes.Ldc_I4 ||
                   opCode == CilOpCodes.Ldc_I4_S ||
                   opCode == CilOpCodes.Ldc_I4_0 ||
                   opCode == CilOpCodes.Ldc_I4_1 ||
                   opCode == CilOpCodes.Ldc_I4_2 ||
                   opCode == CilOpCodes.Ldc_I4_3 ||
                   opCode == CilOpCodes.Ldc_I4_4 ||
                   opCode == CilOpCodes.Ldc_I4_5 ||
                   opCode == CilOpCodes.Ldc_I4_6 ||
                   opCode == CilOpCodes.Ldc_I4_7 ||
                   opCode == CilOpCodes.Ldc_I4_8 ||
                   opCode == CilOpCodes.Ldc_I4_M1;
        }

        private static int ExtractIntValue(CilInstruction instruction)
        {
            // Dependiendo del opcode, extraemos el valor.
            // Ejemplos:
            switch (instruction.OpCode.Code)
            {
                case CilCode.Ldc_I4_0: return 0;
                case CilCode.Ldc_I4_1: return 1;
                case CilCode.Ldc_I4_2: return 2;
                case CilCode.Ldc_I4_3: return 3;
                case CilCode.Ldc_I4_4: return 4;
                case CilCode.Ldc_I4_5: return 5;
                case CilCode.Ldc_I4_6: return 6;
                case CilCode.Ldc_I4_7: return 7;
                case CilCode.Ldc_I4_8: return 8;
                case CilCode.Ldc_I4_M1: return -1;
                case CilCode.Ldc_I4_S:
                    return (sbyte)instruction.Operand; // Ldc.i4.s usa un operando sbyte.
                case CilCode.Ldc_I4:
                    return (int)instruction.Operand;   // Ldc.i4 usa un int32 como operando.
                default:
                    throw new NotSupportedException($"Opcode no soportado: {instruction.OpCode}");
            }
        }

        private static MethodDefinition CreateNativeMethodForInt(int value, ModuleDefinition module, bool isx86)
        {
            var factory = module.CorLibTypeFactory;

            // Firmas: public static int NombreRandom()
            var methodName = "NativeInt_" + Guid.NewGuid().ToString("N");
            var method = new MethodDefinition(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                MethodSignature.CreateStatic(factory.Int32));

            method.ImplAttributes |= MethodImplAttributes.Native
                                     | MethodImplAttributes.Unmanaged
                                     | MethodImplAttributes.PreserveSig;
            method.Attributes |= MethodAttributes.PInvokeImpl;

            // Inyectamos el método en el <Module> type del ensamblado.
            module.GetOrCreateModuleType().Methods.Add(method);

            // Generamos un cuerpo nativo muy simple:
            // Para x86:  B8 XX XX XX XX   -> mov eax, <value> (32 bits) 
            //            C3              -> ret
            //
            // Para x64, también es válido "B8 XX XX XX XX" (mov eax, imm32) + ret,
            // ya que en x64 el retorno en EAX también funciona (se extiende a RAX).
            byte[] nativeCode = CreateMovRetCode(value, isx86);

            var body = new NativeMethodBody(method)
            {
                Code = nativeCode
            };
            method.NativeMethodBody = body;

            Console.WriteLine($"[+] Creado método nativo: {methodName} con valor = {value}");
            return method;
        }

        private static byte[] CreateMovRetCode(int value, bool isx86)
        {
            // Opcodes:
            // 0xB8 + 4 bytes (LE) = mov eax, <inmediato>
            // 0xC3 = ret
            // Esto funciona igual en x86 y x64 para un int32 en EAX.
            var code = new List<byte>();

            // mov eax, value
            code.Add(0xB8);

            // Insertamos el valor en formato little-endian.
            byte[] intBytes = BitConverter.GetBytes(value);
            code.AddRange(intBytes);

            // ret
            code.Add(0xC3);

            return code.ToArray();
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string TempRenamer = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(TempRenamer); } catch (Exception Ex) { this.Errors = Ex; }

            return Execute(TempRenamer);
        }
    }
}
