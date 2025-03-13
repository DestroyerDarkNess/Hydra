using System;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet.Emit;

using EXGuard.Core.RT;
using EXGuard.Core.VM;
using EXGuard.Core.CFG;
using EXGuard.Core.AST.IL;

namespace EXGuard.Core.VMIL.Transforms
{
    public class BlockKeyTransform : IPostTransform
    {
        private Dictionary<ILBlock, BlockKey> Keys;
        private VMMethodInfo methodInfo;

        private VMRuntime runtime;

        public void Initialize(ILPostTransformer tr)
        {
            runtime = tr.Runtime;
            methodInfo = tr.Runtime.Descriptor.Data.LookupInfo(tr.Method);

            ComputeBlockKeys(tr.RootScope);
        }

        public void Transform(ILPostTransformer tr)
        {
            var key = Keys[tr.Block];
            methodInfo.BlockKeys[tr.Block] = new VMBlockKey
            {
                EntryKey = (byte) key.Entry,
                ExitKey = (byte) key.Exit
            };
        }

        private void ComputeBlockKeys(ScopeBlock rootScope)
        {
            var blocks = rootScope.GetBasicBlocks().OfType<ILBlock>().ToList();
            uint id = 1;
            Keys = blocks.ToDictionary(
                block => block,
                block => new BlockKey {Entry = id++, Exit = id++});
            var ehMap = MapEHs(rootScope);

            bool updated;
            do
            {
                updated = false;

                BlockKey key;

                key = Keys[blocks[0]];
                key.Entry = 0xfffffffe;
                Keys[blocks[0]] = key;

                key = Keys[blocks[blocks.Count - 1]];
                key.Exit = 0xfffffffd;
                Keys[blocks[blocks.Count - 1]] = key;

                // Update the state ids with the maximum id
                foreach(var block in blocks)
                {
                    key = Keys[block];
                    if(block.Sources.Count > 0)
                    {
                        var newEntry = block.Sources.Select(b => Keys[(ILBlock) b].Exit).Max();
                        if(key.Entry != newEntry)
                        {
                            key.Entry = newEntry;
                            updated = true;
                        }
                    }
                    if(block.Targets.Count > 0)
                    {
                        var newExit = block.Targets.Select(b => Keys[(ILBlock) b].Entry).Max();
                        if(key.Exit != newExit)
                        {
                            key.Exit = newExit;
                            updated = true;
                        }
                    }
                    Keys[block] = key;
                }

                // Match finally enter = finally exit = try end exit
                // Match filter start = 0xffffffff
                MatchHandlers(ehMap, ref updated);
            } while(updated);

            // Replace id with actual values
            var idMap = new Dictionary<uint, uint>();
            idMap[0xffffffff] = 0;
            idMap[0xfffffffe] = methodInfo.EntryKey;
            idMap[0xfffffffd] = methodInfo.ExitKey;
            foreach(var block in blocks)
            {
                var key = Keys[block];

                var entryId = key.Entry;
                if(!idMap.TryGetValue(entryId, out key.Entry))
                    key.Entry = idMap[entryId] = runtime.Descriptor.RandomGenerator.NextByte();

                var exitId = key.Exit;
                if(!idMap.TryGetValue(exitId, out key.Exit))
                    key.Exit = idMap[exitId] = runtime.Descriptor.RandomGenerator.NextByte();

                Keys[block] = key;
            }
        }

        private EHMap MapEHs(ScopeBlock rootScope)
        {
            var map = new EHMap();
            MapEHsInternal(rootScope, map);
            return map;
        }

        private void MapEHsInternal(ScopeBlock scope, EHMap map)
        {
            if(scope.Type == ScopeType.Filter)
            {
                map.Starts.Add((ILBlock) scope.GetBasicBlocks().First());
            }
            else if(scope.Type != ScopeType.None)
            {
                if(scope.ExceptionHandler.HandlerType == ExceptionHandlerType.Finally)
                {
                    FinallyInfo info;
                    if(!map.Finally.TryGetValue(scope.ExceptionHandler, out info))
                        map.Finally[scope.ExceptionHandler] = info = new FinallyInfo();

                    if(scope.Type == ScopeType.Try)
                    {
                        // Try End Next
                        var scopeBlocks = new HashSet<IBasicBlock>(scope.GetBasicBlocks());
                        foreach(ILBlock block in scopeBlocks)
                            if((block.Flags & BlockFlags.ExitEHLeave) != 0 &&
                               (block.Targets.Count == 0 ||
                                block.Targets.Any(target => !scopeBlocks.Contains(target))))
                                foreach(var target in block.Targets)
                                    info.TryEndNexts.Add((ILBlock) target);
                    }
                    else if(scope.Type == ScopeType.Handler)
                    {
                        // Finally End
                        IEnumerable<IBasicBlock> candidates;
                        if(scope.Children.Count > 0)
                            candidates = scope.Children
                                .Where(s => s.Type == ScopeType.None)
                                .SelectMany(s => s.GetBasicBlocks());
                        else candidates = scope.Content;
                        foreach(ILBlock block in candidates)
                            if((block.Flags & BlockFlags.ExitEHReturn) != 0 &&
                               block.Targets.Count == 0) info.FinallyEnds.Add(block);
                    }
                }
                if(scope.Type == ScopeType.Handler)
                    map.Starts.Add((ILBlock) scope.GetBasicBlocks().First());
            }
            foreach(var child in scope.Children)
                MapEHsInternal(child, map);
        }

        private void MatchHandlers(EHMap map, ref bool updated)
        {
            // handler start = 0xffffffff
            // finally end = next block of try end
            foreach(var start in map.Starts)
            {
                var key = Keys[start];
                if(key.Entry != 0xffffffff)
                {
                    key.Entry = 0xffffffff;
                    Keys[start] = key;
                    updated = true;
                }
            }
            foreach(var info in map.Finally.Values)
            {
                var maxEnd = info.FinallyEnds.Max(block => Keys[block].Exit);
                var maxEntry = info.TryEndNexts.Max(block => Keys[block].Entry);
                var maxId = Math.Max(maxEnd, maxEntry);

                foreach(var block in info.FinallyEnds)
                {
                    var key = Keys[block];
                    if(key.Exit != maxId)
                    {
                        key.Exit = maxId;
                        Keys[block] = key;
                        updated = true;
                    }
                }

                foreach(var block in info.TryEndNexts)
                {
                    var key = Keys[block];
                    if(key.Entry != maxId)
                    {
                        key.Entry = maxId;
                        Keys[block] = key;
                        updated = true;
                    }
                }
            }
        }

        private struct BlockKey
        {
            public uint Entry;
            public uint Exit;
        }

        private class FinallyInfo
        {
            public readonly HashSet<ILBlock> FinallyEnds = new HashSet<ILBlock>();
            public readonly HashSet<ILBlock> TryEndNexts = new HashSet<ILBlock>();
        }

        private class EHMap
        {
            public readonly Dictionary<ExceptionHandler, FinallyInfo> Finally = new Dictionary<ExceptionHandler, FinallyInfo>();
            public readonly HashSet<ILBlock> Starts = new HashSet<ILBlock>();
        }
    }
}