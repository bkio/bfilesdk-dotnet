/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Process.Geometry
{
    public class Vector3D
    {
        public const string X_PROPERTY = "x";
        public const string Y_PROPERTY = "y";
        public const string Z_PROPERTY = "z";

        [JsonProperty(X_PROPERTY)]
        public float X = 0.0f;

        [JsonProperty(Y_PROPERTY)]
        public float Y = 0.0f;

        [JsonProperty(Z_PROPERTY)]
        public float Z = 0.0f;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is Vector3D Casted)) return false;

            if (X != Casted.X) return false;
            if (Y != Casted.Y) return false;
            if (Z != Casted.Z) return false;

            return true;
        }

        public Vector3D() { }
        public Vector3D(Vector3D _Other)
        {
            X = _Other.X;
            Y = _Other.Y;
            Z = _Other.Z;
        }
        public Vector3D(float _X, float _Y, float _Z)
        {
            X = _X;
            Y = _Y;
            Z = _Z;
        }

        public static Vector3D FromBytes(byte[] _Bytes, ref int _Head)
        {
            var Result = new Vector3D();
            Convert.BytesToValue(out Result.X, _Bytes, ref _Head);
            Convert.BytesToValue(out Result.Y, _Bytes, ref _Head);
            Convert.BytesToValue(out Result.Z, _Bytes, ref _Head);
            return Result;
        }
    };
}