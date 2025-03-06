using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HydraEngine.Core
{
    public class MethodTreeLoader
    {
        private readonly TreeView _methodTree;
        private readonly ModuleDefMD _module;
        private bool _all = false;

        private enum NodeType { Namespace, Type, Method }

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

        public void LoadMethods()
        {
            _methodTree.Nodes.Clear();
            _methodTree.BeginUpdate();

            var namespaceGroups = _module.GetTypes()
                .Where(t => !IsSpecialType(t))
                .GroupBy(t => t.Namespace);

            foreach (var group in namespaceGroups)
            {
                var parentNode = CreateNamespaceNode(group.Key);
                PopulateTypeNodes(parentNode, group);

                if (parentNode.Nodes.Count > 0)
                    _methodTree.Nodes.Add(parentNode);
            }

            RemoveEmptyNodes();
            _methodTree.EndUpdate();
            _methodTree.Sort();
        }

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
        #endregion

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

        private void PopulateTypeNodes(TreeNode parentNode, IEnumerable<TypeDef> types)
        {
            foreach (var type in types)
            {
                var typeNode = CreateTypeNode(type);
                PopulateMethodNodes(typeNode, type.Methods);
                if (typeNode.Nodes.Count > 0) parentNode.Nodes.Add(typeNode);
            }
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

        private void PopulateMethodNodes(TreeNode typeNode, IEnumerable<MethodDef> methods)
        {
            foreach (var method in methods.Where(ShouldIncludeMethod))
                typeNode.Nodes.Add(CreateMethodNode(method));
        }

        private TreeNode CreateMethodNode(MethodDef method)
        {
            var node = new TreeNode($"{method.Name}  (0x{method.MDToken:X4})")
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
        #endregion

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
                                         (!ExcludeUnsafeMethods || !IsUnsafeMethod(info.Method));

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

            // Actualizar todos los nodos principales (namespaces)
            foreach (TreeNode node in _methodTree.Nodes)
            {
                node.Checked = _all; // Marcar/desmarcar el namespace
                UpdateNodeSelection(node); // Actualizar hijos recursivamente
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
                        node.Checked = _all; // Marcar/desmarcar clases y otros nodos padres
                        UpdateNodeSelection(node); // Recursión para hijos
                    }
                }
            }
        }

        private bool ShouldCheckMethod(MethodDef method)
        {
            return !method.IsStaticConstructor &&
                   (!ExcludeConstructors || !method.IsConstructor) &&
                   (!ExcludeRedMethods || !IsRedMethod(method)) &&
                   (!ExcludeUnsafeMethods || !IsUnsafeMethod(method));
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

        #endregion

        #region Helpers
        private Color GetMethodColor(MethodDef method)
        {
            if (method.IsStaticConstructor) return Colors.StaticConstructor;
            if (IsUnsafeMethod(method)) return Colors.UnsafeMethod;
            if (IsRedMethod(method)) return Colors.RedMethod;
            return method.IsConstructor
                ? (method.IsPrivate ? Colors.PrivateConstructor : Colors.Constructor)
                : (method.IsPublic ? Colors.PublicMethod : Colors.PrivateMethod);
        }

        private bool IsSpecialType(TypeDef type)
        {
            return string.IsNullOrEmpty(type.Namespace)
                || type.Name.Contains("ImplementationDetails>")
                || type.Name.StartsWith("<");
        }

        private bool ShouldIncludeMethod(MethodDef method)
        {
            return !method.IsStaticConstructor && OVMAnalyzer(method);
        }

        private bool OVMAnalyzer(MethodDef method)
        {
            if (!method.HasBody) return false;

            if (!method.Body.HasInstructions) return false;

            if (method.HasGenericParameters) return false;

            if (method.IsPinvokeImpl) return false;

            if (method.IsUnmanagedExport) return false;

            //if (method.IsSpecialName) return false;

            return true;
        }

        public bool IsRedMethod(MethodDef method)
        {

            //     if (method.MethodSig != null &&
            //method.MethodSig.Params.Any(p => p.IsSingleOrMultiDimensionalArray))
            //     {
            //         return true;
            //     }

            if (method.Body.Instructions.Any(instr =>
                instr.OpCode == OpCodes.Or || instr.OpCode == OpCodes.And))
                return true;

            return false;
        }

        public bool IsUnsafeMethod(MethodDef method)
        {

            //     if (method.MethodSig != null &&
            //method.MethodSig.Params.Any(p => p.IsSingleOrMultiDimensionalArray))
            //     {
            //         return true;
            //     }

            //if (method.Body.Instructions.Any(instr =>
            //    instr.OpCode == OpCodes.Or || instr.OpCode == OpCodes.And))
            //    return true;

            //if (method.Body.HasVariables &&
            //    method.Body.Variables.Any(v => v.Type.IsPointer))
            //{
            //    return true;
            //}

            var unsafeOpcodes = new[] { OpCodes.Ldind_I1, OpCodes.Stind_I1, OpCodes.Conv_I };
            if (method.Body.Instructions.Any(instr => unsafeOpcodes.Contains(instr.OpCode)))
            {
                return true;
            }

            return false;
        }

        private void RemoveEmptyNodes()
        {
            for (int i = _methodTree.Nodes.Count - 1; i >= 0; i--)
                if (_methodTree.Nodes[i].Nodes.Count == 0)
                    _methodTree.Nodes.RemoveAt(i);
        }
        #endregion
    }
}