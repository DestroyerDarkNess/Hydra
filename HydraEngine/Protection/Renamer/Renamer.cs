using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class Renamer : Models.Protection
    {
        private Random Random = new Random();

        public Renamer() : base("Protection.Renamer", "Renamer Phase", "Description for Renamer Phase")
        {
        }

        public string tag { get; set; } = string.Empty;
        public int Length { get; set; } = 20;
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public bool ApplyCompilerGeneratedAttribute { get; set; } = false;

        public bool Properties { get; set; } = false;

        public bool Fields { get; set; } = false;

        public bool Events { get; set; } = false;

        public bool Methods { get; set; } = false;
        public bool Parameters { get; set; } = false;
        public bool ClassName { get; set; } = false;
        public bool Namespace { get; set; } = false;
        public bool NamespaceEmpty { get; set; } = false;
        public bool Resources { get; set; } = false;
        public bool ModuleRenaming { get; set; } = false;
        public bool ModuleInvisible { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                GGeneration.BaseChars = BaseChars;
                GGeneration.Length = Length;
                GGeneration.Custom = tag;

                GGeneration.CustomRN = !string.IsNullOrEmpty(tag);

                if (ModuleRenaming)
                {
                    foreach (ModuleDef module in Module.Assembly.Modules)
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
                        isWpf = Analyzer.IsWpfModule(Module);
                        if (!isWpf)
                        {
                            if (ModuleInvisible == false)
                            {
                                module.Name = GGeneration.GenerateGuidStartingWithLetter();

                                module.EncId = Guid.NewGuid();
                                module.EncBaseId = Guid.NewGuid();

                                module.Assembly.CustomAttributes.Clear();
                                module.Mvid = Guid.NewGuid();
                                module.Assembly.Name = GGeneration.GenerateGuidStartingWithLetter();
                                module.Assembly.Version = new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9));
                            }
                            else
                            {
                                module.Name = "";

                                module.EncId = Guid.NewGuid();
                                module.EncBaseId = Guid.NewGuid();

                                module.Assembly.CustomAttributes.Clear();
                                module.Mvid = Guid.NewGuid();
                                module.Assembly.Name = "";
                                module.Assembly.Version = new Version(Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9), Random.Next(1, 9));
                            }
                        }
                    }
                }

                foreach (TypeDef type in Module.Types)
                {
                    if (Properties)
                    {
                        foreach (PropertyDef property in type.Properties)
                        {
                            if (AnalyzerPhase.CanRename(type, property))
                            {
                                property.Name = GGeneration.GenerateGuidStartingWithLetter();
                            }
                        }
                    }
                    if (Fields)
                    {
                        foreach (FieldDef field in type.Fields)
                        {
                            if (AnalyzerPhase.CanRename(type, field))
                            {
                                field.Name = GGeneration.GenerateGuidStartingWithLetter();
                            }
                        }
                    }
                    if (Events)
                    {
                        foreach (EventDef @event in type.Events)
                        {
                            if (AnalyzerPhase.CanRename(@event))
                            {
                                @event.Name = GGeneration.GenerateGuidStartingWithLetter();
                            }
                        }
                    }
                    if (Methods)
                    {
                        foreach (MethodDef method in type.Methods)
                        {
                            if (AnalyzerPhase.CanRename(method, type))
                            {
                                method.Name = GGeneration.GenerateGuidStartingWithLetter();
                            }
                        }
                    }
                    if (Parameters)
                    {
                        foreach (MethodDef method2 in type.Methods)
                        {
                            foreach (Parameter parameter in method2.Parameters)
                            {
                                foreach (GenericParam genericParameter in type.GenericParameters)
                                {
                                    if (AnalyzerPhase.CanRename(type, parameter))
                                    {
                                        genericParameter.Name = GGeneration.GenerateGuidStartingWithLetter();
                                    }
                                    parameter.Name = GGeneration.GenerateGuidStartingWithLetter();
                                }
                            }
                        }
                    }
                    if (!ClassName || !AnalyzerPhase.CanRename(type))
                    {
                        continue;
                    }

                    if (ApplyCompilerGeneratedAttribute)
                    {
                        var compilerGeneratedAttributeRef = Module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");

                        var compilerGeneratedAttribute = new MemberRefUser(Module, ".ctor",
                            MethodSig.CreateInstance(Module.CorLibTypes.Void),
                            compilerGeneratedAttributeRef);

                        HydraEngine.Core.InjectHelper.AddAttributeToType(type, compilerGeneratedAttribute);
                    }

                    string text = GGeneration.GenerateGuidStartingWithLetter();
                    string text2 = GGeneration.GenerateGuidStartingWithLetter();
                    foreach (MethodDef method3 in type.Methods)
                    {
                        if (type.BaseType != null && type.BaseType.FullName.ToLower().Contains("form"))
                        {
                            foreach (Resource resource in Module.Resources)
                            {
                                if (resource.Name.Contains(string.Concat(type.Name, ".resources")))
                                {
                                    resource.Name = text + "." + text2 + ".resources";
                                }
                            }
                        }
                        if (Namespace)
                        {
                            if (NamespaceEmpty)
                            {
                                type.Namespace = "";
                            }
                            else
                            {
                                type.Namespace = text;
                            }
                        }

                        if (ClassName)
                            type.Name = text2;

                        if (!method3.Name.Equals("InitializeComponent") || !method3.HasBody)
                        {
                            continue;
                        }
                        foreach (Instruction instruction in method3.Body.Instructions)
                        {
                            if (instruction.OpCode.Equals(OpCodes.Ldstr))
                            {
                                string text3 = (string)instruction.Operand;
                                if (text3 == type.Name)
                                {
                                    instruction.Operand = text2;
                                    break;
                                }
                            }
                        }
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

    public class GGeneration
    {
        private static readonly HashSet<string> stored = new HashSet<string>();

        private static Random random = new Random();

        public static int Length { get; set; } = 20;
        public static string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string Custom { get; set; } = "HailHydra";

        public static bool CustomRN { get; set; } = false;

        public static string RandomString(int length)
        {
            return new string((from s in Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", length)
                               select s[random.Next(s.Length)]).ToArray());
        }

        public static string RandomStringWithRandomLength()
        {
            int count = random.Next(5, 101);
            return new string((from s in Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", count)
                               select s[random.Next(s.Length)]).ToArray());
        }

        private static string MD5Hash(string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] array = mD5CryptoServiceProvider.ComputeHash(new UTF8Encoding().GetBytes(input));
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.Append(array[i].ToString("x2"));
            }
            return stringBuilder.ToString();
        }

        private static char GetLetter()
        {
            Random random = new Random();
            int num = random.Next(0, 25);
            return (char)(97 + num);
        }

        public static string GenerateRandomString()
        {
            Random random = new Random();
            string input = GenerateRandomString(random.Next(2, 24));
            string text = MD5Hash(input);
            if (char.IsDigit(text[0]))
            {
                char letter = GetLetter();
                text = text.Replace(text[0], letter);
            }
            return text;
        }

        public static string GenerateRandomString(int size)
        {
            string text = BaseChars;
            char[] array = text.ToCharArray();
            byte[] data = new byte[1];
            RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
            rNGCryptoServiceProvider.GetNonZeroBytes(data);
            data = new byte[size];
            rNGCryptoServiceProvider.GetNonZeroBytes(data);
            StringBuilder stringBuilder = new StringBuilder(size);
            byte[] array2 = data;
            foreach (byte b in array2)
            {
                stringBuilder.Append(array[b % array.Length]);
            }
            return stringBuilder.ToString();
        }

        public static string GenerateGuidStartingWithLetter()
        {
            string text;
            do
            {
                text = (CustomRN ? string.Concat(Custom, "_" + GenerateRandomString()) : GenerateRandomString());
            }
            while (!stored.Add(text));
            return text;
        }
    }

    public class AnalyzerPhase
    {
        private static bool HasHydraNoObfuscateAttribute(IHasCustomAttribute element)
        {
            if (element?.CustomAttributes == null)
                return false;

            return element.CustomAttributes.Any(attr =>
                attr.AttributeType?.Name?.Contains("HydraNoObfuscate") == true ||
                attr.AttributeType?.FullName?.Contains("HydraNoObfuscate") == true);
        }

        public static bool CanRename(TypeDef type)
        {
            try
            {
                if (HasHydraNoObfuscateAttribute(type))
                {
                    return false;
                }

                if (type.Name == "HydraNoObfuscate" || type.Name == "HydraNoObfuscateAttribute")
                {
                    return false;
                }

                if (type.FullName == "TrinityAttribute")
                {
                    return false;
                }
                if (type.Namespace == "Costura")
                {
                    return false;
                }
                if (type.Name.StartsWith("<"))
                {
                    return false;
                }
                if (type.IsGlobalModuleType)
                {
                    return false;
                }
                if (type.IsInterface)
                {
                    return false;
                }
                if (type.IsForwarder)
                {
                    return false;
                }
                if (type.IsSerializable)
                {
                    return false;
                }
                if (type.IsEnum)
                {
                    return false;
                }
                if (type.IsRuntimeSpecialName)
                {
                    return false;
                }
                if (type.IsSpecialName)
                {
                    return false;
                }
                if (type.IsWindowsRuntime)
                {
                    return false;
                }
                if (type.IsNestedFamilyOrAssembly)
                {
                    return false;
                }
                if (type.IsNestedFamilyAndAssembly)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRename(EventDef e)
        {
            try
            {
                if (HasHydraNoObfuscateAttribute(e))
                {
                    return false;
                }

                if (HasHydraNoObfuscateAttribute(e.DeclaringType))
                {
                    return false;
                }

                if (e.IsSpecialName)
                {
                    return false;
                }
                if (e.IsRuntimeSpecialName)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRename(TypeDef type, PropertyDef p)
        {
            try
            {
                // Verificar si la propiedad tiene el atributo HydraNoObfuscate
                if (HasHydraNoObfuscateAttribute(p))
                {
                    return false;
                }

                // También verificar si el tipo declarante tiene el atributo
                if (HasHydraNoObfuscateAttribute(type))
                {
                    return false;
                }

                if (p.DeclaringType.Implements("System.ComponentModel.INotifyPropertyChanged"))
                {
                    return false;
                }
                if (type.Namespace.String.Contains(".Properties"))
                {
                    return false;
                }
                if (p.DeclaringType.Name.String.Contains("AnonymousType"))
                {
                    return false;
                }
                if (p.IsRuntimeSpecialName)
                {
                    return false;
                }
                if (p.IsEmpty)
                {
                    return false;
                }
                if (p.IsSpecialName)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRename(TypeDef type, FieldDef field)
        {
            try
            {
                if (HasHydraNoObfuscateAttribute(field))
                {
                    return false;
                }

                if (HasHydraNoObfuscateAttribute(type))
                {
                    return false;
                }

                if (field.DeclaringType == null)
                {
                    return false;
                }

                if (field.DeclaringType.BaseType == null)
                {
                    return false;
                }

                if (field.DeclaringType.IsSerializable && !field.IsNotSerialized)
                {
                    return false;
                }
                if (field.DeclaringType.BaseType.Name.Contains("Delegate"))
                {
                    return false;
                }
                if (field.Name.StartsWith("<"))
                {
                    return false;
                }
                if (field.IsLiteral && field.DeclaringType.IsEnum)
                {
                    return false;
                }
                if (field.IsFamilyOrAssembly)
                {
                    return false;
                }
                if (field.IsSpecialName)
                {
                    return false;
                }
                if (field.IsRuntimeSpecialName)
                {
                    return false;
                }
                if (field.IsFamily)
                {
                    return false;
                }
                if (field.DeclaringType.IsEnum)
                {
                    return false;
                }
                if (field.DeclaringType.BaseType.Name.Contains("Delegate"))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRename(MethodDef method, TypeDef type)
        {
            try
            {
                if (HasHydraNoObfuscateAttribute(method))
                {
                    return false;
                }

                if (HasHydraNoObfuscateAttribute(type))
                {
                    return false;
                }

                if (method.DeclaringType.IsComImport() && !method.HasAttribute("System.Runtime.InteropServices.DispIdAttribute"))
                {
                    return false;
                }
                if (!method.HasBody || !method.Body.HasInstructions)
                {
                    return false;
                }
                if (method.DeclaringType.BaseType != null && method.DeclaringType.BaseType.Name.Contains("Delegate"))
                {
                    return false;
                }
                if (method.DeclaringType.IsDelegate())
                {
                    return false;
                }
                if (method.DeclaringType.FullName == "System.Windows.Forms.Binding" && method.Name.String == ".ctor")
                {
                    return false;
                }
                if (method.DeclaringType.FullName == "System.Windows.Forms.ControlBindingsCollection")
                {
                    return false;
                }
                if (method.DeclaringType.FullName == "System.Windows.Forms.BindingsCollection")
                {
                    return false;
                }
                if (method.DeclaringType.FullName == "System.Windows.Forms.DataGridViewColumn")
                {
                    return false;
                }
                if (method.Name == "Invoke")
                {
                    return false;
                }
                if (method.IsSetter || method.IsGetter)
                {
                    return false;
                }
                if (method.IsSpecialName)
                {
                    return false;
                }
                if (method.IsFamilyAndAssembly)
                {
                    return false;
                }
                if (method.IsFamily)
                {
                    return false;
                }
                if (method.IsRuntime)
                {
                    return false;
                }
                if (method.IsRuntimeSpecialName)
                {
                    return false;
                }
                if (method.IsConstructor)
                {
                    return false;
                }
                if (method.IsNative)
                {
                    return false;
                }
                if (method.IsPinvokeImpl || method.IsUnmanaged || method.IsUnmanagedExport)
                {
                    return false;
                }
                if (method == null)
                {
                    return false;
                }
                if (method.Name.StartsWith("<"))
                {
                    return false;
                }
                if (method.Overrides.Count > 0)
                {
                    return false;
                }
                if (method.IsStaticConstructor)
                {
                    return false;
                }
                if (method.DeclaringType.IsGlobalModuleType)
                {
                    return false;
                }
                if (method.DeclaringType.IsForwarder)
                {
                    return false;
                }
                if (method.IsVirtual)
                {
                    return false;
                }
                if (method.HasImplMap)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRename(TypeDef type, Parameter p)
        {
            try
            {
                if (p.ParamDef != null && HasHydraNoObfuscateAttribute(p.ParamDef))
                {
                    return false;
                }

                if (HasHydraNoObfuscateAttribute(type))
                {
                    return false;
                }

                if (type.FullName == "<Module>")
                {
                    return false;
                }
                if (p.IsHiddenThisParameter)
                {
                    return false;
                }
                if (p.Name == string.Empty)
                {
                    return false;
                }
                return true;
            }
            catch { return false; }
        }
    }
}