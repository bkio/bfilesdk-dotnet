/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Process.Geometry
{
    public class Color
    {
        public const string R_PROPERTY = "r";
        public const string G_PROPERTY = "g";
        public const string B_PROPERTY = "b";

        [JsonProperty(R_PROPERTY)]
        public byte R = 0;

        [JsonProperty(G_PROPERTY)]
        public byte G = 0;

        [JsonProperty(B_PROPERTY)]
        public byte B = 0;

        public Color() { }
        public Color(Color _Other)
        {
            R = _Other.R;
            G = _Other.G;
            B = _Other.B;
        }
        public Color(byte _R, byte _G, byte _B)
        {
            R = _R;
            G = _G;
            B = _B;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is Color Casted)) return false;

            if (R != Casted.R) return false;
            if (G != Casted.G) return false;
            if (B != Casted.B) return false;

            return true;
        }

        public static Color FromBytes(byte[] _Bytes, ref int _Head)
        {
            var Result = new Color();
            Convert.BytesToValue(out Result.R, _Bytes, ref _Head);
            Convert.BytesToValue(out Result.G, _Bytes, ref _Head);
            Convert.BytesToValue(out Result.B, _Bytes, ref _Head);
            return Result;
        }
    };
}