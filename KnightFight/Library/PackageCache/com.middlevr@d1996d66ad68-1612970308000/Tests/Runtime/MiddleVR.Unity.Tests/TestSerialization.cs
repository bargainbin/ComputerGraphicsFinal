using System.Collections.Generic;
using NUnit.Framework;
using MiddleVR.Unity.Serialization;
using System;
using System.IO;
using UnityEngine;

public class TestSerialization
{
    #region Helper methods
    private void RoundTripOverwrite<T>(T inputValue, ref T outputValue)
    {
        byte[] arr = null;
        using (var ms = new MemoryStream())
        {
            using (var bw = new BinaryWriter(ms))
            {
                SerializationUtil.Write(bw, ref inputValue);
            }
            arr = ms.ToArray();
        }

        using (var ms = new MemoryStream(arr))
        {
            using (var br = new BinaryReader(ms))
            {
                SerializationUtil.Read(br, ref outputValue);
            }
            arr = ms.ToArray();
        }
    }

    private T RoundTrip<T>(T inputValue)
    {
        var outputValue = default(T);
        RoundTripOverwrite(inputValue, ref outputValue);
        return outputValue;
    }
    #endregion

    #region Basic types
    [Test]
    public void BasicTypes()
    {
        Assert.AreEqual(true, RoundTrip(true)); // bool
        Assert.AreEqual(false, RoundTrip(false));
        Assert.AreEqual((byte)241, RoundTrip((byte)241)); // byte
        Assert.AreEqual((char)'A', RoundTrip('A')); // char
        Assert.AreEqual(10.10, RoundTrip(10.10)); // double
        Assert.AreEqual(0, RoundTrip(0)); // int
        Assert.AreEqual(1337, RoundTrip(1337));
        Assert.AreEqual(0L, RoundTrip(0L)); // long
        Assert.AreEqual(1337L, RoundTrip(1337L));
    }
    #endregion

    #region Strings
    [Test]
    public void String()
    {
        Assert.AreEqual("", RoundTrip(""));
        Assert.AreEqual("Test", RoundTrip("Test"));
        Assert.AreEqual((string)null, RoundTrip((string)null));
    }
    #endregion

    #region Blittable

    [Serializable]
    struct BlittableStruct
    {
        public bool TestBool;
        public int TestInt;
        public double TestDouble;

        public override bool Equals(object obj)
        {
            if (!(obj is BlittableStruct))
                return false;
            var other = (BlittableStruct)obj;

            return
                TestBool == other.TestBool &&
                TestInt == other.TestInt &&
                TestDouble == other.TestDouble;
        }

        public override int GetHashCode()
        {
            return TestBool.GetHashCode() ^ TestInt.GetHashCode() ^ TestDouble.GetHashCode();
        }
    }

    [Test]
    public void Blittable()
    {
        var value = new BlittableStruct { TestBool = true, TestInt = 1337, TestDouble = 10.10 };
        Assert.AreEqual(value, RoundTrip(value));
    }
    #endregion

    #region Class
    class NestedNonSerializableClass
    {
        public string Value = "";
    }

    [Serializable]
    class NestedSerializableClass
    {
        public string Value = "";
    }

    struct NestedUnmanagedStruct
    {
        public Vector3 Value;
    }

    [Serializable]
    class SerializableClass
    {
        public NestedNonSerializableClass NestedNonSClass = new NestedNonSerializableClass();

        public NestedSerializableClass NestedSClass = new NestedSerializableClass();

        public NestedUnmanagedStruct NUStruct;

        // List is a known collection
        public List<Vector3> Vectors = new List<Vector3>();

        public List<Vector3> VectorsNull = null;

        // Inherits from Unity.Object, reference will not be serialized
        public GameObject GO = null;

        public string Str = "";

        // NonSerialized Attribute should prevent serialization
        [NonSerialized]
        public int PublicNonSerialized = 0;

        // SerializeField enables serialization even on Non-public members
        [SerializeField]
        private int _privateSerialized = 0;

        public void SetPrivateValue(int i)
        {
            _privateSerialized = i;
        }

        public bool PrivateEquals(SerializableClass other)
        {
            return other._privateSerialized == _privateSerialized;
        }
    }

    [Test]
    public void Class()
    {
        var s = new SerializableClass();
        s.SetPrivateValue(1337);
        s.PublicNonSerialized = 6538723;
        s.Str = null;
        s.Vectors = new List<Vector3>() { Vector3.zero, Vector3.up, Vector3.one };
        s.NUStruct.Value = Vector3.one;
        s.NestedSClass.Value = "String in serializable nested class";
        s.NestedNonSClass.Value = "String in non-serializable nested class";

        var o = RoundTrip(s);
        Assert.That(o.PrivateEquals(s));
        Assert.AreEqual(0, o.PublicNonSerialized);
        Assert.Null(o.Str);
        Assert.AreEqual(Vector3.zero, o.Vectors[0]);
        Assert.AreEqual(Vector3.up, o.Vectors[1]);
        Assert.AreEqual(Vector3.one, o.Vectors[2]);
        Assert.AreEqual(Vector3.one, o.NUStruct.Value);
        Assert.AreEqual("String in serializable nested class", o.NestedSClass.Value);
        Assert.AreEqual("", o.NestedNonSClass.Value);
    }
    #endregion

    #region Unity math types
    [Test]
    public void UnityMathTypes()
    {
        Assert.That(BlittableSerializer.IsUnmanaged<Vector2>());
        Assert.That(BlittableSerializer.IsUnmanaged<Vector2Int>());
        Assert.That(BlittableSerializer.IsUnmanaged<Vector3>());
        Assert.That(BlittableSerializer.IsUnmanaged<Vector3Int>());
        Assert.That(BlittableSerializer.IsUnmanaged<Vector4>());
        Assert.That(BlittableSerializer.IsUnmanaged<Quaternion>());
        Assert.That(BlittableSerializer.IsUnmanaged<Rect>());
        Assert.That(BlittableSerializer.IsUnmanaged<RectInt>());
        Assert.That(BlittableSerializer.IsUnmanaged<Color>());
        Assert.That(BlittableSerializer.IsUnmanaged<Color32>());
        Assert.That(BlittableSerializer.IsUnmanaged<Matrix4x4>());
    }
    #endregion

    #region Test forbidden types
    // class without [Serialized]
    class NotSerializableClass
    {
    }

    // non-blittable struct without [Serialized]
    struct NotSerializableStruct
    {
#pragma warning disable CS0649
        public object Object;
#pragma warning restore
    }

    [Test]
    public void ForbiddenTypes()
    {
        Assert.Null(SerializationRegistry<NotSerializableClass>.Serializer);
        Assert.Null(SerializationRegistry<NotSerializableStruct>.Serializer);
    }
    #endregion
}
