/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;

namespace ServiceUtilities.Process.RandomAccessFile
{
    public static class FileHeader
    {
        public const int HeaderSize = sizeof(uint);

        public static int WriteHeader(uint _FileSDKVersion, byte[] _File)
        {
            int Size = HeaderSize;
            Buffer.BlockCopy(BitConverter.GetBytes(_FileSDKVersion), 0, _File, 0, Size);
            return Size;
        }
        public static int ReadHeader(out uint _FileSDKVersion, byte[] _File)
        {
            _FileSDKVersion = BitConverter.ToUInt32(_File, 0);
            return HeaderSize;
        }
    }
}