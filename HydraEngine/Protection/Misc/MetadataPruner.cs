using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Meta
{
    /// <summary>
    /// Protection.Meta.MetadataPruner
    /// Podado de metadatos no esenciales:
    /// - Elimina atributos informativos (listas negras + lista blanca de preservación).
    /// - Limpia info de depuración (PDB) por método.
    /// - Blanquea nombres de parámetros (mantiene tipos/attrs).
    /// - Quita DeclSecurity si se habilita.
    /// - Métricas, logging y modo DryRun.
    /// Compatible con C# 7.3 y dnlib clásico.
    /// </summary>
    public class MetadataPruner : Models.Protection
    {
        public MetadataPruner()
            : base("Protection.Meta.MetadataPruner", "Metadata Phase",
                   "Remove non-essential metadata (attrs, debug info, param names, decl security).")
        { }

        // ===== Opciones =====
        /// <summary>Simular sin modificar, solo reportar.</summary>
        public bool DryRun { get; set; } = false;

        /// <summary>Eliminar info PDB (sequence points, scopes, etc.).</summary>
        public bool StripDebugInfo { get; set; } = true;

        /// <summary>Vaciar nombres de parámetros (mantener tipos/attrs).</summary>
        public bool StripParamNames { get; set; } = true;

        /// <summary>Eliminar atributos “ruido” de parámetros.</summary>
        public bool StripParamNoiseAttributes { get; set; } = true;

        /// <summary>Eliminar DeclSecurity (CAS) obsoleto.</summary>
        public bool StripDeclSecurity { get; set; } = true;

        /// <summary>Logging detallado por elemento.</summary>
        public bool Verbose { get; set; } = false;

        /// <summary>Atributos a remover a nivel de ensamblado/módulo.</summary>
        public readonly HashSet<string> RemoveAssemblyAttrNames = new HashSet<string>(StringComparer.Ordinal)
        {
            // Informativos / build
            "System.Diagnostics.DebuggableAttribute",
            "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
            "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
            "System.Reflection.AssemblyTitleAttribute",
            "System.Reflection.AssemblyDescriptionAttribute",
            "System.Reflection.AssemblyCompanyAttribute",
            "System.Reflection.AssemblyProductAttribute",
            "System.Reflection.AssemblyTrademarkAttribute",
            "System.Reflection.AssemblyConfigurationAttribute",
            "System.Reflection.AssemblyFileVersionAttribute",
            "System.Reflection.AssemblyInformationalVersionAttribute",
            "System.Reflection.AssemblyCopyrightAttribute",
        };

        /// <summary>Atributos a remover en tipos/miembros/params.</summary>
        public readonly HashSet<string> RemoveMemberAttrNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Diagnostics.DebuggerStepThroughAttribute",
            "System.Diagnostics.DebuggerNonUserCodeAttribute",
            "System.Diagnostics.DebuggerBrowsableAttribute",
            "System.ComponentModel.EditorBrowsableAttribute",
            "System.Diagnostics.Contracts.PureAttribute",
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        };

        /// <summary>Atributos a preservar SIEMPRE (lista blanca).</summary>
        public readonly HashSet<string> KeepAttrNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Runtime.Versioning.TargetFrameworkAttribute",
            "System.Runtime.InteropServices.GuidAttribute",
            "System.Runtime.InteropServices.ComVisibleAttribute",
            "System.Runtime.InteropServices.InterfaceTypeAttribute",
            "System.Runtime.InteropServices.ClassInterfaceAttribute",
            "System.Runtime.InteropServices.StructLayoutAttribute",
            "System.Runtime.CompilerServices.InternalsVisibleToAttribute",
            "System.Reflection.AssemblyVersionAttribute",
            "System.Reflection.AssemblyKeyFileAttribute",
            "System.Reflection.AssemblyDelaySignAttribute",
        };

        // ===== Métricas =====
        private int _asmAttrsRemoved, _modAttrsRemoved, _memberAttrsRemoved, _paramNamesCleared, _paramAttrsRemoved, _pdbCleared, _declSecRemoved, _typesVisited, _methodsVisited;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            ResetCounters();

            try
            {
                // 1) Ensamblado / Módulo
                if (module.Assembly != null && module.Assembly.CustomAttributes.Count > 0)
                    _asmAttrsRemoved += RemoveByName(module.Assembly.CustomAttributes, RemoveAssemblyAttrNames, KeepAttrNames, DryRun, Verbose, "[AsmAttr]");

                if (module.CustomAttributes != null && module.CustomAttributes.Count > 0)
                    _modAttrsRemoved += RemoveByName(module.CustomAttributes, RemoveAssemblyAttrNames, KeepAttrNames, DryRun, Verbose, "[ModAttr]");

                // 2) Tipos / Miembros
                foreach (var type in module.GetTypes())
                {
                    _typesVisited++;

                    _memberAttrsRemoved += RemoveByName(type.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[TypeAttr] " + type.FullName);

                    if (StripDeclSecurity && type.DeclSecurities != null && type.DeclSecurities.Count > 0)
                    {
                        _declSecRemoved += type.DeclSecurities.Count;
                        if (!DryRun) type.DeclSecurities.Clear();
                        if (Verbose) Console.WriteLine("[DeclSec][Type] Cleared: " + type.FullName);
                    }

                    foreach (var f in type.Fields)
                        _memberAttrsRemoved += RemoveByName(f.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[FieldAttr] " + f.FullName);

                    foreach (var p in type.Properties)
                        _memberAttrsRemoved += RemoveByName(p.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[PropAttr] " + p.FullName);

                    foreach (var e in type.Events)
                        _memberAttrsRemoved += RemoveByName(e.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[EventAttr] " + e.FullName);

                    foreach (var m in type.Methods)
                    {
                        _methodsVisited++;

                        _memberAttrsRemoved += RemoveByName(m.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[MethAttr] " + m.FullName);

                        if (StripDeclSecurity && m.DeclSecurities != null && m.DeclSecurities.Count > 0)
                        {
                            _declSecRemoved += m.DeclSecurities.Count;
                            if (!DryRun) m.DeclSecurities.Clear();
                            if (Verbose) Console.WriteLine("[DeclSec][Method] Cleared: " + m.FullName);
                        }

                        // ParamDefs (no Parameters)
                        if (m.ParamDefs != null && m.ParamDefs.Count > 0)
                        {
                            for (int i = 0; i < m.ParamDefs.Count; i++)
                            {
                                var pd = m.ParamDefs[i];

                                if (StripParamNames && !UTF8String.IsNullOrEmpty(pd.Name))
                                {
                                    if (!DryRun) pd.Name = UTF8String.Empty;
                                    _paramNamesCleared++;
                                    if (Verbose) Console.WriteLine("[ParamName] Cleared {0} (param #{1})", m.FullName, pd.Sequence);
                                }

                                if (StripParamNoiseAttributes && pd.CustomAttributes != null && pd.CustomAttributes.Count > 0)
                                    _paramAttrsRemoved += RemoveByName(pd.CustomAttributes, RemoveMemberAttrNames, KeepAttrNames, DryRun, Verbose, "[ParamAttr] " + m.FullName + " (param " + pd.Sequence + ")");
                            }
                        }

                        // Debug info (PDB)
                        if (StripDebugInfo && m.HasBody)
                        {
                            var body = m.Body;
                            if (body != null && body.PdbMethod != null)
                            {
                                _pdbCleared++; // contamos métodos con PDB limpiado
                                if (!DryRun)
                                {
                                    // Forma más compatible: eliminar toda la info PDB del método
                                    body.PdbMethod = null;
                                }
                                if (Verbose) Console.WriteLine("[DebugInfo] Cleared PdbMethod: " + m.FullName);
                            }

                            // Además, limpiar CustomDebugInfos del método
                            if (!DryRun) m.CustomDebugInfos.Clear();
                        }
                    }
                }

                // Limpieza IL (si no es DryRun)
                if (!DryRun)
                {
                    foreach (var type in module.Types)
                    {
                        foreach (var m in type.Methods)
                        {
                            if (!m.HasBody) continue;
                            var body = m.Body;
                            body.SimplifyMacros(m.Parameters);
                            body.OptimizeMacros();
                            body.SimplifyBranches();
                            body.OptimizeBranches();
                        }
                    }
                }

                // Report
                Console.WriteLine("[MetadataCleaner] Types visited:         {0}", _typesVisited);
                Console.WriteLine("[MetadataCleaner] Methods visited:       {0}", _methodsVisited);
                Console.WriteLine("[MetadataCleaner] Assembly attrs removed:{0}", _asmAttrsRemoved);
                Console.WriteLine("[MetadataCleaner] Module attrs removed:  {0}", _modAttrsRemoved);
                Console.WriteLine("[MetadataCleaner] Member attrs removed:  {0}", _memberAttrsRemoved);
                Console.WriteLine("[MetadataCleaner] Param names cleared:   {0}", _paramNamesCleared);
                Console.WriteLine("[MetadataCleaner] Param attrs removed:   {0}", _paramAttrsRemoved);
                Console.WriteLine("[MetadataCleaner] PDB methods cleared:   {0}", _pdbCleared);
                Console.WriteLine("[MetadataCleaner] DeclSecurity removed:  {0}", _declSecRemoved);
                Console.WriteLine("[MetadataCleaner] DryRun:                {0}", DryRun ? "YES" : "NO");

                return true;
            }
            catch (Exception ex)
            {
                this.Errors = ex;
                Console.WriteLine("[MetadataCleaner] ERROR: " + ex);
                return false;
            }
            finally { await Task.CompletedTask; }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        // ===== Helpers =====
        private static int RemoveByName(CustomAttributeCollection col, HashSet<string> blacklist, HashSet<string> whitelist, bool dryRun, bool verbose, string logPrefix)
        {
            if (col == null || col.Count == 0) return 0;
            int removed = 0;

            for (int i = col.Count - 1; i >= 0; i--)
            {
                var ca = col[i];
                string full = ca.TypeFullName;
                if (string.IsNullOrEmpty(full)) continue;

                if (whitelist.Contains(full))
                    continue;

                if (blacklist.Contains(full))
                {
                    if (verbose) Console.WriteLine("{0} Remove: {1}", logPrefix, full);
                    if (!dryRun)
                        col.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        private void ResetCounters()
        {
            _asmAttrsRemoved = 0;
            _modAttrsRemoved = 0;
            _memberAttrsRemoved = 0;
            _paramNamesCleared = 0;
            _paramAttrsRemoved = 0;
            _pdbCleared = 0;
            _declSecRemoved = 0;
            _typesVisited = 0;
            _methodsVisited = 0;
        }
    }
}