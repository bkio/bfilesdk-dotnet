/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Text;
using Newtonsoft.Json;

namespace ServiceUtilities.Process.RandomAccessFile
{
    public class MetadataNode : Node
    {
        public override ENodeType GetNodeType()
        {
            return ENodeType.Metadata;
        }

        public const string METADATA_PROPERTY = "metadata";

        [JsonProperty(METADATA_PROPERTY)]
        public string Metadata;

        public override bool Equals(object _Other)
        {
            if (!base.Equals(_Other)) return false;
            if (!(_Other is MetadataNode Casted)) return false;
            return Casted.Metadata == Metadata;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetSize()
        {
            return base.GetSize() +
                sizeof(uint) + Encoding.UTF8.GetByteCount(Metadata);

        }
        public override int ToBytes(byte[] _WriteToBytes, int _Head)
        {
            int Head = _Head;
            Head += base.ToBytes(_WriteToBytes, Head);
            Convert.ValueToBytes(BitConverter.GetBytes(Encoding.UTF8.GetByteCount(Metadata)), _WriteToBytes, ref Head);

            Convert.ValueToBytes(Encoding.UTF8.GetBytes(Metadata), _WriteToBytes, ref Head);

            return Head - _Head;
        }
        public override int FromBytes(byte[] _FromBytes, int _Head)
        {
            int Head = _Head;
            Head += base.FromBytes(_FromBytes, Head);

            Convert.BytesToValue(out int StringSize, _FromBytes, ref Head);

            Metadata = Encoding.UTF8.GetString(_FromBytes, Head, StringSize);

            Head += StringSize;

            return Head - _Head;
        }
    }
}