using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.Addons.DataSyncHelpers
{
    public static class SerializationTools
    {
        #region Serialisation size
        const string UNKNWON_SERIALIZATION_TYPE = "Unknown type. Use float, Vector3, int, Quaternion, byte, ...";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(Vector3 pos)
        {
            return sizeof(float) * 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(Vector2 pos)
        {
            return sizeof(float) * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(float f)
        {
            return sizeof(float);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(byte b)
        {
            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(int i)
        {
            return sizeof(int);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(uint i)
        {
            return sizeof(uint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(Quaternion q)
        {
            return sizeof(float) * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(NetworkBehaviourId behaviourId)
        {
            return SerializationSize(behaviourId.Object.Raw) + SerializationSize(behaviourId.Behaviour);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(object o)
        {
            if (o is float f) return SerializationSize(f);
            if (o is Vector3 v) return SerializationSize(v);
            if (o is Vector2 v2) return SerializationSize(v2);
            if (o is uint ui) return SerializationSize(ui);
            if (o is int i) return SerializationSize(i);
            if (o is byte b)  return SerializationSize(b);
            if (o is Quaternion q) return SerializationSize(q);
            if (o is NetworkBehaviourId behaviourId) return SerializationSize(behaviourId);
            throw new Exception(UNKNWON_SERIALIZATION_TYPE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializationSize(params object[] objects)
        {
            int size = 0;
            for(int i = 0; i < objects.Length; i++)
            {
                size += SerializationSize(objects[i]);
            }
            return size;
        }

        #endregion

        #region Serialisation as byte array
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(Vector2 pos)
        {
            var repr = new byte[SerializationSize(pos)];
            System.Buffer.BlockCopy(AsByteArray(pos.x), 0, repr, 0, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(pos.y), 0, repr, sizeof(float), sizeof(float));
            return repr;
        }
        public static byte[] AsByteArray(Vector3 pos)
        {
            var repr = new byte[SerializationSize(pos)];
            System.Buffer.BlockCopy(AsByteArray(pos.x), 0, repr, 0, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(pos.y), 0, repr, sizeof(float), sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(pos.z), 0, repr, sizeof(float) * 2, sizeof(float));
            return repr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(Quaternion q)
        {
            var repr = new byte[SerializationSize(q)];
            System.Buffer.BlockCopy(AsByteArray(q.x), 0, repr, 0, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.y), 0, repr, sizeof(float), sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.z), 0, repr, sizeof(float) * 2, sizeof(float));
            System.Buffer.BlockCopy(AsByteArray(q.w), 0, repr, sizeof(float) * 3, sizeof(float));
            return repr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(float f)
        {
            return System.BitConverter.GetBytes(f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(uint f)
        {
            return System.BitConverter.GetBytes(f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(byte b)
        {
            return new byte[1] { b };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(int i)
        {
            return System.BitConverter.GetBytes(i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(NetworkBehaviourId behaviourId)
        {
            var repr = new byte[SerializationSize(behaviourId)];
            System.Buffer.BlockCopy(AsByteArray(behaviourId.Object.Raw), 0, repr, 0, SerializationSize(behaviourId.Object.Raw));
            System.Buffer.BlockCopy(AsByteArray(behaviourId.Behaviour), 0, repr, SerializationSize(behaviourId.Object.Raw), SerializationSize(behaviourId.Behaviour));
            return repr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(object o)
        {
            if (o is float f) return AsByteArray(f);
            if (o is Vector3 v) return AsByteArray(v);
            if (o is Vector2 v2) return AsByteArray(v2);
            if (o is uint ui) return AsByteArray(ui);
            if (o is byte b) return AsByteArray(b);
            if (o is Quaternion q) return AsByteArray(q);
            if (o is int i) return AsByteArray(i);
            if (o is NetworkBehaviourId bid) return AsByteArray(bid);

            throw new Exception(UNKNWON_SERIALIZATION_TYPE + " (" + o.GetType().Name + ")");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] AsByteArray(params object[] objects)
        {
            int size = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                size += SerializationSize(o);
            }
            byte[] repr = new byte[size];
            int nextWritePosition = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                var objectRepr = AsByteArray(o);
                System.Buffer.BlockCopy(objectRepr, 0, repr, nextWritePosition, objectRepr.Length);
                nextWritePosition += objectRepr.Length;
            }
            return repr;
        }
        #endregion

        #region Unserialisation from byte array to objects
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out float f)
        {
            f = default;
            int size = SerializationSize(f);
            if ((unserializePosition + size) > dataBytes.Length)
            {
                throw new Exception("Unserialize trying to recover too much data");
            }
            var objectBytes = new byte[size];
            System.Buffer.BlockCopy(dataBytes, unserializePosition, objectBytes, 0, size);
            f = System.BitConverter.ToSingle(objectBytes);
            unserializePosition += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out int i)
        {
            i = default;
            int size = SerializationSize(i);
            if ((unserializePosition + size) > dataBytes.Length)
            {
                throw new Exception("Unserialize trying to recover too much data");
            }
            var objectBytes = new byte[size];
            System.Buffer.BlockCopy(dataBytes, unserializePosition, objectBytes, 0, size);
            i = System.BitConverter.ToInt32(objectBytes);
            unserializePosition += size;
        }
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out uint i)
        {
            i = default;
            int size = SerializationSize(i);
            if ((unserializePosition + size) > dataBytes.Length)
            {
                throw new Exception("Unserialize trying to recover too much data");
            }
            var objectBytes = new byte[size];
            System.Buffer.BlockCopy(dataBytes, unserializePosition, objectBytes, 0, size);
            i = System.BitConverter.ToUInt32(objectBytes);
            unserializePosition += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out byte b)
        {
            b = default;
            int size = SerializationSize(b);
            if ((unserializePosition + size) > dataBytes.Length)
            {
                throw new Exception("Unserialize trying to recover too much data");
            }
            b = dataBytes[unserializePosition];
            unserializePosition += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out Vector3 v)
        {
            v = default;
            Unserialize(dataBytes, ref unserializePosition, out v.x);
            Unserialize(dataBytes, ref unserializePosition, out v.y);
            Unserialize(dataBytes, ref unserializePosition, out v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out Vector2 v)
        {
            v = default;
            Unserialize(dataBytes, ref unserializePosition, out v.x);
            Unserialize(dataBytes, ref unserializePosition, out v.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out Quaternion q)
        {
            q = default;
            Unserialize(dataBytes, ref unserializePosition, out q.x);
            Unserialize(dataBytes, ref unserializePosition, out q.y);
            Unserialize(dataBytes, ref unserializePosition, out q.z);
            Unserialize(dataBytes, ref unserializePosition, out q.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unserialize(byte[] dataBytes, ref int unserializePosition, out NetworkBehaviourId behaviourId)
        {
            behaviourId = default;
            Unserialize(dataBytes, ref unserializePosition, out behaviourId.Object.Raw);
            Unserialize(dataBytes, ref unserializePosition, out behaviourId.Behaviour);
        }

        public static void SerializationTest()
        {
            var v = UnityEngine.Random.insideUnitSphere;
            var v2 = UnityEngine.Random.insideUnitSphere;
            var f = UnityEngine.Random.Range(0f, 4f);
            var i = UnityEngine.Random.Range(0, 200);
            var b = (byte)UnityEngine.Random.Range(0, 255);
            var q = Quaternion.Euler(UnityEngine.Random.insideUnitSphere);
            // Serialization
            var buffer = SerializationTools.AsByteArray(v, v2, f, i, b, q, q);
            // Unserialise
            var unserializePosition = 0;
            SerializationTools.Unserialize(buffer, ref unserializePosition, out Vector3 unserializedv);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out Vector3 unserializedv2);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out float unserializedf);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out int unserializedi);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out byte unserializedb);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out Quaternion unserializedq);
            SerializationTools.Unserialize(buffer, ref unserializePosition, out Quaternion unserializedq2);
            Debug.LogError($"Source: {v} {v2} {f} {i} {b} {q} {q}");
            Debug.LogError($"Unserialized: {unserializedv} {unserializedv2} {unserializedf} {unserializedi} {unserializedb} {unserializedq} {unserializedq2}");
        }
        #endregion

        #region Serialization fallbacks
        // Avoid using this in production for ring buffers, streaming, ...: this serialization is extremely data length/bandwidth expensive
        public static byte[] AsByteArrayThroughJSON(object o)
        {
            var json = JsonUtility.ToJson(o);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        // Avoid using this in production for ring buffers, streaming, ...: this serialization is extremely data length/bandwidth expensive
        public static T UnserializeFromJSON<T>(byte[] data)
        {
            T o = default;
            string json = System.Text.Encoding.UTF8.GetString(data);
            o = JsonUtility.FromJson<T>(json);
            return o;
        }

        #endregion
    }
}
