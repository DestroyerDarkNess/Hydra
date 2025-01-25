using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxCall : HxOpCode
    {
        public override void Execute(Context ctx, HxInstruction instruction)
        {
            var obj = (string)instruction.Operand.GetObject();

            var type = Helper.ReadPrefix(obj);
            var prefix = Helper.ReadPrefix(obj, 1);
            var mdtoken = int.Parse(obj.Substring(2));

            switch (prefix)
            {
                // Constructor
                case 0:
                    {
                        var constructor = Helper.ResolveConstructor(mdtoken);
                        var parameters = Helper.GetMethodParameters(ctx, constructor);
                        var owner = constructor.IsStatic ? null : ctx.Stack.Pop()?.GetObject();

                        object instance;

                        if (type == 0)
                        {
                            instance = constructor.Invoke(owner, parameters.ToArray());
                        }
                        else
                        {
                            var proxy = Helper.CreateProxyMethodCall(constructor);

                            if (!constructor.IsStatic)
                                parameters.Insert(0, owner);

                            instance = proxy.DynamicInvoke(parameters.ToArray());
                        }

                        if (instance != null)
                            ctx.Stack.Push(instance);
                    }
                    break;
                // Method
                case 1:
                    {
                        var method = Helper.ResolveMethod(mdtoken);
                        var parameters = Helper.GetMethodParameters(ctx, method);
                        var owner = method.IsStatic ? null : ctx.Stack.Pop()?.GetObject();

                        object result;

                        if (type == 0)
                        {
                            result = method.Invoke(owner, parameters.ToArray());
                        }
                        else
                        {
                            var proxy = Helper.CreateProxyMethodCall(method);

                            if (!method.IsStatic)
                                parameters.Insert(0, owner);

                            result = proxy.DynamicInvoke(parameters.ToArray());
                        }

                        if (result != null)
                            ctx.Stack.Push(result);
                        break;
                    }
                // MemberRef
                case 2:
                    {
                        var member = Helper.ResolveMember(mdtoken);
                        var parameters = Helper.GetMethodParameters(ctx, member);
                        var owner = member.IsStatic ? null : ctx.Stack.Pop()?.GetObject();

                        object result;

                        if (type == 0)
                        {
                            result = member.Invoke(owner, parameters.ToArray());
                        }
                        else
                        {
                            try
                            {
                                var proxy = Helper.CreateProxyMethodCall(member);

                                if (!member.IsStatic)
                                    parameters.Insert(0, owner);

                                result = proxy.DynamicInvoke(parameters.ToArray());
                            } catch
                            {
                                result = member.Invoke(owner, parameters.ToArray());
                            }
                        }

                        if (result != null)
                            ctx.Stack.Push(result);
                    }
                    break;
            }
            ctx.Index++;
        }
    }
}