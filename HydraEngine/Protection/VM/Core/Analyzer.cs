using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM.Core
{
    public static class Analyzer
    {
        private static string DontObfuscateAtrributeName = "HydraOperator_DontObfuscate";


        public static bool IsWpfModule(ModuleDefMD module)
        {
            bool referencesPresentationFramework = module.GetAssemblyRefs()
                .Any(assemblyRef => assemblyRef.Name == "PresentationFramework");
            return referencesPresentationFramework;
        }
        public static bool CanRename(TypeDef type)
        {
            if (type.Namespace == "Costura")
                return false;
            if (type.Name.StartsWith("<"))
                return false;
            if (type.IsGlobalModuleType)
                return false;
            if (type.IsInterface)
                return false;
            if (type.IsForwarder)
                return false;
            if (type.IsSerializable)
                return false;
            //if (type.IsAbstract)
            //    return false;
            if (type.IsEnum)
                return false;
            if (type.IsRuntimeSpecialName)
                return false;
            if (type.IsSpecialName)
                return false;
            if (type.IsWindowsRuntime)
                return false;
            if (type.IsNestedFamilyOrAssembly)
                return false;
            if (type.IsNestedFamilyAndAssembly)
                return false;
            if (string.Equals(type.Name, DontObfuscateAtrributeName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (HaveAttribute(type.CustomAttributes, DontObfuscateAtrributeName))
                return false;
            return true;
        }
        public static bool CanRename(EventDef e)
        {
            if (e.IsSpecialName)
                return false;
            if (e.IsRuntimeSpecialName)
                return false;
            if (HaveAttribute(e.CustomAttributes, DontObfuscateAtrributeName))
                return false;
            return true;
        }
        public static bool CanRename(TypeDef type, PropertyDef p)
        {
            if (type.Namespace.String.Contains(".Properties"))
                return false;
            if (p.DeclaringType.Name.Contains("AnonymousType"))
                return false;
            if (p.IsRuntimeSpecialName)
                return false;
            if (p.IsEmpty)
                return false;
            if (p.IsSpecialName)
                return false;
            if (HaveAttribute(p.CustomAttributes, DontObfuscateAtrributeName))
                return false;
            return true;
        }
        public static bool CanRename(TypeDef type, Parameter p)
        {
            if (type.FullName == "<Module>")
                return false;
            if (p.IsHiddenThisParameter)
                return false;
            if (p.Name == string.Empty)
                return false;
            return true;
        }
        public static bool CanRename(TypeDef type, FieldDef field)
        {
            if (type.Namespace.String.Contains(".Properties"))
                return false;
            if (field.DeclaringType.IsSerializable && !field.IsNotSerialized)
                return false;
            if (field.DeclaringType.BaseType != null && field.DeclaringType.BaseType.Name.Contains("Delegate"))
                return false;
            if (field.Name.StartsWith("<"))
                return false;
            if (field.IsLiteral && field.DeclaringType.IsEnum)
                return false;
            if (field.IsFamilyOrAssembly)
                return false;
            if (field.IsSpecialName)
                return false;
            if (field.IsRuntimeSpecialName)
                return false;
            if (field.IsFamily)
                return false;
            if (field.DeclaringType.IsEnum)
                return false;
            if (field.DeclaringType.BaseType != null && field.DeclaringType.BaseType.Name.Contains("Delegate"))
                return false;
            if (HaveAttribute(field.CustomAttributes, DontObfuscateAtrributeName))
                return false;
            return true;
        }
        public static bool CanRename(MethodDef method)
        {
            if (!method.HasBody || !method.Body.HasInstructions)
                return false;
            if (method.DeclaringType.BaseType != null)
            {
                if (method.DeclaringType.BaseType.Name.Contains("Delegate"))
                    return false;
            }
            if (method.DeclaringType.FullName == "System.Windows.Forms.Binding" && method.Name.String == ".ctor")
                return false;
            if (method.Name == "Invoke")
                return false;
            if (method.IsSetter || method.IsGetter)
                return false;
            if (method.IsSpecialName)
                return false;
            if (method.IsFamilyAndAssembly)
                return false;
            if (method.IsFamily)
                return false;
            if (method.IsRuntime)
                return false;
            if (method.IsRuntimeSpecialName)
                return false;
            if (method.IsConstructor)
                return false;
            if (method.IsNative)
                return false;
            if (method.IsPinvokeImpl || method.IsUnmanaged || method.IsUnmanagedExport)
                return false;
            if (method == null)
                return false;
            if (method.Name.StartsWith("<"))
                return false;
            if (method.Overrides.Count > 0)
                return false;
            if (method.IsStaticConstructor)
                return false;
            if (method.DeclaringType.IsGlobalModuleType)
                return false;
            if (method.DeclaringType.IsForwarder)
                return false;
            if (method.IsVirtual)
                return false;
            if (method.HasImplMap)
                return false;
            if (HaveAttribute(method.CustomAttributes, DontObfuscateAtrributeName))
                return false;
            return true;
        }

        private static bool HaveAttribute(CustomAttributeCollection attributes, string attributeName)
        {
            foreach (var customAttribute in attributes)
            {
                if (string.Equals(customAttribute.AttributeType.Name, attributeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(customAttribute.AttributeType.Name, attributeName + "Attribute", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
