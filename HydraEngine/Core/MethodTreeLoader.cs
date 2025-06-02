using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraEngine.Core
{
    public class MethodTreeLoader
    {
        private readonly TreeView _methodTree;
        private readonly ModuleDefMD _module;
        private bool _all = false;

        private enum NodeType
        { Namespace, Type, Method }

        private class NodeInfo
        {
            public NodeType Type { get; set; }
            public MethodDef Method { get; set; }
        }

        private static class Colors
        {
            public static readonly Color PublicMethod = Color.FromArgb(16744448);
            public static readonly Color PrivateMethod = Color.FromArgb(13395456);
            public static readonly Color Constructor = Color.FromArgb(5163440);
            public static readonly Color PrivateConstructor = Color.FromArgb(4567708);
            public static readonly Color SelectedMethod = Color.Lime;
            public static readonly Color StaticConstructor = Color.Cyan;
            public static readonly Color RedMethod = Color.Tomato;
            public static readonly Color UnsafeMethod = Color.DarkRed;
            public static readonly Color UserStaticMethod = Color.FromArgb(255, 165, 0); // Orange
            public static readonly Color CompilerGeneratedMethod = Color.Gray;
        }

        // Cache para análisis de métodos
        private readonly ConcurrentDictionary<uint, bool> _ovmAnalysisCache = new ConcurrentDictionary<uint, bool>();

        private readonly ConcurrentDictionary<uint, bool> _redMethodCache = new ConcurrentDictionary<uint, bool>();
        private readonly ConcurrentDictionary<uint, bool> _unsafeMethodCache = new ConcurrentDictionary<uint, bool>();
        private readonly ConcurrentDictionary<uint, bool> _userStaticMethodCache = new ConcurrentDictionary<uint, bool>();
        private readonly ConcurrentDictionary<uint, bool> _compilerGeneratedCache = new ConcurrentDictionary<uint, bool>();
        private readonly ConcurrentDictionary<uint, Color> _colorCache = new ConcurrentDictionary<uint, Color>();

        // Eventos para reportar progreso
        public event EventHandler<ProgressEventArgs> ProgressChanged;

        public event EventHandler LoadingStarted;

        public event EventHandler LoadingCompleted;

        public class ProgressEventArgs : EventArgs
        {
            public int CurrentStep { get; set; }
            public int TotalSteps { get; set; }
            public string CurrentOperation { get; set; }
            public int PercentComplete => TotalSteps > 0 ? (CurrentStep * 100) / TotalSteps : 0;
        }

        public MethodTreeLoader(TreeView methodTree, ModuleDefMD module)
        {
            _methodTree = methodTree;
            _module = module;
            _methodTree.AfterCheck += MethodTree_AfterCheck;
            _methodTree.NodeMouseDoubleClick += MethodTree_NodeMouseDoubleClick;
            _methodTree.CheckBoxes = true;
        }

        public bool All
        {
            get => _all;
            set
            {
                _all = value;
                UpdateAllMethodsSelection();
            }
        }

        public bool ExcludeConstructors { get; set; } = false;
        public bool ExcludeRedMethods { get; set; } = true;
        public bool ExcludeUnsafeMethods { get; set; } = true;
        public bool ExcludeCompilerGenerated { get; set; } = true;
        public bool HighlightUserStaticMethods { get; set; } = true;

        // Método principal optimizado con carga asíncrona y progreso
        public async Task LoadMethodsAsync(IProgress<ProgressEventArgs> progress = null)
        {
            LoadingStarted?.Invoke(this, EventArgs.Empty);
            _methodTree.Nodes.Clear();
            _methodTree.BeginUpdate();

            try
            {
                // Paso 1: Filtrar tipos válidos
                ReportProgress(progress, 1, 5, "Analizando tipos...");
                var validTypes = _module.GetTypes()
                    .Where(t => !IsSpecialType(t))
                    .ToList();

                if (validTypes.Count == 0)
                {
                    _methodTree.EndUpdate();
                    LoadingCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // Paso 2: Pre-analizar métodos en paralelo
                ReportProgress(progress, 2, 5, "Pre-analizando métodos...");
                await PreAnalyzeMethodsAsync(validTypes, progress);

                // Paso 3: Agrupar por namespace
                ReportProgress(progress, 3, 5, "Agrupando por namespace...");
                var namespaceGroups = validTypes
                    .GroupBy(t => t.Namespace ?? string.Empty)
                    .ToList();

                // Paso 4: Crear nodos del árbol
                ReportProgress(progress, 4, 5, "Creando nodos del árbol...");
                var rootNodes = await CreateTreeNodesAsync(namespaceGroups, progress);

                // Paso 5: Agregar nodos al TreeView
                ReportProgress(progress, 5, 5, "Finalizando carga...");
                _methodTree.Nodes.AddRange(rootNodes.ToArray());
            }
            finally
            {
                _methodTree.EndUpdate();
                //_methodTree.Sort();
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        // Método síncrono optimizado con progreso
        public void LoadMethods(IProgress<ProgressEventArgs> progress = null)
        {
            LoadingStarted?.Invoke(this, EventArgs.Empty);
            _methodTree.Nodes.Clear();
            _methodTree.BeginUpdate();

            try
            {
                // Paso 1: Filtrar tipos válidos
                ReportProgress(progress, 1, 4, "Analizando tipos...");
                var validTypes = _module.GetTypes()
                    .Where(t => !IsSpecialType(t))
                    .ToList();

                if (validTypes.Count == 0)
                {
                    LoadingCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // Paso 2: Agrupar por namespace
                ReportProgress(progress, 2, 4, "Agrupando por namespace...");
                var namespaceGroups = validTypes
                    .GroupBy(t => t.Namespace ?? string.Empty)
                    .ToList();

                // Paso 3: Crear nodos con progreso detallado
                ReportProgress(progress, 3, 4, "Creando nodos del árbol...");
                var rootNodes = CreateTreeNodesWithProgress(namespaceGroups, progress);

                // Paso 4: Agregar nodos al TreeView
                ReportProgress(progress, 4, 4, "Finalizando carga...");
                _methodTree.Nodes.AddRange(rootNodes.ToArray());
            }
            finally
            {
                _methodTree.EndUpdate();
                //_methodTree.Sort();
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        // Pre-análisis en paralelo para cache con progreso
        private async Task PreAnalyzeMethodsAsync(List<TypeDef> types, IProgress<ProgressEventArgs> progress = null)
        {
            var allMethods = types.SelectMany(t => t.Methods).ToList();
            var processedCount = 0;
            var totalMethods = allMethods.Count;

            await Task.Run(() =>
            {
                var lockObj = new object();

                Parallel.ForEach(allMethods, method =>
                {
                    var token = method.MDToken.Raw;

                    // Pre-calcular análisis OVM
                    _ovmAnalysisCache.TryAdd(token, OVMAnalyzer(method));

                    // Pre-calcular análisis de métodos rojos y unsafe
                    if (_ovmAnalysisCache[token])
                    {
                        _redMethodCache.TryAdd(token, IsRedMethod(method));
                        _unsafeMethodCache.TryAdd(token, IsUnsafeMethod(method));
                        _userStaticMethodCache.TryAdd(token, IsUserStaticMethod(method));
                        _compilerGeneratedCache.TryAdd(token, IsCompilerGenerated(method));
                        _colorCache.TryAdd(token, CalculateMethodColor(method));
                    }

                    // Reportar progreso cada 100 métodos procesados
                    lock (lockObj)
                    {
                        processedCount++;
                        if (processedCount % 100 == 0 || processedCount == totalMethods)
                        {
                            var subProgress = new ProgressEventArgs
                            {
                                CurrentStep = processedCount,
                                TotalSteps = totalMethods,
                                CurrentOperation = $"Analizando método {processedCount}/{totalMethods}"
                            };
                            progress?.Report(subProgress);
                        }
                    }
                });
            });
        }

        // Crear nodos del árbol con progreso (versión asíncrona)
        private async Task<List<TreeNode>> CreateTreeNodesAsync(List<IGrouping<UTF8String, TypeDef>> namespaceGroups, IProgress<ProgressEventArgs> progress = null)
        {
            var rootNodes = new List<TreeNode>();
            var processedGroups = 0;
            var totalGroups = namespaceGroups.Count;

            foreach (var group in namespaceGroups)
            {
                var namespaceNode = CreateNamespaceNode(group.Key.ToString());

                foreach (var type in group)
                {
                    var typeNode = CreateTypeNode(type);
                    var validMethods = type.Methods.Where(ShouldIncludeMethod).ToList();

                    if (validMethods.Count > 0)
                    {
                        foreach (var method in validMethods)
                        {
                            typeNode.Nodes.Add(CreateMethodNode(method));
                        }
                        namespaceNode.Nodes.Add(typeNode);
                    }
                }

                if (namespaceNode.Nodes.Count > 0)
                    rootNodes.Add(namespaceNode);

                processedGroups++;
                var subProgress = new ProgressEventArgs
                {
                    CurrentStep = processedGroups,
                    TotalSteps = totalGroups,
                    CurrentOperation = $"Procesando namespace {processedGroups}/{totalGroups}: {group.Key}"
                };
                progress?.Report(subProgress);

                // Yield control to UI thread occasionally
                if (processedGroups % 5 == 0)
                    await Task.Yield();
            }

            return rootNodes;
        }

        // Crear nodos del árbol con progreso (versión síncrona)
        private List<TreeNode> CreateTreeNodesWithProgress(List<IGrouping<UTF8String, TypeDef>> namespaceGroups, IProgress<ProgressEventArgs> progress = null)
        {
            var rootNodes = new List<TreeNode>();
            var processedGroups = 0;
            var totalGroups = namespaceGroups.Count;

            foreach (var group in namespaceGroups)
            {
                var namespaceNode = CreateNamespaceNode(group.Key.ToString());

                foreach (var type in group)
                {
                    var typeNode = CreateTypeNode(type);
                    var validMethods = type.Methods.Where(ShouldIncludeMethod).ToList();

                    if (validMethods.Count > 0)
                    {
                        foreach (var method in validMethods)
                        {
                            typeNode.Nodes.Add(CreateMethodNode(method));
                        }
                        namespaceNode.Nodes.Add(typeNode);
                    }
                }

                if (namespaceNode.Nodes.Count > 0)
                    rootNodes.Add(namespaceNode);

                processedGroups++;
                var subProgress = new ProgressEventArgs
                {
                    CurrentStep = processedGroups,
                    TotalSteps = totalGroups,
                    CurrentOperation = $"Procesando namespace {processedGroups}/{totalGroups}: {group.Key}"
                };
                progress?.Report(subProgress);

                // Permite que la UI se actualice
                Application.DoEvents();
            }

            return rootNodes;
        }

        // Método auxiliar para reportar progreso
        private void ReportProgress(IProgress<ProgressEventArgs> progress, int currentStep, int totalSteps, string operation)
        {
            var args = new ProgressEventArgs
            {
                CurrentStep = currentStep,
                TotalSteps = totalSteps,
                CurrentOperation = operation
            };

            progress?.Report(args);
            ProgressChanged?.Invoke(this, args);
        }

        private TreeNode CreateNamespaceWithTypes(IGrouping<string, TypeDef> namespaceGroup)
        {
            var namespaceNode = CreateNamespaceNode(namespaceGroup.Key);

            foreach (var type in namespaceGroup)
            {
                var typeNode = CreateTypeNode(type);

                // Filtrar y crear nodos de métodos de una vez
                var methodNodes = type.Methods
                    .Where(ShouldIncludeMethod)
                    .Select(CreateMethodNode)
                    .ToArray();

                if (methodNodes.Length > 0)
                {
                    typeNode.Nodes.AddRange(methodNodes);
                    namespaceNode.Nodes.Add(typeNode);
                }
            }

            return namespaceNode;
        }

        // Métodos de análisis optimizados con cache
        private bool ShouldIncludeMethod(MethodDef method)
        {
            if (method.IsStaticConstructor) return false;

            var token = method.MDToken.Raw;
            return _ovmAnalysisCache.GetOrAdd(token, _ => OVMAnalyzer(method));
        }

        public bool IsRedMethod(MethodDef method)
        {
            var token = method.MDToken.Raw;
            return _redMethodCache.GetOrAdd(token, _ =>
            {
                if (!method.HasBody || !method.Body.HasInstructions) return false;

                return method.Body.Instructions.Any(instr =>
                    instr.OpCode == OpCodes.Or || instr.OpCode == OpCodes.And);
            });
        }

        public bool IsUnsafeMethod(MethodDef method)
        {
            var token = method.MDToken.Raw;
            return _unsafeMethodCache.GetOrAdd(token, _ =>
            {
                if (!method.HasBody || !method.Body.HasInstructions) return false;

                var unsafeOpcodes = new[] { OpCodes.Ldind_I1, OpCodes.Stind_I1, OpCodes.Conv_I };
                return method.Body.Instructions.Any(instr => unsafeOpcodes.Contains(instr.OpCode));
            });
        }

        // Método para identificar métodos estáticos escritos por el usuario
        public bool IsUserStaticMethod(MethodDef method)
        {
            var token = method.MDToken.Raw;
            return _userStaticMethodCache.GetOrAdd(token, _ =>
            {
                // Debe ser estático pero no constructor estático
                if (!method.IsStatic || method.IsStaticConstructor) return false;

                // No debe ser generado por el compilador
                if (IsCompilerGenerated(method)) return false;

                // No debe ser un método especial del sistema
                if (method.IsSpecialName) return false;

                // Verificar que no sea parte de tipos especiales
                if (IsSpecialType(method.DeclaringType)) return false;

                return true;
            });
        }

        // Método para identificar métodos generados por el compilador
        public bool IsCompilerGenerated(MethodDef method)
        {
            var token = method.MDToken.Raw;
            return _compilerGeneratedCache.GetOrAdd(token, _ =>
            {
                // Verificar atributo CompilerGenerated
                if (method.HasCustomAttributes)
                {
                    var compilerGeneratedAttr = method.CustomAttributes
                        .FirstOrDefault(attr => attr.AttributeType.Name == "CompilerGeneratedAttribute" ||
                                              attr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
                    if (compilerGeneratedAttr != null) return true;
                }

                // Verificar patrones de nombres generados por el compilador
                var methodName = method.Name;

                // Métodos lambda y funciones anónimas
                if (methodName.Contains("<>") || methodName.Contains("__") || methodName.StartsWith("<"))
                    return true;

                // Métodos de backing field para propiedades automáticas
                if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                {
                    var propertyName = methodName.Substring(4);
                    var declaringType = method.DeclaringType;
                    if (declaringType != null && declaringType.HasProperties)
                    {
                        var property = declaringType.Properties.FirstOrDefault(p => p.Name == propertyName);
                        if (property != null && property.HasCustomAttributes)
                        {
                            var compilerGeneratedAttr = property.CustomAttributes
                                .FirstOrDefault(attr => attr.AttributeType.Name == "CompilerGeneratedAttribute");
                            if (compilerGeneratedAttr != null) return true;
                        }
                    }
                }

                // Métodos de eventos automáticos
                if (methodName.StartsWith("add_") || methodName.StartsWith("remove_"))
                {
                    var eventName = methodName.Substring(4);
                    var declaringType = method.DeclaringType;
                    if (declaringType != null && declaringType.HasEvents)
                    {
                        var eventDef = declaringType.Events.FirstOrDefault(e => e.Name == eventName);
                        if (eventDef != null && eventDef.HasCustomAttributes)
                        {
                            var compilerGeneratedAttr = eventDef.CustomAttributes
                                .FirstOrDefault(attr => attr.AttributeType.Name == "CompilerGeneratedAttribute");
                            if (compilerGeneratedAttr != null) return true;
                        }
                    }
                }

                // Verificar si el tipo contenedor es generado por el compilador
                if (method.DeclaringType != null)
                {
                    var declaringType = method.DeclaringType;

                    // Tipos generados para expresiones lambda, async/await, etc.
                    if (declaringType.Name.Contains("<>") ||
                        declaringType.Name.Contains("__") ||
                        declaringType.Name.StartsWith("<") ||
                        declaringType.Name.Contains("DisplayClass") ||
                        declaringType.Name.Contains("StateMachine"))
                        return true;

                    // Verificar atributo CompilerGenerated en el tipo
                    if (declaringType.HasCustomAttributes)
                    {
                        var compilerGeneratedAttr = declaringType.CustomAttributes
                            .FirstOrDefault(attr => attr.AttributeType.Name == "CompilerGeneratedAttribute");
                        if (compilerGeneratedAttr != null) return true;
                    }
                }

                return false;
            });
        }

        // Método auxiliar para obtener métodos estáticos del usuario
        public List<MethodDef> GetUserStaticMethods()
        {
            var userStatics = new List<MethodDef>();
            foreach (TreeNode node in _methodTree.Nodes)
                CollectUserStaticMethods(node, userStatics);
            return userStatics;
        }

        private void CollectUserStaticMethods(TreeNode parentNode, List<MethodDef> methods)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is NodeInfo info && info.Type == NodeType.Method && IsUserStaticMethod(info.Method))
                    methods.Add(info.Method);

                CollectUserStaticMethods(node, methods);
            }
        }

        private Color GetMethodColor(MethodDef method)
        {
            var token = method.MDToken.Raw;
            return _colorCache.GetOrAdd(token, _ => CalculateMethodColor(method));
        }

        private Color CalculateMethodColor(MethodDef method)
        {
            if (method.IsStaticConstructor) return Colors.StaticConstructor;
            if (IsCompilerGenerated(method)) return Colors.CompilerGeneratedMethod;
            if (IsUnsafeMethod(method)) return Colors.UnsafeMethod;
            if (IsRedMethod(method)) return Colors.RedMethod;
            if (IsUserStaticMethod(method)) return Colors.UserStaticMethod;
            return method.IsConstructor
                ? (method.IsPrivate ? Colors.PrivateConstructor : Colors.Constructor)
                : (method.IsPublic ? Colors.PublicMethod : Colors.PrivateMethod);
        }

        // Método para limpiar cache si es necesario
        public void ClearCache()
        {
            _ovmAnalysisCache.Clear();
            _redMethodCache.Clear();
            _unsafeMethodCache.Clear();
            _userStaticMethodCache.Clear();
            _compilerGeneratedCache.Clear();
            _colorCache.Clear();
        }

        // Resto de métodos sin cambios significativos...
        public List<MethodDef> GetSelectedMethods()
        {
            var selectedMethods = new List<MethodDef>();
            foreach (TreeNode node in _methodTree.Nodes)
                CollectSelectedMethods(node, selectedMethods);
            return selectedMethods;
        }

        public List<uint> GetSelectedMethodTokens()
        {
            var selectedTokens = new List<uint>();
            foreach (TreeNode node in _methodTree.Nodes)
                CollectSelectedMethodTokens(node, selectedTokens);
            return selectedTokens;
        }

        public static List<MethodDef> ResolveMethodsFromTokens(ModuleDefMD module, List<uint> tokens)
        {
            var resolvedMethods = new List<MethodDef>();
            foreach (var token in tokens)
            {
                var method = module.ResolveToken(token) as MethodDef;
                if (method != null)
                    resolvedMethods.Add(method);
            }
            return resolvedMethods;
        }

        #region Event Handlers

        private void MethodTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            _methodTree.BeginUpdate();
            var node = e.Node;

            if (node.Tag is NodeInfo info)
            {
                if (info.Type == NodeType.Namespace || info.Type == NodeType.Type)
                    UpdateChildNodes(node, node.Checked);

                if (info.Type == NodeType.Method)
                    node.ForeColor = node.Checked ? Colors.SelectedMethod : GetMethodColor(info.Method);
            }

            _methodTree.EndUpdate();
        }

        private void MethodTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is NodeInfo info && info.Type == NodeType.Method)
            {
                e.Node.Checked = !e.Node.Checked;
                e.Node.ForeColor = e.Node.Checked ?
                    Colors.SelectedMethod :
                    GetMethodColor(info.Method);
            }
        }

        #endregion Event Handlers

        #region Tree Population

        private TreeNode CreateNamespaceNode(string namespaceName)
        {
            return new TreeNode(namespaceName)
            {
                Tag = new NodeInfo { Type = NodeType.Namespace },
                ImageKey = "namespace",
                SelectedImageKey = "namespace"
            };
        }

        private TreeNode CreateTypeNode(TypeDef type)
        {
            var cleanName = type.Name.Contains("`")
                ? type.Name.Substring(0, type.Name.IndexOf('`'))
                : type.Name.Replace("`", string.Empty);

            return new TreeNode(cleanName)
            {
                Tag = new NodeInfo { Type = NodeType.Type },
                ImageKey = "class",
                SelectedImageKey = "class"
            };
        }

        private TreeNode CreateMethodNode(MethodDef method)
        {
            var methodName = method.Name;
            var token = $"0x{method.MDToken:X4}";

            // Añadir indicadores visuales para métodos especiales
            var indicators = new List<string>();
            if (IsUserStaticMethod(method)) indicators.Add("[STATIC]");
            if (IsCompilerGenerated(method)) indicators.Add("[COMPILER]");
            if (IsRedMethod(method)) indicators.Add("[RED]");
            if (IsUnsafeMethod(method)) indicators.Add("[UNSAFE]");

            var displayName = indicators.Count > 0
                ? $"{methodName} {string.Join(" ", indicators)} ({token})"
                : $"{methodName} ({token})";

            var node = new TreeNode(displayName)
            {
                Tag = new NodeInfo { Type = NodeType.Method, Method = method },
                ForeColor = GetMethodColor(method),
                Checked = All && ShouldCheckMethod(method)
            };

            node.ImageKey = method.IsConstructor ? "constructor" : "method";
            node.SelectedImageKey = node.ImageKey;
            return node;
        }

        private void CollectSelectedMethodTokens(TreeNode parentNode, List<uint> tokens)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is NodeInfo info && info.Type == NodeType.Method && node.Checked)
                    tokens.Add(info.Method.MDToken.Raw);

                CollectSelectedMethodTokens(node, tokens);
            }
        }

        #endregion Tree Population

        #region Selection Logic

        private void UpdateChildNodes(TreeNode parentNode, bool isChecked)
        {
            foreach (TreeNode child in parentNode.Nodes)
            {
                if (child.Tag is NodeInfo info)
                {
                    if (info.Type == NodeType.Method)
                    {
                        bool shouldCheck = isChecked &&
                                         (!ExcludeConstructors || !info.Method.IsConstructor) &&
                                         (!ExcludeRedMethods || !IsRedMethod(info.Method)) &&
                                         (!ExcludeUnsafeMethods || !IsUnsafeMethod(info.Method)) &&
                                         (!ExcludeCompilerGenerated || !IsCompilerGenerated(info.Method));

                        child.Checked = shouldCheck;
                        child.ForeColor = shouldCheck ? Colors.SelectedMethod : GetMethodColor(info.Method);
                    }
                    else
                    {
                        child.Checked = isChecked;
                        UpdateChildNodes(child, isChecked);
                    }
                }
            }
        }

        private void UpdateAllMethodsSelection()
        {
            _methodTree.BeginUpdate();

            foreach (TreeNode node in _methodTree.Nodes)
            {
                node.Checked = _all;
                UpdateNodeSelection(node);
            }

            _methodTree.EndUpdate();
        }

        private void UpdateNodeSelection(TreeNode parentNode)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is NodeInfo info)
                {
                    if (info.Type == NodeType.Method)
                    {
                        bool shouldCheck = _all && ShouldCheckMethod(info.Method);
                        node.Checked = shouldCheck;
                        node.ForeColor = shouldCheck ? Colors.SelectedMethod : GetMethodColor(info.Method);
                    }
                    else
                    {
                        node.Checked = _all;
                        UpdateNodeSelection(node);
                    }
                }
            }
        }

        private bool ShouldCheckMethod(MethodDef method)
        {
            return !method.IsStaticConstructor &&
                   (!ExcludeConstructors || !method.IsConstructor) &&
                   (!ExcludeRedMethods || !IsRedMethod(method)) &&
                   (!ExcludeUnsafeMethods || !IsUnsafeMethod(method)) &&
                   (!ExcludeCompilerGenerated || !IsCompilerGenerated(method));
        }

        private void CollectSelectedMethods(TreeNode parentNode, List<MethodDef> methods)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is NodeInfo info && info.Type == NodeType.Method && node.Checked)
                    methods.Add(info.Method);

                CollectSelectedMethods(node, methods);
            }
        }

        #endregion Selection Logic

        #region Helpers

        private bool IsSpecialType(TypeDef type)
        {
            return string.IsNullOrEmpty(type.Namespace)
                || type.Name.Contains("ImplementationDetails>")
                || type.Name.StartsWith("<");
        }

        private bool OVMAnalyzer(MethodDef method)
        {
            if (!method.HasBody) return false;
            if (!method.Body.HasInstructions) return false;
            if (method.HasGenericParameters) return false;
            if (method.IsPinvokeImpl) return false;
            if (method.IsUnmanagedExport) return false;

            return true;
        }

        #endregion Helpers
    }
}