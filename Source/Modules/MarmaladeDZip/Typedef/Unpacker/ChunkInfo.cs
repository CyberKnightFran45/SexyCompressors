using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.MarmaladeDZip
{
/// <summary> Describes a Chunk inside a DZip file. </summary>

[StructLayout(LayoutKind.Explicit, Size = 30)]

public readonly struct ChunkInfo
{
/// <summary> Offset to Dir name. </summary>

[FieldOffset(0)]
public readonly ushort RootPos;

/// <summary> Offset to File name. </summary>

[FieldOffset(2)]
public readonly ushort NamePos;

/// <summary> Chuck index inside List. </summary>

[FieldOffset(4)]
public readonly ushort ChunkIndex;

/// <summary> Chuck offset inside Blob. </summary>

[FieldOffset(6)]
public readonly int ChunkOffset;

/// <summary> File Size (after Compression). </summary>

[FieldOffset(10)]
public readonly int SizeCompressed;

/// <summary> Chunck Size </summary>

[FieldOffset(14)]
public readonly int ChunkSize;

/// <summary> Compression flags </summary>

[FieldOffset(18)]
public readonly DzFlags CompressionFlags;

/// <summary> File Size (before Compression). </summary>

[FieldOffset(20)]
public readonly int RawSize;

/// <summary> Part index (for Mutipart Compression). </summary>

[FieldOffset(24)]
public readonly int PartIndex;

/// <summary> File index. </summary>

[FieldOffset(28)]
public readonly ushort FileIndex;
 
// Read ChunkInfo

public static ChunkInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[30];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ChunkInfo>(rawData);
}

}

}