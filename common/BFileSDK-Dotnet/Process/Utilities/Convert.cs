/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using ServiceUtilities.Process.RandomAccessFile;
using System;

namespace ServiceUtilities.Process
{
    public class Convert
    {
        public static void BytesToValue(out byte _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = _Bytes[_Head];
            _Head += sizeof(byte);
        }
        public static void BytesToValue(out float _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToSingle(_Bytes, _Head);
            _Head += sizeof(float);
        }
        public static void BytesToValue(out short _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt16(_Bytes, _Head);
            _Head += sizeof(short);
        }
        public static void BytesToValue(out ushort _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt16(_Bytes, _Head);
            _Head += sizeof(ushort);
        }
        public static void BytesToValue(out int _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt32(_Bytes, _Head);
            _Head += sizeof(int);
        }
        public static void BytesToValue(out uint _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt32(_Bytes, _Head);
            _Head += sizeof(uint);
        }
        public static void BytesToValue(out long _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt64(_Bytes, _Head);
            _Head += sizeof(long);
        }
        public static void BytesToValue(out ulong _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt64(_Bytes, _Head);
            _Head += sizeof(ulong);
        }

        public static void ValueToBytes(byte _Value, byte[] _WriteToBytes, ref int _Head)
        {
            _WriteToBytes[_Head] = _Value;
            _Head += sizeof(byte);
        }
        public static void ValueToBytes(byte[] _Value, byte[] _WriteToBytes, ref int Head)
        {
            Buffer.BlockCopy(_Value, 0, _WriteToBytes, Head, _Value.Length);
            Head += _Value.Length;
        }

        public static void UniqueIDToStartIndexAndSize(ulong _UniqueID, out uint _StartIndex, out uint _Size)
        {
            _StartIndex = (uint)(_UniqueID >> 32);
            _Size = (uint)(_UniqueID & 0x00000000FFFFFFFF);
        }

        public static int BufferToNode(out Node _Result, ENodeType _NodeType, byte[] _Buffer, int _Offset = 0)
        {
            _Result = null;

            switch (_NodeType)
            {
                case ENodeType.Hierarchy:
                    _Result = new HierarchyNode();
                    break;
                case ENodeType.Geometry:
                    _Result = new GeometryNode();
                    break;
                case ENodeType.Metadata:
                    _Result = new MetadataNode();
                    break;
            }

            return _Result.FromBytes(_Buffer, _Offset);
        }
    }
}