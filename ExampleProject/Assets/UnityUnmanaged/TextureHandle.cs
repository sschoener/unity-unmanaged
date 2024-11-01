using System;

namespace UnityUnmanaged
{
    public struct TextureHandle
    {
#if UNITY_EDITOR
        public int InstanceID;
        // Add 4 bytes of padding, assuming we're on a 64bit platform.
        public int Padding;
#else
        public IntPtr Pointer;
#endif
    }
}