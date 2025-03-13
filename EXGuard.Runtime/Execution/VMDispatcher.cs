using System;
using System.Runtime.CompilerServices;

using EXGuard.Runtime.Data;
using EXGuard.Runtime.Execution.Internal;

namespace EXGuard.Runtime.Execution
{
    internal static class VMDispatcher
    {
        public static ExecutionState Invoke(VMContext ctx)
        {
            ExecutionState state = ExecutionState.Next;
            bool isAbnormal = true;
            do
            {
                try
                {
                    state = RunInternal(ctx);
                    switch (state)
                    {
                        case ExecutionState.Throw:
                            {
                                var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
                                var ex = ctx.Stack[sp--];
                                ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
                                DoThrow(ctx, ex.O);
                                break;
                            }
                        case ExecutionState.Rethrow:
                            {
                                var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
                                var ex = ctx.Stack[sp--];
                                ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
                                HandleRethrow(ctx, ex.O);
                                return state;
                            }
                    }
                    isAbnormal = false;
                }
                catch (Exception ex)
                {
                    // Patched to catch object
                    SetupEHState(ctx, ex);
                    isAbnormal = false;
                }
                finally
                {
                    if (isAbnormal)
                    {
                        HandleAbnormalExit(ctx);
                        state = ExecutionState.Exit;
                    }
                    else if (ctx.EHStates.Count > 0)
                    {
                        do
                        {
                            HandleEH(ctx, ref state);
                        } while (state == ExecutionState.Rethrow);
                    }
                }
            } while (state != ExecutionState.Exit);
            return state;
        }

        static Exception Throw(object obj)
        {
            return null;
        }

        static ExecutionState RunInternal(VMContext ctx)
        {
            ExecutionState state;
            while (true)
            {
                var op = ctx.ReadByte();
                var p = ctx.ReadByte(); // For key fixup
                OpCodeMap.Lookup(op).Run(ctx, out state);

                if (ctx.Registers[ctx.Data.Constants.REG_IP].U8 == 1)
                    state = ExecutionState.Exit;

                if (state != ExecutionState.Next)
                    return state;
            }
        }

        static void SetupEHState(VMContext ctx, object ex)
        {
            EHState ehState;
            if (ctx.EHStates.Count != 0)
            {
                ehState = ctx.EHStates[ctx.EHStates.Count - 1];
                if (ehState.CurrentFrame != null)
                {
                    if (ehState.CurrentProcess == EHState.EHProcess.Searching)
                    {
                        // Return from filter => throw exception, default to 0 by Partition III 3.34
                        ctx.Registers[ctx.Data.Constants.REG_R1].U1 = 0;
                    }
                    else if (ehState.CurrentProcess == EHState.EHProcess.Unwinding)
                    {
                        // Return from finally => throw exception, replace current exception
                        // https://stackoverflow.com/questions/2911215/what-happens-if-a-finally-block-throws-an-exception
                        ehState.ExceptionObj = ex;
                    }
                    return;
                }
            }
            ehState = new EHState
            {
                OldBP = ctx.Registers[ctx.Data.Constants.REG_BP],
                OldSP = ctx.Registers[ctx.Data.Constants.REG_SP],
                ExceptionObj = ex,
                CurrentProcess = EHState.EHProcess.Searching,
                CurrentFrame = null,
                HandlerFrame = null
            };
            ctx.EHStates.Add(ehState);
        }

