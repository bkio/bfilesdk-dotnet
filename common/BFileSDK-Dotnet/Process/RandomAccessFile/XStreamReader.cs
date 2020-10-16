﻿/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ServiceUtilities.Process.RandomAccessFile
{
    public class XStreamReader : IDisposable
    {
        private readonly ENodeType FileType;
        private readonly Action<uint> OnFileSDKVersionRead;
        private readonly Action<Node> OnNodeRead_TS;
        
        private readonly EDeflateCompression Compression;
        private readonly GZipStream DecompressionStream;
        private readonly Stream InnerStream;

        public const int MaximumChunkSize = 8192;

        public XStreamReader(ENodeType _FileType, Stream _InnerStream, Action<uint> _OnFileSDKVersionRead, Action<Node> _OnNodeRead_TS, EDeflateCompression _Compression)
        {
            FileType = _FileType;
            OnFileSDKVersionRead = _OnFileSDKVersionRead;
            OnNodeRead_TS = _OnNodeRead_TS;
            InnerStream = _InnerStream;

            Compression = _Compression;
            if(Compression == EDeflateCompression.Compress)
            {
                DecompressionStream = new GZipStream(InnerStream, CompressionMode.Decompress);
            }

            ProcessThread = new Thread(Process_Runnable);
            ProcessThread.Start();
        }

        public bool Process(Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var ReadChunk = new byte[MaximumChunkSize];

                while (true)
                {
                    int ReadCount;

                    if (Compression == EDeflateCompression.Compress)
                    {
                        ReadCount = DecompressionStream.Read(ReadChunk, 0, MaximumChunkSize);
                    }
                    else
                    {
                        ReadCount = InnerStream.Read(ReadChunk, 0, MaximumChunkSize);
                    }

                    if (ReadCount <= 0)
                        break;

                    Write(ReadChunk, 0, ReadCount);
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("XStreamReader: " + e.Message + ", trace:" + e.StackTrace);
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            bInnerStreamReadCompleted = true;
            try
            {
                ThreadOperationCompletedEvent.WaitOne();
                ThreadOperationCompletedEvent.Close();
            }
            catch (Exception) { }

            try
            {
                if (DecompressionStream != null)
                {
                    DecompressionStream.Dispose();
                }
            }
            catch (Exception) { }

            Flush();
        }

        private void Flush()
        {
            WaitingDataBlockQueue_Header_TotalSize = 0;
            WaitingDataBlockQueue_Header.Clear();

            UnprocessedDataSize = 0;
            UnprocessedDataQueue.Clear();
        }

        private void Write(byte[] _Buffer, int _Offset, int _Count)
        {
            int Index = _Offset;
            int RemainedBytes = _Count;
            while (RemainedBytes > 0)
            {
                var BytesToProcess = Math.Min(RemainedBytes, MaximumChunkSize);

                Process(_Buffer, Index, BytesToProcess);
                
                RemainedBytes -= BytesToProcess;
                Index += BytesToProcess;
            }
        }
        
        private bool bHeaderRead = false;

        private int WaitingDataBlockQueue_Header_TotalSize = 0;
        private readonly Queue<byte[]> WaitingDataBlockQueue_Header = new Queue<byte[]>();

        private Thread ProcessThread;

        private readonly ConcurrentQueue<byte[]> UnprocessedDataQueue = new ConcurrentQueue<byte[]>();
        private int UnprocessedDataSize = 0;

        private bool bInnerStreamReadCompleted = false;
        private readonly ManualResetEvent ThreadOperationCompletedEvent = new ManualResetEvent(false);

        private void Process(byte[] _Buffer, int _Offset, int _Count)
        {
            if (!bHeaderRead)
            {
                if ((WaitingDataBlockQueue_Header_TotalSize + _Count) >= FileHeader.HeaderSize)
                {
                    var CurrentBlock = new byte[WaitingDataBlockQueue_Header_TotalSize > 0 ? (WaitingDataBlockQueue_Header_TotalSize + _Count) : _Count];

                    int CurrentIx = 0;
                    while (WaitingDataBlockQueue_Header.TryDequeue(out byte[] WaitingBlock))
                    {
                        for (int i = 0; i < WaitingBlock.Length; i++)
                        {
                            CurrentBlock[CurrentIx++] = WaitingBlock[i];
                        }
                        WaitingDataBlockQueue_Header_TotalSize -= WaitingBlock.Length;
                    }
                    for (int i = _Offset; i < _Count; i++)
                    {
                        CurrentBlock[CurrentIx++] = _Buffer[i];
                    }

                    FileHeader.ReadHeader(out uint FileSDKVersion, CurrentBlock);
                    OnFileSDKVersionRead(FileSDKVersion);

                    if (CurrentBlock.Length > FileHeader.HeaderSize)
                    {
                        int Count = CurrentBlock.Length - FileHeader.HeaderSize;
                        var Rest = new byte[Count];
                        Buffer.BlockCopy(CurrentBlock, FileHeader.HeaderSize, Rest, 0, Count);

                        UnprocessedDataQueue.Enqueue(Rest);
                        Interlocked.Add(ref UnprocessedDataSize, Rest.Length);
                    }

                    bHeaderRead = true;
                }
                else
                {
                    var CurrentBuffer = new byte[_Count];
                    Buffer.BlockCopy(_Buffer, _Offset, CurrentBuffer, 0, _Count);
                    WaitingDataBlockQueue_Header.Enqueue(CurrentBuffer);
                    WaitingDataBlockQueue_Header_TotalSize += _Count;
                }
            }
            else
            {
                var CurrentBuffer = new byte[_Count];
                Buffer.BlockCopy(_Buffer, _Offset, CurrentBuffer, 0, _Count);

                UnprocessedDataQueue.Enqueue(CurrentBuffer);
                Interlocked.Add(ref UnprocessedDataSize, _Count);
            }
        }

        private void Process_Runnable()
        {
            Thread.CurrentThread.IsBackground = true;
            
            try
            {
                Process_Internal();
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    ProcessThread = new Thread(Process_Runnable)
                    {
                        Priority = ThreadPriority.Highest
                    };
                    ProcessThread.Start();
                }
            }
        }

        private void Process_Internal()
        {
            byte[] FailedLeftOverBlock = null;

            while (true)
            {
                int ProcessedBufferCount = 0;

                var UnprocessedDataSize_Current = UnprocessedDataSize;
                if (UnprocessedDataSize_Current > 0)
                {
                    var CurrentBuffer = new byte[(FailedLeftOverBlock != null ? FailedLeftOverBlock.Length : 0) + UnprocessedDataSize_Current];
                    if (FailedLeftOverBlock != null)
                    {
                        Buffer.BlockCopy(FailedLeftOverBlock, 0, CurrentBuffer, 0, FailedLeftOverBlock.Length);
                    }
                    int CurrentIndex = FailedLeftOverBlock != null ? FailedLeftOverBlock.Length : 0;
                    FailedLeftOverBlock = null;

                    while (UnprocessedDataQueue.TryPeek(out byte[] CurrentBlock) && CurrentIndex < CurrentBuffer.Length)
                    {
                        Buffer.BlockCopy(CurrentBlock, 0, CurrentBuffer, CurrentIndex, CurrentBlock.Length);
                        CurrentIndex += CurrentBlock.Length;
                        Interlocked.Add(ref UnprocessedDataSize, -1 * CurrentBlock.Length);
                        ProcessedBufferCount++;

                        UnprocessedDataQueue.TryDequeue(out byte[] _);
                    }

                    var SuccessOffset = ReadUntilFailure(CurrentBuffer);
                    if (SuccessOffset == -1) continue;

                    FailedLeftOverBlock = new byte[CurrentBuffer.Length - SuccessOffset];
                    Buffer.BlockCopy(CurrentBuffer, SuccessOffset, FailedLeftOverBlock, 0, FailedLeftOverBlock.Length);
                }
                if (bInnerStreamReadCompleted && UnprocessedDataQueue.Count == 0 && UnprocessedDataSize == 0)
                {
                    try
                    {
                        ThreadOperationCompletedEvent.Set();
                    }
                    catch (Exception) { }
                    return;
                }
                if (ProcessedBufferCount < 32)
                {
                    Thread.Sleep(50);
                }
            }
        }

        //Returns -1 on full success on reading
        private int ReadUntilFailure(byte[] _Input)
        {
            int SuccessOffset = 0;

            while (SuccessOffset < _Input.Length)
            {
                Node NewNode = null;
                try
                {
                    var Offset = Convert.BufferToNode(out NewNode, FileType, _Input, SuccessOffset);
                    SuccessOffset += Offset;
                }
                catch (Exception ex)
                {
                    if (ex is IndexOutOfRangeException || ex is ArgumentException)
                    {
                        return SuccessOffset;
                    }
                    throw;
                }

                BTaskWrapper.Run(() =>
                {
                    OnNodeRead_TS(NewNode);
                });
            }
            return -1;
        }
    }
}