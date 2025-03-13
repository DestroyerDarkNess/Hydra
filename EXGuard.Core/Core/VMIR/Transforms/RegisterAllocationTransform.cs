using EXGuard.Core.VMIR.RegAlloc;

namespace EXGuard.Core.VMIR.Transforms
{
    public class RegisterAllocationTransform : ITransform
    {
        public static readonly object RegAllocatorKey = new object();
        private RegisterAllocator allocator;

        public void Initialize(IRTransformer tr)
        {
            allocator = new RegisterAllocator(tr);
            allocator.Initialize();
            tr.Annotations[RegAllocatorKey] = allocator;
        }

        public void Transform(IRTransformer tr)
        {
            allocator.Allocate(tr.Block);
        }
    }
}