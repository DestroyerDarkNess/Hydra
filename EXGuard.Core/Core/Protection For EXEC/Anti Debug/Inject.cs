using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    public static class AntiDebug_Inject
    {
        public static IList<MethodDef> Execute(ModuleDef module)
        {
            var typeDef = ModuleDefMD.Load(typeof(AntiDebug_Runtime).Module).ResolveTypeDef(MDToken.ToRID(typeof(AntiDebug_Runtime).MetadataToken));
            var members = Helpers.Injection.InjectHelper.Inject(typeDef, module.GlobalType, module);
            var init = members.OfType<MethodDef>().Single(method => method.Name == "Initialize");

            var methods = new HashSet<MethodDef>();
            methods.Add(init);
            methods.Add(members.OfType<MethodDef>().Single(method => method.Name == "Worker"));

            var Module_ctor = module.GlobalType.FindOrCreateStaticConstructor();
            Module_ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

            #region Rename Merged Methods
            ///////////////////////////////////////////////////////////////////////
            foreach (IDnlibDef def in members)
            {
                IMemberDef memberDef = def as IMemberDef;

                if ((memberDef as MethodDef) != null)
                    memberDef.Name = new NameService().NewName(memberDef.Name);
                else if ((memberDef as FieldDef) != null)
                    memberDef.Name = new NameService().NewName(memberDef.Name);
            }
            ///////////////////////////////////////////////////////////////////////
            #endregion

            return methods.ToList();
        }
    }
}
