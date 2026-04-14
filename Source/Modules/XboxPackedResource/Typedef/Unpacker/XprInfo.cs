using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.XboxPackedResource
{
/// <summary> Stores info related to a Xbox Package. </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public struct XprInfo
{
/// <summary> Total file size </summary>

[FieldOffset(0)]
public uint TotalSize;

/// <summary> GPU section length (always 0) </summary>

[FieldOffset(4)]
public uint GPUDataSize;

/// <summary> Amount of files embedded </summary>

[FieldOffset(8)]
public uint FileCount;

// Read XprInfo

public static XprInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<XprInfo>(rawData);
info.SwapEndian();

return info;
}

// Reverse Endianness

private void SwapEndian()
{
TotalSize = BinaryPrimitives.ReverseEndianness(TotalSize);

GPUDataSize = BinaryPrimitives.ReverseEndianness(GPUDataSize);
FileCount = BinaryPrimitives.ReverseEndianness(FileCount);
}

}

}