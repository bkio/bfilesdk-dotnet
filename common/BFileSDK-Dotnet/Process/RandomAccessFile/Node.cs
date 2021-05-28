/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using Newtonsoft.Json;
using System;

namespace ServiceUtilities.Process.RandomAccessFile
{
    public enum ENodeType : byte
    {
        Hierarchy = 0,
        Geometry = 1,
        Metadata = 2
    };

    public abstract class Node
    {
        public const string ID_PROPERTY = "id";
        public const string SIZE_PROPERTY = "size";

        public const ulong UNDEFINED_ID = 0xFFFFFFFF00000000;

        [JsonProperty(ID_PROPERTY)]
        public ulong UniqueID = UNDEFINED_ID;
        [JsonProperty(SIZE_PROPERTY)]
        public ulong Size = 0;

        public abstract ENodeType GetNodeType();

        public override bool Equals(object _Other)
        {
            if (!(_Other is Node Casted)) return false;
            return Casted.UniqueID == UniqueID;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual int GetSize()
        {
            return sizeof(ulong);
        }
        public virtual int ToBytes(byte[] _WriteToBytes, int _Head)
        {
            int Head = _Head;
            Convert.ValueToBytes(BitConverter.GetBytes(UniqueID), _WriteToBytes, ref Head);
            //Convert.ValueToBytes(BitConverter.GetBytes(Size), _WriteToBytes, ref Head);
            return Head - _Head;
        }
        public virtual int FromBytes(byte[] _FromBytes, int _Head)
        {
            int Head = _Head;
            Convert.BytesToValue(out UniqueID, _FromBytes, ref Head);
            //Convert.BytesToValue(out Size, _FromBytes, ref Head);
            return Head - _Head;
        }
    };
}