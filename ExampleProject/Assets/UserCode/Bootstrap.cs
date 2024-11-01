using System;
using Unity.Burst;
using UnityEngine;
using UnityUnmanaged;

[BurstCompile]
public class Bootstrap : MonoBehaviour
{
    public Texture Texture;
    public void Start()
    {
        TextureHelper.Init();

        // Access
        //  Texture.mipmapCount
        // but in Burst:
        TextureHandle handle = TextureHelper.GetTextureHandle(Texture);
        int mips = GetMipMapCount(handle);

        Debug.Log($"Mip count: {mips}");

        Span<char> chars = stackalloc char[10];
        chars[0] = 'H';
        chars[1] = 'e';
        chars[2] = 'l';
        chars[3] = 'l';
        chars[4] = 'o';
        unsafe
        {
            fixed (char* cs = chars)
            {
                SetName(handle, cs, 5);
            }
        }

        Debug.Log($"New texture name: {Texture.name}");
    }

    [BurstCompile]
    public static int GetMipMapCount(in TextureHandle texture)
    {
        return TextureHelper.GetMipmapCount(texture);
    }

    [BurstCompile]
    public static unsafe void SetName(in TextureHandle texture, void* chars, int length)
    {
        TextureHelper.SetName(texture, chars, length);
    }
}