/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Process.Geometry
{
    public class Vector2D
    {
        public const string X_PROPERTY = "x";
        public const string Y_PROPERTY = "y";

        [JsonProperty(X_PROPERTY)]
        public float X = 0.0f;

        [JsonProperty(Y_PROPERTY)]
        public float Y = 0.0f;

        public Vector2D() { }
        public Vector2D(Vector2D _Other)
        {
            X = _Other.X;
            Y = _Other.Y;
        }
        public Vector2D(float _X, float _Y)
        {
            X = _X;
            Y = _Y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is Vector2D Casted)) return false;

            if (X != Casted.X) return false;
            if (Y != Casted.Y) return false;

            return true;
        }

        public static Vector2D FromBytes(byte[] _Bytes, ref int _Head)
        {
            var Result = new Vector2D();
            Convert.BytesToValue(out Result.X, _Bytes, ref _Head);
            Convert.BytesToValue(out Result.Y, _Bytes, ref _Head);
            return Result;
        }
    };
}