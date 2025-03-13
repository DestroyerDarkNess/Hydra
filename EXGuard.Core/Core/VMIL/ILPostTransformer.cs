using System;
using System.Collections.Generic;

using dnlib.DotNet;

using EXGuard.Core.RT;
using EXGuard.Core.CFG;
using EXGuard.Core.AST.IL;
using EXGuard.Core.VMIL.Transforms;

namespace EXGuard.Core.VMIL
{
    public class ILPostTransformer
    {
        private IPostTransform[] pipeline;

        public ILPostTransformer(MethodDef method, ScopeBlock rootScope, VMRuntime runtime)
        {
            RootScope = rootScope;
            Method = method;
            Runtime = runtime;

            Annotations = new Dictionary<object, object>();
            pipeline = InitPipeline();
        }

        public VMRuntime Runtime
        {
            get;
        }

        public MethodDef Method
        {
            get;
        }

        public ScopeBlock RootScope
        {
            get;
        }

        internal Dictionary<object, object> Annotations
        {
            get;
        }

        internal ILBlock Block
        {
            get;
            private set;
        }

        internal ILInstrList Instructions => Block.Content;

        private IPostTransform[] InitPipeline()
        {
            return new IPostTransform[]
            {
                new SaveRegistersTransform(),
                new FixMethodRefTransform(),
                new BlockKeyTransform()
            };
        }

        public void Transform()
        {
            if(pipeline == null)
                throw new InvalidOperationException("Transformer already used.");

            foreach(var handler in pipeline)
            {
                handler.Initialize(this);

                RootScope.ProcessBasicBlocks<ILInstrList>(block =>
                {
                    Block = (ILBlock) block;
                    handler.Transform(this);
                });
            }

            pipeline = null;
        }
    }
}