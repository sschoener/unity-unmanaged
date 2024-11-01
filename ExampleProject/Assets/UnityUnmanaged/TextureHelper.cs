using System;
using Unity.Burst;
using UnityEngine;
using UnityExposed.Bindings;

#if !UNITY_EDITOR
using System.Reflection;
#endif

namespace UnityUnmanaged
{
    public static class TextureHelper
    {
        static bool s_Inited = false;

#if UNITY_EDITOR
        struct TagType_ResolveFromInstanceID { }
        static readonly SharedStatic<IntPtr> s_ResolveFromInstanceID_BurstFP = SharedStatic<IntPtr>.GetOrCreate<TagType_ResolveFromInstanceID>();

        delegate IntPtr ResolveFromInstanceId_Dlg(int instanceID);
        static ResolveFromInstanceId_Dlg s_ResolveFromInstanceID_GCGuard;

        [AOT.MonoPInvokeCallback(typeof(ResolveFromInstanceId_Dlg))]
        static IntPtr ResolveFromInstanceID_MonoFunc(int instanceID)
        {
            return UnityExposed.Object.GetPtrFromInstanceID<Texture>(instanceID);
        }

        static bool IsRunningBurst()
        {
            int x = 0;
            [BurstDiscard]
            static void SetToOneOutsideOfBurst(ref int x)
            {
                x = 1;
            }
            SetToOneOutsideOfBurst(ref x);
            return x == 0;
        }
#else
        static MethodInfo s_MarshalTexture;
#endif


        public static void Init()
        {
            if (s_Inited)
                return;

#if UNITY_EDITOR
            s_ResolveFromInstanceID_GCGuard = ResolveFromInstanceID_MonoFunc;
            s_ResolveFromInstanceID_BurstFP.Data = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(s_ResolveFromInstanceID_GCGuard);
#else
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            Type objectType = typeof(UnityEngine.Object);
            Type marshalledUnityObject = objectType.GetNestedType("MarshalledUnityObject", bindingFlags);

            // Get the MethodInfo for the generic Marshal method
            MethodInfo marshalFunc = marshalledUnityObject.GetMethod("Marshal", bindingFlags);
            // Specify the type argument (e.g., int) for the generic method
            s_MarshalTexture = marshalFunc.MakeGenericMethod(typeof(UnityEngine.Texture));
#endif
            s_Inited = true;
        }

        public static IntPtr Resolve(TextureHandle handle)
        {
#if UNITY_EDITOR
            // If we are running in Burst, reverse pinvoke into Mono.
            if (IsRunningBurst())
            {
                var fp = new FunctionPointer<ResolveFromInstanceId_Dlg>(s_ResolveFromInstanceID_BurstFP.Data);
                IntPtr result = fp.Invoke(handle.InstanceID);
                if (result == IntPtr.Zero)
                {
                    throw new Exception("Null reference!");
                }
                return result;
            }
            else
            {
                IntPtr result = new IntPtr();
                [BurstDiscard]
                static void MonoFunction(int instanceID, ref IntPtr result)
                {
                    result = UnityExposed.Object.GetPtrFromInstanceID<Texture>(instanceID);
                    if (result == IntPtr.Zero)
                    {
                        throw new Exception("Null reference!");
                    }
                }
                MonoFunction(handle.InstanceID, ref result);
                return result;
            }
#else
            return handle.Pointer;
#endif
        }

        public static TextureHandle GetTextureHandle(Texture texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            return new TextureHandle
            {
#if UNITY_EDITOR
                InstanceID = texture.GetInstanceID(),
#else
                Pointer = (IntPtr)s_MarshalTexture.Invoke(null, new object[] { texture })
#endif
            };
        }

        public static unsafe void SetName(TextureHandle texture, void* chars, int length)
        {
            UnityExposed.Object.SetName_Injected(Resolve(texture), new ManagedSpanWrapper(chars, length));
        }

        public static int GetMipmapCount(TextureHandle texture)
        {
            return UnityExposed.Texture.get_mipmapCount_Injected(Resolve(texture));
        }
    }
}