using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Zorro.Core.Serizalization;

namespace ContentLibrary
{
    internal class TypeSerializer : BinarySerializer
    {
        public void WriteShort(short value)
        {
            NativeArray<short> nativeArray = new NativeArray<short>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            nativeArray[0] = value;
            this.WriteBytes(nativeArray.Reinterpret<byte>(UnsafeUtility.SizeOf<short>()));
            nativeArray.Dispose();
        }
    }
}