        static void HandleRethrow(VMContext ctx, object ex)
        {
            if (ctx.EHStates.Count > 0)
                SetupEHState(ctx, ex);
            else
                DoThrow(ctx, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DoThrow(VMContext ctx, object ex)
        {
            if (ex is Exception exception)
                throw exception;

            throw Throw(ex);
        }

        static void HandleEH(VMContext ctx, ref ExecutionState state)
        {
            var ehState = ctx.EHStates[ctx.EHStates.Count - 1];
            switch (ehState.CurrentProcess)
            {
                case EHState.EHProcess.Searching:
                    {
                        if (ehState.CurrentFrame != null)
                        {
                            // Return from filter
                            bool filterResult = ctx.Registers[ctx.Data.Constants.REG_R1].U1 != 0;
                            if (filterResult)
                            {
                                ehState.CurrentProcess = EHState.EHProcess.Unwinding;
                                ehState.HandlerFrame = ehState.CurrentFrame;
                                ehState.CurrentFrame = ctx.EHStack.Count;
                                state = ExecutionState.Next;
                                goto case EHState.EHProcess.Unwinding;
                            }
                            ehState.CurrentFrame--;
                        }
                        else
                            ehState.CurrentFrame = ctx.EHStack.Count - 1;

                        var exType = ehState.ExceptionObj.GetType();
                        for (; ehState.CurrentFrame >= 0 && ehState.HandlerFrame == null; ehState.CurrentFrame--)
                        {
                            var frame = ctx.EHStack[ehState.CurrentFrame.Value];
                            if (frame.EHType == ctx.Instance.Data.Constants.EH_FILTER)
                            {
                                // Run filter
                                var sp = ehState.OldSP.U4;
                                ctx.Stack.SetTopPosition(++sp);
                                ctx.Stack[sp] = new VMSlot { O = ehState.ExceptionObj };
                                ctx.Registers[ctx.Data.Constants.REG_K1].U1 = 0;
                                ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
                                ctx.Registers[ctx.Data.Constants.REG_BP] = frame.BP;
                                ctx.Registers[ctx.Data.Constants.REG_IP].U8 = frame.FilterAddr;
                                break;
                            }
                            if (frame.EHType == ctx.Instance.Data.Constants.EH_CATCH)
                            {
                                if (frame.CatchType.IsAssignableFrom(exType))
                                {
                                    ehState.CurrentProcess = EHState.EHProcess.Unwinding;
                                    ehState.HandlerFrame = ehState.CurrentFrame;
                                    ehState.CurrentFrame = ctx.EHStack.Count;
                                    goto case EHState.EHProcess.Unwinding;
                                }
                            }
                        }
                        if (ehState.CurrentFrame == -1 && ehState.HandlerFrame == null)
                        {
                            ctx.EHStates.RemoveAt(ctx.EHStates.Count - 1);
                            state = ExecutionState.Rethrow;
                            if (ctx.EHStates.Count == 0)
                                HandleRethrow(ctx, ehState.ExceptionObj);
                        }
                        else
                            state = ExecutionState.Next;
                        break;
                    }
                case EHState.EHProcess.Unwinding:
                    {
                        ehState.CurrentFrame--;
                        int i;
                        for (i = ehState.CurrentFrame.Value; i > ehState.HandlerFrame.Value; i--)
                        {
                            var frame = ctx.EHStack[i];
                            ctx.EHStack.RemoveAt(i);
                            if (frame.EHType == ctx.Instance.Data.Constants.EH_FAULT || frame.EHType == ctx.Instance.Data.Constants.EH_FINALLY)
                            {
                                // Run finally
                                SetupFinallyFrame(ctx, frame);
                                break;
                            }
                        }
                        ehState.CurrentFrame = i;

                        if (ehState.CurrentFrame == ehState.HandlerFrame)
                        {
                            var frame = ctx.EHStack[ehState.HandlerFrame.Value];
                            ctx.EHStack.RemoveAt(ehState.HandlerFrame.Value);
                            // Run handler
                            frame.SP.U4++;
                            ctx.Stack.SetTopPosition(frame.SP.U4);
                            ctx.Stack[frame.SP.U4] = new VMSlot { O = ehState.ExceptionObj };

                            ctx.Registers[ctx.Data.Constants.REG_K1].U1 = 0;
                            ctx.Registers[ctx.Data.Constants.REG_SP] = frame.SP;
                            ctx.Registers[ctx.Data.Constants.REG_BP] = frame.BP;
                            ctx.Registers[ctx.Data.Constants.REG_IP].U8 = frame.HandlerAddr;

                            ctx.EHStates.RemoveAt(ctx.EHStates.Count - 1);
                        }
                        state = ExecutionState.Next;
                        break;
                    }
                default:
                    throw new ExecutionEngineException();
            }
        }

        static void HandleAbnormalExit(VMContext ctx)
        {
            var oldBP = ctx.Registers[ctx.Data.Constants.REG_BP];
            var oldSP = ctx.Registers[ctx.Data.Constants.REG_SP];

            for (int i = ctx.EHStack.Count - 1; i >= 0; i--)
            {
                var frame = ctx.EHStack[i];
                if (frame.EHType == ctx.Instance.Data.Constants.EH_FAULT || frame.EHType == ctx.Instance.Data.Constants.EH_FINALLY)
                {
                    SetupFinallyFrame(ctx, frame);
                    Invoke(ctx);
                }
            }
            ctx.EHStack.Clear();
        }

        static void SetupFinallyFrame(VMContext ctx, EHFrame frame)
        {
            frame.SP.U4++;
            ctx.Registers[ctx.Data.Constants.REG_K1].U1 = 0;
            ctx.Registers[ctx.Data.Constants.REG_SP] = frame.SP;
            ctx.Registers[ctx.Data.Constants.REG_BP] = frame.BP;
            ctx.Registers[ctx.Data.Constants.REG_IP].U8 = frame.HandlerAddr;

            ctx.Stack[frame.SP.U4] = new VMSlot { U8 = 1 };
        }
    }
}