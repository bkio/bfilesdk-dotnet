/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceUtilities.Process.Geometry
{
    public class LOD
    {
        public const string VNT_LIST_PROPERTY = "vertexNormalTangentList";
        public const string INDEXES_PROPERTY = "indexes";

        [JsonProperty(VNT_LIST_PROPERTY)]
        public List<VertexNormalTangent> VertexNormalTangentList = new List<VertexNormalTangent>();

        [JsonProperty(INDEXES_PROPERTY)]
        public List<uint> Indexes = new List<uint>();

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is LOD Casted)) return false;

            if (VertexNormalTangentList.Count != Casted.VertexNormalTangentList.Count) return false;
            if (Indexes.Count != Casted.Indexes.Count) return false;

            for (int i = 0; i < VertexNormalTangentList.Count; ++i)
            {
                if (!VertexNormalTangentList[i].Equals(Casted.VertexNormalTangentList[i])) return false;
            }
            for (int i = 0; i < Indexes.Count; ++i)
            {
                if (Indexes[i] != Casted.Indexes[i]) return false;
            }

            return true;
        }

        public int GetSize()
        {
            return
                sizeof(int) +
                (sizeof(float) * 9 * VertexNormalTangentList.Count) +
                sizeof(int) +
                (sizeof(uint) * Indexes.Count);
        }

        public static LOD FromBytes(byte[] _FromBytes, ref int _Head)
        {
            var Result = new LOD();

            Convert.BytesToValue(out int VNTCount, _FromBytes, ref _Head);
            for (int i = 0; i < VNTCount; ++i)
            {
                Result.VertexNormalTangentList.Add(VertexNormalTangent.FromBytes(_FromBytes, ref _Head));
            }
            Convert.BytesToValue(out int IndexCount, _FromBytes, ref _Head);
            for (var i = 0; i < IndexCount; ++i)
            {
                Convert.BytesToValue(out uint NewIndex, _FromBytes, ref _Head);
                Result.Indexes.Add(NewIndex);
            }

            return Result;
        }

        public void ToBytes(byte[] _WriteToBytes, ref int _Head)
        {
            Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList.Count), _WriteToBytes, ref _Head);
            for (int i = 0; i < VertexNormalTangentList.Count; ++i)
            {
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Vertex.X), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Vertex.Y), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Vertex.Z), _WriteToBytes, ref _Head);

                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Normal.X), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Normal.Y), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Normal.Z), _WriteToBytes, ref _Head);

                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Tangent.X), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Tangent.Y), _WriteToBytes, ref _Head);
                Convert.ValueToBytes(BitConverter.GetBytes(VertexNormalTangentList[i].Tangent.Z), _WriteToBytes, ref _Head);
            }

            Convert.ValueToBytes(BitConverter.GetBytes(Indexes.Count), _WriteToBytes, ref _Head);
            for (int i = 0; i < Indexes.Count; ++i)
            {
                Convert.ValueToBytes(BitConverter.GetBytes(Indexes[i]), _WriteToBytes, ref _Head);
            }
        }
    }
}