using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.DataSyncHelpers
{
    /*
     * Interface for struct that can be converted to/from bytes with custom code
     * To avoid allocations while using ByteArrayTools.ByteArrayRepresentationLength on a IByteArraySerializable type, add a [ByteArray(SIZE)] attribute before the struct with the designed serialization size
     * (in debug, a warning will suggest the appropriate size to set if the attribute is not already added)
     */
    public interface IByteArraySerializable
    {
        void FillFromBytes(byte[] entryBytes);
        byte[] AsByteArray { get; }
    }

    public static class ByteArrayTools
    {
        public static string PreviewString(byte[] a)
        {
            string dataStr = "";
            for (int i = 0; i < a.Length; i++)
            {
                dataStr += $"{(dataStr == "" ? "" : "|")}{i}: {a[i]}";
                if (i > 100)
                {
                    dataStr += " ...";
                    break;
                }
            }
            return $"[{a.Length} bytes] {dataStr}";
        }

        public static void FillWithRandomInt(ref byte[] data, int size, int rangeMin = 0, int rangeMax = 100)
        {
            if (data != null && data.Length != size)
            {
                data = new byte[size];
            }
            for (int i = 0; i < size; i++)
            {
                data[i] = (byte)UnityEngine.Random.Range(rangeMin, rangeMax);
            }
        }

        public static Dictionary<(int width, int height), Texture2D> CachedRenderTextureCopyTextures = new Dictionary<(int width, int height), Texture2D>();
        public static byte[] TextureData(RenderTexture rt)
        {

            var currentRt = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D cachedRenderTextureCopyTexture = null;
            if (CachedRenderTextureCopyTextures.ContainsKey((rt.width, rt.height)))
            {
                Debug.LogError("Reusing cached texture");
                cachedRenderTextureCopyTexture = CachedRenderTextureCopyTextures[(rt.width, rt.height)];
            }
            else
            {
                cachedRenderTextureCopyTexture = new Texture2D(rt.width, rt.height);
            }

            cachedRenderTextureCopyTexture.ReadPixels(new Rect(0, 0, cachedRenderTextureCopyTexture.width, cachedRenderTextureCopyTexture.height), 0, 0);
            cachedRenderTextureCopyTexture.Apply();
            RenderTexture.active = currentRt;
            return TextureData(cachedRenderTextureCopyTexture);
        }


        public static byte[] TextureData(Texture2D texture)
        {
            return texture.EncodeToJPG();
        }

        // Fills a texture with the given data
        //  Note that you have to manage freeing the TExture2D memory with UnityEngine.Object.Destroy(tex) whenever it is not needed anymore
        public static void FillTexture(ref Texture2D tex, byte[] data)
        {
            if (tex == null)
            {
                // LoadImage sets the proper size
                tex = new Texture2D(2, 2);
            }

            tex.LoadImage(data);
        }

        #region Byte array splitting
        static List<Type> unoptimizedTypes = new List<Type>();

        /*
         * Return the serialization size of this type, as return by AsByteArray
         *
         * To avoid allocations while using ByteArrayTools.ByteArrayRepresentationLength on a IByteArraySerializable type, add a[ByteArray(SIZE)] attribute before the struct with the designed serialization size
         *  Otherwise, a sample instance will be allocated (and a warning will appear in debug, with the suggested size)
         */
        public static int ByteArrayRepresentationLength<T>() where T : unmanaged, IByteArraySerializable
        {
            var t = typeof(T);
            foreach (Attribute attr in Attribute.GetCustomAttributes(t))
            {
                if (attr is ByteArraySizeAttribute a)
                {
                    return a.Size;
                }
            }
            T sampleEntry = new T();
            var entrySize = sampleEntry.AsByteArray.Length;
#if DEBUG
            if(unoptimizedTypes.Contains(t) == false)
            {
                Debug.LogWarning($"To optimize alloc, add [ByteArraySize({entrySize})] before {t.Name} definition");
                unoptimizedTypes.Add(t);
            }
#endif
            return entrySize;
        }

        // Split new bytes in T entries, ensuring that the new data ends with a full entry, with the remainging bytes at the start, not long enough to fill an entry, returned in newPaddingStartBytes
        unsafe public static void Split<T>(byte[] newBytes, out byte[] newPaddingStartBytes, out T[] newEntries) where T : unmanaged, IByteArraySerializable
        {
            Split<T>(newBytes, out newPaddingStartBytes, out newEntries, out _);
        }

        // Split new bytes in T entries, ensuring that the new data ends with a full entry, with the remainging bytes at the start, not long enough to fill an entry, returned in newPaddingStartBytes
        unsafe public static void Split<T>(byte[] newBytes, out byte[] newPaddingStartBytes, out T[] newEntries, out int entrySize) where T : unmanaged, IByteArraySerializable
        {
            entrySize = ByteArrayRepresentationLength<T>();
            var entryCount = newBytes.Length / entrySize;
            int paddingLength = newBytes.Length - entryCount * entrySize;
            newPaddingStartBytes = new byte[paddingLength];
            newEntries = new T[entryCount];
            int cursor = 0;
            if (paddingLength > 0)
            {
                System.Buffer.BlockCopy(newBytes, 0, newPaddingStartBytes, 0, paddingLength);
                cursor = paddingLength;
            }
            if (paddingLength != newBytes.Length)
            {
                int entryIndex = 0;
                while (cursor < newBytes.Length)
                {
                    byte[] entryBytes = new byte[entrySize];
                    System.Buffer.BlockCopy(newBytes, cursor, entryBytes, 0, entrySize);
                    T entry = new T();
                    entry.FillFromBytes(entryBytes);
                    newEntries[entryIndex] = entry;
                    entryIndex++;
                    cursor += entrySize;
                }
            }
        }
        #endregion
    }

    [System.AttributeUsage(System.AttributeTargets.Struct)]
    public class ByteArraySizeAttribute : Attribute {
        public int Size;

        public ByteArraySizeAttribute(int size)
        {
            Size = size;
        }
    }
}
