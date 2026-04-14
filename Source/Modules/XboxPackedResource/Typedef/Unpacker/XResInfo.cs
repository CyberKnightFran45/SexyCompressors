using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.XboxPackedResource
{
/// <summary> Stores info about a Xbox Packed resource. </summary>

[StructLayout(LayoutKind.Explicit, Size = 16)]

public struct XResInfo
{
/// <summary> Root dir where this Resource is located. </summary>

[FieldOffset(0)]
public uint RootDir;

/// <summary> File offset </summary>

[FieldOffset(4)]
public uint FileOffset;

/// <summary> File size </summary>

[FieldOffset(8)]
public uint FileSize;

/// <summary> Offset to ResName inside Path table </summary>

[FieldOffset(12)]
public uint PathOffset;

// Read ResInfo

public static XResInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[16];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<XResInfo>(rawData);
info.SwapEndian();

return info;
}

// Reverse Endianness

private void SwapEndian()
{
RootDir = BinaryPrimitives.ReverseEndianness(RootDir);
FileOffset = BinaryPrimitives.ReverseEndianness(FileOffset);

FileSize = BinaryPrimitives.ReverseEndianness(FileSize);
PathOffset = BinaryPrimitives.ReverseEndianness(PathOffset);
}

}

}