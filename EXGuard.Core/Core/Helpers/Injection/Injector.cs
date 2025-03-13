using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;

using EXGuard.Core.Services;

namespace EXGuard.Core.Helpers.Injection
{
    public class Injector
    {
        public ModuleDef TargetModule { get; }
        public Type RuntimeType { get; }
        public List<IDnlibDef> Members { get; }
        public Injector(ModuleDef targetModule, Type type, bool injectType = true)
        {
            TargetModule = targetModule;
            RuntimeType = type;
            Members = new List<IDnlibDef>();

            if (injectType)
                InjectType();
        }

        public void InjectType()
        {
            var typeModule = ModuleDefMD.Load(RuntimeType.Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(RuntimeType.MetadataToken));
            Members.AddRange(InjectHelper.Inject(typeDefs, TargetModule.GlobalType, TargetModule).ToList());
        }
        public IDnlibDef FindMember(string name)
        {
            foreach (var member in Members)
                if (member.Name == name)
                    return member;
            throw new Exception("Error to find member.");
        }

        public void Rename()
        {
            foreach (var mem in Members)
            {
                if (mem is MethodDef method)
                {
                    if (method.HasImplMap)
                        continue;
                    if (method.DeclaringType.IsDelegate)
                        continue;
                }
                mem.Name = new RandomGenerator().NextHexString(true);
            }
        }
    }
}
