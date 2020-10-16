/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;

namespace ServiceUtilities.Process.Geometry
{
    public class VertexNormalTangent
    {
        public const string VERTEX_PROPERTY = "vertex";
        public const string NORMAL_PROPERTY = "normal";
        public const string TANGENT_PROPERTY = "tangent";

        [JsonProperty(VERTEX_PROPERTY)]
        public Vector3D Vertex = new Vector3D();

        [JsonProperty(NORMAL_PROPERTY)]
        public Vector3D Normal = new Vector3D();

        [JsonProperty(TANGENT_PROPERTY)]
        public Vector3D Tangent = new Vector3D();

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is VertexNormalTangent Casted)) return false;

            if (!Vertex.Equals(Casted.Vertex)) return false;
            if (!Normal.Equals(Casted.Normal)) return false;
            if (!Tangent.Equals(Casted.Tangent)) return false;

            return true;
        }

        public static VertexNormalTangent FromBytes(byte[] _Bytes, ref int _Head)
        {
            return new VertexNormalTangent()
            {
                Vertex = Vector3D.FromBytes(_Bytes, ref _Head),
                Normal = Vector3D.FromBytes(_Bytes, ref _Head),
                Tangent = Vector3D.FromBytes(_Bytes, ref _Head)
            };
        }
    }
}