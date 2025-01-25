using dnlib.DotNet;
using VM.Core.Helper;
using System.Linq;
using HydraEngine.Protection.VM;

namespace VM.Core.Protections.Impl.Virtualization.RuntimeProtections
{
    internal static class Renamer
    {
        public static void Execute(ModuleDefMD mod)
        {
            foreach (var type in mod.GetTypes().Where(t => !t.IsGlobalModuleType).ToArray())
            {
                type.Namespace = string.Empty;
                type.Name = Generator.RandomName();

                foreach (MethodDef method in type.Methods.Where(m => !m.IsConstructor).ToArray())
                    if (method.Name != Virtualizer.Instance.theMethod.Name)
                    {
                        foreach (var param in method.Parameters)
                            param.Name = Generator.RandomName();

                        if (!method.IsVirtual)
                            method.Name = Generator.RandomName();

                        if (method.IsAbstract)
                        {
                            var oldName = method.Name;
                            var oldParam1 = method.Parameters[0].Name;
                            var oldParam2 = method.Parameters[1].Name;
                            method.Name = Generator.RandomName();
                            method.Parameters[0].Name = Generator.RandomName();
                            method.Parameters[1].Name = Generator.RandomName();
                            foreach (var t in mod.Types.Where(t => !t.IsGlobalModuleType))
                                foreach (var m in t.Methods.Where(m => m.IsVirtual))
                                    if (m.Name == oldName)
                                    {
                                        m.Name = method.Name;
                                        m.Parameters[0].Name = method.Parameters[0].Name;
                                        m.Parameters[1].Name = method.Parameters[1].Name;
                                    }
                        }
                    }

                foreach (PropertyDef property in type.Properties)
                    property.Name = Generator.RandomName();

                foreach (FieldDef field in type.Fields)
                    field.Name = Generator.RandomName();

                foreach (EventDef thisEvent in type.Events)
                    thisEvent.Name = Generator.RandomName();

                foreach (var x in type.Interfaces)
                    x.Interface.Name = Generator.RandomName();
            }
        }
    }
}