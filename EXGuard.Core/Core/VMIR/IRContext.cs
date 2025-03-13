using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.AST;
using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR
{
    public class IRContext
    {
        private readonly IRVariable[] args;
        private readonly Dictionary<ExceptionHandler, IRVariable> ehVars;
        private readonly IRVariable[] locals;
        private readonly Dictionary<ILASTVariable, IRVariable> varMap = new Dictionary<ILASTVariable, IRVariable>();
        private readonly List<IRVariable> vRegs = new List<IRVariable>();

        public IRContext(MethodDef method, CilBody body)
        {
            Method = method;
            IsRuntime = false;

            locals = new IRVariable[body.Variables.Count];
            for(var i = 0; i < locals.Length; i++)
            {
                if(body.Variables[i].Type.IsPinned)
                    throw new NotSupportedException("Pinned variables are not supported.");

                locals[i] = new IRVariable
                {
                    Id = i,
                    Name = "local_" + i,
                    Type = TypeInference.ToASTType(body.Variables[i].Type),
                    RawType = body.Variables[i].Type,
                    VariableType = IRVariableType.Local
                };
            }

            args = new IRVariable[method.Parameters.Count];
            for(var i = 0; i < args.Length; i++)
                args[i] = new IRVariable
                {
                    Id = i,
                    Name = "arg_" + i,
                    Type = TypeInference.ToASTType(method.Parameters[i].Type),
                    RawType = method.Parameters[i].Type,
                    VariableType = IRVariableType.Argument
                };

            ehVars = new Dictionary<ExceptionHandler, IRVariable>();
            var id = -1;
            foreach(var eh in body.ExceptionHandlers)
            {
                id++;
                if(eh.HandlerType == ExceptionHandlerType.Fault ||
                   eh.HandlerType == ExceptionHandlerType.Finally)
                    continue;
                var type = eh.CatchType.ToTypeSig();
                ehVars.Add(eh, new IRVariable
                {
                    Id = id,
                    Name = "ex_" + id,
                    Type = TypeInference.ToASTType(type),
                    RawType = type,
                    VariableType = IRVariableType.VirtualRegister
                });
            }
        }

        public MethodDef Method
        {
            get;
        }

        public bool IsRuntime
        {
            get;
            set;
        }

        public IRVariable AllocateVRegister(ASTType type)
        {
            var vReg = new IRVariable
            {
                Id = vRegs.Count,
                Name = "vreg_" + vRegs.Count,
                Type = type,
                VariableType = IRVariableType.VirtualRegister
            };
            vRegs.Add(vReg);
            return vReg;
        }

        public IRVariable ResolveVRegister(ILASTVariable variable)
        {
            if(variable.VariableType == ILASTVariableType.ExceptionVar)
                return ResolveExceptionVar((ExceptionHandler) variable.Annotation);

            IRVariable vReg;
            if(varMap.TryGetValue(variable, out vReg))
                return vReg;
            vReg = AllocateVRegister(variable.Type);
            varMap[variable] = vReg;
            return vReg;
        }

        public IRVariable ResolveParameter(Parameter param)
        {
            return args[param.Index];
        }

        public IRVariable ResolveLocal(Local local)
        {
            return locals[local.Index];
        }

        public IRVariable[] GetParameters()
        {
            return args;
        }

        public IRVariable ResolveExceptionVar(ExceptionHandler eh)
        {
            return ehVars[eh];
        }
    }
}