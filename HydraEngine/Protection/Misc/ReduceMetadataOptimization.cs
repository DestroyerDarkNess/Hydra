using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{
     public class ReduceMetadataOptimization : Models.Protection
    {
        public ReduceMetadataOptimization() : base("Protection.Misc.ReduceMetadataOptimization", "Reduce Metadata", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
               
                foreach (IMemberDef typeDefEx in module.Types)
                {

                    IMemberDef memberDef = typeDefEx as IMemberDef;

                    TypeDef typeDef;

                    if ((typeDef = (memberDef as TypeDef)) != null && !this.IsTypePublic(typeDef))
                    {
                        if (typeDef.IsEnum)
                        {
                            int num = 0;
                            while (typeDef.Fields.Count != 1)
                            {
                                if (typeDef.Fields[num].Name != "value__")
                                {
                                    typeDef.Fields.RemoveAt(num);
                                }
                                else
                                {
                                    num++;
                                }
                            }
                        }
                    }
                    else if (memberDef is EventDef)
                    {
                        if (memberDef.DeclaringType != null)
                        {
                            memberDef.DeclaringType.Events.Remove(memberDef as EventDef);
                        }
                    }
                    else if (memberDef is PropertyDef && memberDef.DeclaringType != null)
                    {
                        memberDef.DeclaringType.Properties.Remove(memberDef as PropertyDef);
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

        private bool IsTypePublic(TypeDef type)
        {
            while (type.IsPublic || type.IsNestedFamily || type.IsNestedFamilyAndAssembly || type.IsNestedFamilyOrAssembly || type.IsNestedPublic || type.IsPublic)
            {
                type = type.DeclaringType;
                if (type == null)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
