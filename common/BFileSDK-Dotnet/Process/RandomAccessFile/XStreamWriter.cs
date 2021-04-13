/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ServiceUtilities.Process.RandomAccessFile
{
    public class XStreamWriter : IDisposable
    {
        private readonly uint FileSDKVersion = 1;

        //Needs to be closed/disposed exclusively
        private readonly Dictionary<ENodeType, StreamStruct> FileTypeStreamMap;

        private HierarchyNode RootNode = null;

        private readonly ConcurrentDictionary<ulong, Node> HierarchyNodes = new ConcurrentDictionary<ulong, Node>();
        private readonly ConcurrentDictionary<ulong, Node> GeometryNodes = new ConcurrentDictionary<ulong, Node>();
        private readonly ConcurrentDictionary<ulong, Node> MetadataNodes = new ConcurrentDictionary<ulong, Node>();

        private readonly ConcurrentDictionary<ulong, ulong> HierarchyIDReplacements = new ConcurrentDictionary<ulong, ulong>();
        private readonly ConcurrentDictionary<ulong, ulong> GeometryIDReplacements = new ConcurrentDictionary<ulong, ulong>();
        private readonly ConcurrentDictionary<ulong, ulong> MetadataIDReplacements = new ConcurrentDictionary<ulong, ulong>();

        private void AddToSortQueue(Node _Node)
        {
            if (_Node.GetNodeType() == ENodeType.Hierarchy)
            {
                lock (HierarchySort_Lock)
                {
                    var HierarchyNodeSize = (ulong)_Node.GetSize();
                    _Node.UniqueID = HierarchyCurrentGlobalIndex;
                    _Node.Size = HierarchyNodeSize;
                    HierarchyCurrentGlobalIndex += HierarchyNodeSize;
                    HierarchySortedQueue.Enqueue(_Node);
                }
            }
            else if (_Node.GetNodeType() == ENodeType.Geometry)
            {
                lock (GeometrySort_Lock)
                {
                    var GeometryNodeSize = (ulong)_Node.GetSize();
                    _Node.UniqueID = GeometryCurrentGlobalIndex;
                    _Node.Size = GeometryNodeSize;
                    GeometryCurrentGlobalIndex += GeometryNodeSize;
                    GeometrySortedQueue.Enqueue(_Node);
                }
            }
            else if (_Node.GetNodeType() == ENodeType.Metadata)
            {
                lock (MetadataSort_Lock)
                {
                    var MetadataNodeSize = (ulong)_Node.GetSize();
                    _Node.UniqueID = MetadataCurrentGlobalIndex;
                    _Node.Size = MetadataNodeSize;
                    MetadataCurrentGlobalIndex += MetadataNodeSize;
                    MetadataSortedQueue.Enqueue(_Node);
                }
            }
        }
        private readonly object HierarchySort_Lock = new object();
        private readonly object GeometrySort_Lock = new object();
        private readonly object MetadataSort_Lock = new object();
        private readonly ConcurrentQueue<Node> HierarchySortedQueue = new ConcurrentQueue<Node>();
        private readonly ConcurrentQueue<Node> GeometrySortedQueue = new ConcurrentQueue<Node>();
        private readonly ConcurrentQueue<Node> MetadataSortedQueue = new ConcurrentQueue<Node>();
        private ulong HierarchyCurrentGlobalIndex = FileHeader.HeaderSize;
        private ulong GeometryCurrentGlobalIndex = FileHeader.HeaderSize;
        private ulong MetadataCurrentGlobalIndex = FileHeader.HeaderSize;

        private bool bRandomAccessGranted = false;
        private bool Writing = true;
        private ManualResetEvent WaitFor = new ManualResetEvent(false);
        private int Remaining = 3;

        public XStreamWriter(Dictionary<ENodeType, StreamStruct> _FileTypeStreamMap)
        {
            FileTypeStreamMap = _FileTypeStreamMap ?? throw new NullReferenceException("File type stream map must not be null.");

            if (!FileTypeStreamMap.ContainsKey(ENodeType.Hierarchy)
                || !FileTypeStreamMap.ContainsKey(ENodeType.Geometry)
                || !FileTypeStreamMap.ContainsKey(ENodeType.Metadata))
                throw new ArgumentException("Custom write order must contain all types.");

            BTaskWrapper.Run(() =>
            {
                WriteToStreamContinuous(FileTypeStreamMap[ENodeType.Geometry], GeometrySortedQueue);
                if (Interlocked.Decrement(ref Remaining) == 0)
                {
                    try
                    {
                        WaitFor.Set();
                    }
                    catch (Exception)
                    {

                    }
                }
            });
            BTaskWrapper.Run(() =>
            {
                WriteToStreamContinuous(FileTypeStreamMap[ENodeType.Metadata], MetadataSortedQueue);
                if (Interlocked.Decrement(ref Remaining) == 0)
                {
                    try
                    {
                        WaitFor.Set();
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        public void Write(HierarchyNode _Node)
        {
            HierarchyNodes[_Node.UniqueID] = _Node;

            if (RootNode == null && _Node.ParentID == Node.UNDEFINED_ID)
            {
                RootNode = _Node;
            }
        }
        public void Write(GeometryNode _Node)
        {
            //GeometryNodes.AddOrUpdate(_Node.UniqueID, _Node, (K,V) => V);
            ulong OldId = _Node.UniqueID;
            AddToSortQueue(_Node);
            GeometryIDReplacements[OldId] = _Node.UniqueID;
            
        }
        public void Write(MetadataNode _Node)
        {
            //MetadataNodes[_Node.UniqueID] = _Node;
            ulong OldId = _Node.UniqueID;
            AddToSortQueue(_Node);
            MetadataIDReplacements[OldId] = _Node.UniqueID;
        }

        public void Dispose()
        {
            GrantRandomAccess();

            BTaskWrapper.Run(() =>
            {
                WriteToStream(FileTypeStreamMap[ENodeType.Hierarchy], HierarchySortedQueue);
                Writing = false;
                if (Interlocked.Decrement(ref Remaining) == 0)
                {
                    try
                    {
                        WaitFor.Set();
                    }
                    catch (Exception)
                    {

                    }
                }
            });


            try
            {
                WaitFor.WaitOne();
                WaitFor.Close();
                FileTypeStreamMap[ENodeType.Hierarchy].IOStream.Flush();
                FileTypeStreamMap[ENodeType.Metadata].IOStream.Flush();
                FileTypeStreamMap[ENodeType.Geometry].IOStream.Flush();

                FileTypeStreamMap[ENodeType.Hierarchy].IOStream.Close();
                FileTypeStreamMap[ENodeType.Metadata].IOStream.Close();
                FileTypeStreamMap[ENodeType.Geometry].IOStream.Close();
            }
            catch (Exception) { }

            try
            {
                WaitForRemainedParallelTasks.Close();
            }
            catch (Exception) { }
        }

        private void WriteToStreamContinuous(StreamStruct _Stream, ConcurrentQueue<Node> _Nodes)
        {
            if (_Stream.IOCompression == EDeflateCompression.Compress)
            {
                WriteToStreamContinuous_Compress(_Stream.IOStream, _Nodes);
                _Stream.IOStream.Flush();
            }
            else if (_Stream.IOCompression == EDeflateCompression.DoNotCompress)
            {
                WriteToStreamContinuous_Base(_Stream.IOStream, _Nodes);
                _Stream.IOStream.Flush();
            }
        }

        private void WriteToStreamContinuous_Compress(Stream _Stream, ConcurrentQueue<Node> _Nodes)
        {
            using (var CompressionStream = new GZipStream(_Stream, CompressionLevel.Optimal))
            {
                WriteToStreamContinuous_Base(CompressionStream, _Nodes);
            }
        }

        private void WriteToStreamContinuous_Base(Stream _Stream, ConcurrentQueue<Node> _Nodes)
        {
            var Buffer = new byte[FileHeader.HeaderSize];
            var Head = FileHeader.WriteHeader(FileSDKVersion, Buffer);
            _Stream.Write(Buffer, 0, FileHeader.HeaderSize);

            while (Writing || _Nodes.Count > 0)
            {
                if (_Nodes.TryDequeue(out Node Current))
                {
                    var Size = Current.GetSize();


                    Buffer = new byte[Size];
                    Current.ToBytes(Buffer, 0);

                    _Stream.Write(Buffer, 0, Size);
                    Head += Size;
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void WriteToStream(StreamStruct _Stream, ConcurrentQueue<Node> _Nodes)
        {
            if (_Stream.IOCompression == EDeflateCompression.Compress)
            {
                WriteToStream_Compress(_Stream.IOStream, _Nodes);
                _Stream.IOStream.Flush();
            }
            else if (_Stream.IOCompression == EDeflateCompression.DoNotCompress)
            {
                WriteToStream_Base(_Stream.IOStream, _Nodes);
                _Stream.IOStream.Flush();
            }
        }

        private void WriteToStream_Compress(Stream _Stream, ConcurrentQueue<Node> _Nodes)
        {
            using (var CompressionStream = new GZipStream(_Stream, CompressionLevel.Optimal))
            {
                WriteToStream_Base(CompressionStream, _Nodes);
            }
        }

        private void WriteToStream_Base(Stream _Stream, ConcurrentQueue<Node> _Nodes)
        {
            var Buffer = new byte[FileHeader.HeaderSize];
            var Head = FileHeader.WriteHeader(FileSDKVersion, Buffer);
            _Stream.Write(Buffer, 0, FileHeader.HeaderSize);

            while (_Nodes.TryDequeue(out Node Current))
            {

                var Size = Current.GetSize();


                Buffer = new byte[Size];
                Current.ToBytes(Buffer, 0);

                _Stream.Write(Buffer, 0, Size);
                Head += Size;
            }
        }

        private void OnTaskCompleted()
        {
            if (Interlocked.Decrement(ref RemainedParallelTasks) == 0)
            {
                try
                {
                    WaitForRemainedParallelTasks.Set();
                }
                catch (Exception) { }
            }
        }
        private readonly ManualResetEvent WaitForRemainedParallelTasks = new ManualResetEvent(false);
        private int RemainedParallelTasks;
        private void GrantRandomAccess()
        {
            if (!bRandomAccessGranted && RootNode != null)
            {
                RemainedParallelTasks = HierarchyNodes.Count;
                GrantRandomAccessRecursive(RootNode);
                try
                {
                    WaitForRemainedParallelTasks.WaitOne();
                }
                catch (Exception) { }

                HierarchyIDReplacements.Clear();
                GeometryIDReplacements.Clear();
                MetadataIDReplacements.Clear();

                bRandomAccessGranted = true;
            }
        }
        private void GrantRandomAccessRecursive(HierarchyNode _Node, List<ulong> _SetThisListWith_NewUniqueID = null, int _AtThisIndex = -1)
        {
            BTaskWrapper.Run(() =>
            {
                var Result = GrantRandomAccessRecursive_Internal(_Node);
                if (_SetThisListWith_NewUniqueID != null && _AtThisIndex >= 0)
                {
                    _SetThisListWith_NewUniqueID[_AtThisIndex] = Result;
                }
                OnTaskCompleted();
            });
        }
        private ulong GrantRandomAccessRecursive_Internal(HierarchyNode _Node)
        {
            var OldHierarchyNodeID = _Node.UniqueID;

            HierarchyNodes.Remove(OldHierarchyNodeID, out Node _);
            AddToSortQueue(_Node);
            HierarchyIDReplacements[OldHierarchyNodeID] = _Node.UniqueID;

            for (var i = 0; i < _Node.GeometryParts.Count; ++i)
            {
                bool bSuccess = true;
                do
                {
                    if (bSuccess/*If this is first try*/ && GeometryIDReplacements.TryGetValue(_Node.GeometryParts[i].GeometryID, out ulong NewID))
                    {
                        _Node.GeometryParts[i].GeometryID = NewID;
                        bSuccess = true;
                    }
                    else
                    {
                        PowerNap(bSuccess);
                        bSuccess = false;
                    }
                } while (!bSuccess);
            }

            {
                bool bSuccess = true;
                do
                {
                    if (bSuccess/*If this is first try*/ && MetadataIDReplacements.TryGetValue(_Node.MetadataID, out ulong NewID))
                    {
                        _Node.MetadataID = NewID;
                        bSuccess = true;
                    }
                    else
                    {
                        PowerNap(bSuccess);
                        bSuccess = false;
                    }
                    
                } while (!bSuccess);
            }

            for (var i = 0; i < _Node.ChildNodes.Count; ++i)
            {
                bool bSuccess = true;
                do
                {
                    if (bSuccess/*If this is first try*/ && HierarchyNodes.TryRemove(_Node.ChildNodes[i], out Node HNode))
                    {
                        var ChildNode = (HierarchyNode)HNode;
                        ChildNode.ParentID = _Node.UniqueID;
                        GrantRandomAccessRecursive(ChildNode, _Node.ChildNodes, i);
                    }
                    else if (PowerNap(bSuccess) && HierarchyIDReplacements.TryGetValue(_Node.ChildNodes[i], out ulong NewID))
                    {
                        _Node.ChildNodes[i] = NewID;
                        bSuccess = true;
                    }
                    else bSuccess = false;
                } while (!bSuccess);
            }

            return _Node.UniqueID;
        }

        private bool PowerNap(bool _bFirstTry)
        {
            if (!_bFirstTry)
            {
                Thread.Sleep(10);
            }
            return true;
        }
    }
}