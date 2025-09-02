using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.INT
{
    /// <summary>
    /// Protection.Enum.EnumStripper
    /// Elimina enums definidos por el usuario y reemplaza todas sus referencias
    /// por el tipo primitivo subyacente (I4/I8/U1/etc.).
    /// </summary>
    public class EnumStripper : Models.Protection
    {
        public EnumStripper()
            : base("Protection.Enum.EnumStripper", "IL Rewrite Phase",
                   "Remove user-defined enums replacing all references with underlying integer types.")
        {
        }

        public bool RemoveNestedEnums { get; set; } = true;

        private class EnumInfo
        {
            public TypeDef EnumType { get; set; }
            public CorLibTypeSig UnderlyingSig { get; set; }
            public ITypeDefOrRef UnderlyingTypeRef { get; set; }
        }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                var enumInfos = CollectUserEnums(module);
                if (enumInfos.Count == 0)
                    return true;

                ReplaceAllSignatures(module, enumInfos);
                RewriteInstructionOperands(module, enumInfos);
                RewriteOrStripEnumCustomAttributes(module, enumInfos);
                RemoveEnumTypes(module, enumInfos);

                foreach (var type in module.Types)
                {
                    foreach (var m in type.Methods.Where(mm => mm.HasBody))
                    {
                        m.Body.Instructions.SimplifyMacros(m.Body.Variables, m.Parameters);
                        m.Body.Instructions.SimplifyBranches();
                        m.Body.Instructions.OptimizeBranches();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                this.Errors = ex;
                return false;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        // --------------------------------
        // 1) Recolectar enums de usuario
        // --------------------------------
        private Dictionary<TypeDef, EnumInfo> CollectUserEnums(ModuleDefMD module)
        {
            var map = new Dictionary<TypeDef, EnumInfo>();

            foreach (var t in module.GetTypes())
            {
                if (!IsUserEnum(t)) continue;

                var valueField = t.Fields.FirstOrDefault(f => f.IsSpecialName && f.Name == "value__");
                if (valueField == null) continue;

                var vt = valueField.FieldSig.Type.ElementType;
                CorLibTypeSig underlyingSig;

                switch (vt)
                {
                    case ElementType.I1: underlyingSig = module.CorLibTypes.SByte; break;
                    case ElementType.U1: underlyingSig = module.CorLibTypes.Byte; break;
                    case ElementType.I2: underlyingSig = module.CorLibTypes.Int16; break;
                    case ElementType.U2: underlyingSig = module.CorLibTypes.UInt16; break;
                    case ElementType.I4: underlyingSig = module.CorLibTypes.Int32; break;
                    case ElementType.U4: underlyingSig = module.CorLibTypes.UInt32; break;
                    case ElementType.I8: underlyingSig = module.CorLibTypes.Int64; break;
                    case ElementType.U8: underlyingSig = module.CorLibTypes.UInt64; break;
                    default: underlyingSig = module.CorLibTypes.Int32; break;
                }

                var info = new EnumInfo();
                info.EnumType = t;
                info.UnderlyingSig = underlyingSig;
                info.UnderlyingTypeRef = underlyingSig.ToTypeDefOrRef();

                map[t] = info;
            }

            return map;
        }

        private bool IsUserEnum(TypeDef t)
        {
            if (t == null) return false;
            if (!t.IsEnum) return false;
            if (t.IsInterface || t.IsGlobalModuleType) return false;
            if (!RemoveNestedEnums && t.IsNested) return false;
            if (t.BaseType == null) return false;
            return true;
        }

        // ---------------------------------------------------
        // 2) Reemplazar tipos enum en TODAS las firmas
        // ---------------------------------------------------
        private void ReplaceAllSignatures(ModuleDefMD module, Dictionary<TypeDef, EnumInfo> enums)
        {
            // Campos
            foreach (var type in module.GetTypes())
            {
                foreach (var field in type.Fields)
                {
                    if (field.FieldSig != null)
                        field.FieldSig.Type = ReplaceTypeSig(field.FieldSig.Type, enums);
                }
            }

            // Métodos (ret/params/locals)
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.MethodSig != null)
                    {
                        method.MethodSig.RetType = ReplaceTypeSig(method.MethodSig.RetType, enums);
                        for (int i = 0; i < method.MethodSig.Params.Count; i++)
                            method.MethodSig.Params[i] = ReplaceTypeSig(method.MethodSig.Params[i], enums);
                    }

                    if (method.HasBody)
                    {
                        foreach (var local in method.Body.Variables)
                            local.Type = ReplaceTypeSig(local.Type, enums);
                    }
                }
            }

            // Propiedades
            foreach (var type in module.GetTypes())
            {
                foreach (var prop in type.Properties)
                {
                    if (prop.PropertySig != null)
                    {
                        prop.PropertySig.RetType = ReplaceTypeSig(prop.PropertySig.RetType, enums);
                        for (int i = 0; i < prop.PropertySig.Params.Count; i++)
                            prop.PropertySig.Params[i] = ReplaceTypeSig(prop.PropertySig.Params[i], enums);
                    }
                }
            }

            // Eventos
            foreach (var type in module.GetTypes())
            {
                foreach (var evt in type.Events)
                {
                    if (evt.EventType != null)
                        evt.EventType = ReplaceITypeDefOrRef(evt.EventType, enums);
                }
            }

            // Restricciones genéricas
            foreach (var type in module.GetTypes())
            {
                foreach (var gp in type.GenericParameters)
                {
                    foreach (var c in gp.GenericParamConstraints)
                        if (c.Constraint != null)
                            c.Constraint = ReplaceITypeDefOrRef(c.Constraint, enums);
                }

                foreach (var method in type.Methods)
                {
                    foreach (var gp in method.GenericParameters)
                    {
                        foreach (var c in gp.GenericParamConstraints)
                            if (c.Constraint != null)
                                c.Constraint = ReplaceITypeDefOrRef(c.Constraint, enums);
                    }
                }
            }
        }

        private TypeSig ReplaceTypeSig(TypeSig sig, Dictionary<TypeDef, EnumInfo> enums)
        {
            if (sig == null) return sig;

            switch (sig.ElementType)
            {
                case ElementType.Class:
                case ElementType.ValueType:
                    {
                        var tdrSig = sig as TypeDefOrRefSig;
                        if (tdrSig != null)
                        {
                            var tdr = tdrSig.TypeDefOrRef;
                            var td = tdr.ResolveTypeDef();
                            EnumInfo info;
                            if (td != null && enums.TryGetValue(td, out info))
                                return info.UnderlyingSig;
                        }
                        return sig;
                    }

                case ElementType.Ptr:
                    return new PtrSig(ReplaceTypeSig(sig.Next, enums));

                case ElementType.ByRef:
                    return new ByRefSig(ReplaceTypeSig(sig.Next, enums));

                case ElementType.SZArray:
                    return new SZArraySig(ReplaceTypeSig(sig.Next, enums));

                case ElementType.Array:
                    {
                        var arr = sig as ArraySig;
                        if (arr != null)
                            return new ArraySig(ReplaceTypeSig(arr.Next, enums), arr.Rank, arr.Sizes, arr.LowerBounds);
                        return sig;
                    }

                case ElementType.GenericInst:
                    {
                        var gi = sig as GenericInstSig;
                        if (gi == null) return sig;

                        var newArgs = new List<TypeSig>(gi.GenericArguments.Count);
                        bool changed = false;
                        for (int i = 0; i < gi.GenericArguments.Count; i++)
                        {
                            var a = gi.GenericArguments[i];
                            var na = ReplaceTypeSig(a, enums);
                            newArgs.Add(na);
                            if (!TypeSigEquals(a, na)) changed = true;
                        }
                        if (!changed) return gi;
                        return new GenericInstSig(gi.GenericType, newArgs);
                    }

                case ElementType.CModReqd:
                    {
                        var msig = sig as CModReqdSig;
                        if (msig != null)
                        {
                            var modType = ReplaceITypeDefOrRef(msig.Modifier, enums);
                            return new CModReqdSig(modType, ReplaceTypeSig(msig.Next, enums));
                        }
                        return sig;
                    }

                case ElementType.CModOpt:
                    {
                        var msig = sig as CModOptSig;
                        if (msig != null)
                        {
                            var modType = ReplaceITypeDefOrRef(msig.Modifier, enums);
                            return new CModOptSig(modType, ReplaceTypeSig(msig.Next, enums));
                        }
                        return sig;
                    }

                default:
                    return sig;
            }
        }

        private ITypeDefOrRef ReplaceITypeDefOrRef(ITypeDefOrRef tdr, Dictionary<TypeDef, EnumInfo> enums)
        {
            if (tdr == null) return tdr;
            var td = tdr.ResolveTypeDef();
            EnumInfo info;
            if (td != null && enums.TryGetValue(td, out info))
                return info.UnderlyingTypeRef;
            return tdr;
        }

        // ---------------------------------------------------
        // 3) Reescribir operandos IL que referencien los enums
        // ---------------------------------------------------
        private void RewriteInstructionOperands(ModuleDefMD module, Dictionary<TypeDef, EnumInfo> enums)
        {
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods.Where(m => m.HasBody && m.Body.HasInstructions))
                {
                    var instrs = method.Body.Instructions;

                    for (int i = 0; i < instrs.Count; i++)
                    {
                        var ins = instrs[i];

                        var tdr = ins.Operand as ITypeDefOrRef;
                        if (tdr != null)
                        {
                            var newTdr = ReplaceITypeDefOrRef(tdr, enums);
                            if (!object.ReferenceEquals(newTdr, tdr))
                                ins.Operand = newTdr;
                            continue;
                        }

                        var ts = ins.Operand as TypeSpec;
                        if (ts != null)
                        {
                            var newSig = ReplaceTypeSig(ts.TypeSig, enums);
                            if (!TypeSigEquals(ts.TypeSig, newSig))
                                ins.Operand = new TypeSpecUser(newSig);
                            continue;
                        }

                        var mr = ins.Operand as MemberRef;
                        if (mr != null)
                        {
                            // Cambiar DeclaringType si era enum
                            var decl = mr.DeclaringType;
                            var newDecl = ReplaceITypeDefOrRef(decl, enums);

                            MemberRef newMr = mr;
                            if (!object.ReferenceEquals(newDecl, decl))
                            {
                                if (mr.MethodSig != null)
                                    newMr = new MemberRefUser(module, mr.Name, mr.MethodSig, newDecl);
                                else if (mr.FieldSig != null)
                                    newMr = new MemberRefUser(module, mr.Name, mr.FieldSig, newDecl);

                                ins.Operand = newMr;
                            }

                            // Mapear firmas
                            var mSig = newMr.MethodSig as MethodSig;
                            if (mSig != null)
                            {
                                mSig.RetType = ReplaceTypeSig(mSig.RetType, enums);
                                for (int p = 0; p < mSig.Params.Count; p++)
                                    mSig.Params[p] = ReplaceTypeSig(mSig.Params[p], enums);
                            }
                            else if (newMr.FieldSig != null)
                            {
                                newMr.FieldSig.Type = ReplaceTypeSig(newMr.FieldSig.Type, enums);
                            }
                            continue;
                        }

                        var mspec = ins.Operand as MethodSpec;
                        if (mspec != null)
                        {
                            // Instanciación genérica cerrada del método
                            var gims = mspec.Instantiation as GenericInstMethodSig;
                            if (gims != null)
                            {
                                bool changed = false;
                                for (int a = 0; a < gims.GenericArguments.Count; a++)
                                {
                                    var orig = gims.GenericArguments[a];
                                    var na = ReplaceTypeSig(orig, enums);
                                    if (!TypeSigEquals(orig, na))
                                    {
                                        gims.GenericArguments[a] = na;
                                        changed = true;
                                    }
                                }
                                if (changed)
                                {
                                    // Crear MethodSpec nuevo por seguridad
                                    ins.Operand = new MethodSpecUser(mspec.Method, gims);
                                }
                            }
                            continue;
                        }
                    }

                    method.Body.Instructions.SimplifyMacros(method.Body.Variables, method.Parameters);
                    method.Body.Instructions.SimplifyBranches();
                    method.Body.Instructions.OptimizeBranches();
                }
            }
        }

        // -------------------------------------------------------------------
        // 4) CustomAttributes: mapear TypeSig en CAArgument/CANamedArgument
        // -------------------------------------------------------------------
        private void RewriteOrStripEnumCustomAttributes(ModuleDefMD module, Dictionary<TypeDef, EnumInfo> enums)
        {
            Func<CustomAttribute, bool> needsFix = (ca) =>
            {
                var mr = ca.Constructor as MemberRef;
                if (mr != null)
                {
                    var dt = mr.DeclaringType.ResolveTypeDef();
                    if (dt != null && enums.ContainsKey(dt))
                        return true;
                }

                for (int i = 0; i < ca.ConstructorArguments.Count; i++)
                    if (ArgUsesEnum(ca.ConstructorArguments[i].Type, enums)) return true;

                for (int i = 0; i < ca.NamedArguments.Count; i++)
                    if (ArgUsesEnum(ca.NamedArguments[i].Type, enums)) return true;

                return false;
            };

            Action<IHasCustomAttribute> tryMapOrRemove = (owner) =>
            {
                if (owner == null || !owner.HasCustomAttributes) return;

                for (int i = owner.CustomAttributes.Count - 1; i >= 0; i--)
                {
                    var ca = owner.CustomAttributes[i];
                    if (!needsFix(ca)) continue;

                    try
                    {
                        // Posicionales
                        for (int a = 0; a < ca.ConstructorArguments.Count; a++)
                        {
                            var arg = ca.ConstructorArguments[a];
                            if (ArgUsesEnum(arg.Type, enums))
                            {
                                var mapped = MapCAType(arg.Type, enums);
                                ca.ConstructorArguments[a] = new CAArgument(mapped, arg.Value);
                            }
                        }

                        // Nombrados
                        for (int a = 0; a < ca.NamedArguments.Count; a++)
                        {
                            var na = ca.NamedArguments[a];
                            if (ArgUsesEnum(na.Type, enums))
                            {
                                var mapped = MapCAType(na.Type, enums);
                                ca.NamedArguments[a] = new CANamedArgument(
                                    na.IsField,                 // bool isField
                                    mapped,                     // TypeSig type
                                    na.Name,                    // UTF8String name
                                    new CAArgument(mapped, na.Argument.Value) // CAArgument argument
                                );
                            }
                        }

                        // Constructor (declaring type)
                        var ctorMr = ca.Constructor as MemberRef;
                        if (ctorMr != null)
                        {
                            var decl = ctorMr.DeclaringType;
                            var repl = ReplaceITypeDefOrRef(decl, enums);
                            if (!object.ReferenceEquals(repl, decl))
                            {
                                var newCtor = new MemberRefUser(module, ctorMr.Name, ctorMr.MethodSig, repl);

                                // Ajustar firma del ctor por si usa enums en params
                                var ctorSig = newCtor.MethodSig as MethodSig;
                                if (ctorSig != null)
                                {
                                    ctorSig.RetType = ReplaceTypeSig(ctorSig.RetType, enums);
                                    for (int p = 0; p < ctorSig.Params.Count; p++)
                                        ctorSig.Params[p] = ReplaceTypeSig(ctorSig.Params[p], enums);
                                }

                                ca.Constructor = newCtor;
                            }
                        }
                    }
                    catch
                    {
                        owner.CustomAttributes.RemoveAt(i);
                    }
                }
            };

            foreach (var t in module.GetTypes())
            {
                tryMapOrRemove(t);
                foreach (var f in t.Fields) tryMapOrRemove(f);
                foreach (var m in t.Methods)
                {
                    tryMapOrRemove(m);
                    // OJO: ParamDefs, no Parameters
                    foreach (var pd in m.ParamDefs) tryMapOrRemove(pd);
                }
                foreach (var p in t.Properties) tryMapOrRemove(p);
                foreach (var e in t.Events) tryMapOrRemove(e);
            }
        }

        private bool ArgUsesEnum(TypeSig type, Dictionary<TypeDef, EnumInfo> enums)
        {
            if (type == null) return false;

            var cv = type as ClassOrValueTypeSig;
            if (cv != null)
            {
                var td = cv.TypeDefOrRef.ResolveTypeDef();
                if (td != null && enums.ContainsKey(td))
                    return true;
            }
            return false;
        }

        private TypeSig MapCAType(TypeSig type, Dictionary<TypeDef, EnumInfo> enums)
        {
            if (type == null) return type;

            var cv = type as ClassOrValueTypeSig;
            if (cv != null)
            {
                var td = cv.TypeDefOrRef.ResolveTypeDef();
                EnumInfo info;
                if (td != null && enums.TryGetValue(td, out info))
                {
                    // Sustituir por el tipo primitivo subyacente
                    return info.UnderlyingSig;
                }
            }
            return type;
        }

        // --------------------------------
        // 5) Eliminar los TypeDef de enum
        // --------------------------------
        private void RemoveEnumTypes(ModuleDefMD module, Dictionary<TypeDef, EnumInfo> enums)
        {
            foreach (var kv in enums)
            {
                var enumType = kv.Key;
                if (enumType.IsNested && enumType.DeclaringType != null)
                    enumType.DeclaringType.NestedTypes.Remove(enumType);
                else
                    module.Types.Remove(enumType);
            }
        }

        // -------------------------
        // Utils
        // -------------------------
        private bool TypeSigEquals(TypeSig a, TypeSig b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a == null || (object)b == null) return false;
            return a.GetFullName() == b.GetFullName();
        }
    }
}