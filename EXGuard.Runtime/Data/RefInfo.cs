using System.Reflection;

namespace EXGuard.Runtime.Data
{
    internal class RefInfo
    {
        readonly double encryptKey;

        public int Token
        {
            get;
            private set;
        }

        public MemberInfo Resolved
        {
            get;
            private set;
        }

        public RefInfo(int token, double encryptKey)
        {
            Token = token;
            this.encryptKey = encryptKey;
        }

        public MemberInfo Member()
		{
			MemberInfo result;

            if ((result = Resolved) == null)
                result = Resolved = VMInstance.__ExecuteModule.ResolveMember(Utils.Decrypt(Token, encryptKey));

            return result;
		}
    }
}
