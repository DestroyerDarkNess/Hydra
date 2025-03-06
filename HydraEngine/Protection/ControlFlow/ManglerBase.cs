using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    internal abstract class ManglerBase
    {
        protected static IEnumerable<BlockParser.InstrBlock> GetAllBlocks(BlockParser.ScopeBlock scope)
        {
            foreach (BlockParser.BlockBase child in scope.Children)
            {
                if (!(child is BlockParser.InstrBlock))
                {
                    foreach (BlockParser.InstrBlock allBlock in GetAllBlocks((BlockParser.ScopeBlock)child))
                    {
                        yield return allBlock;
                    }
                }
                else
                {
                    yield return (BlockParser.InstrBlock)child;
                }
            }
        }

        public abstract void Mangle(CilBody body, BlockParser.ScopeBlock root, ModuleDefMD ctx, MethodDef method, TypeSig retType);
    }

}
