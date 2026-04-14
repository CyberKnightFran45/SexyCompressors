using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Manifest for CompositeInfo </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public struct ManifestCompositeInfo
{
/// <summary> Offset to CompositeID </summary>

[FieldOffset(0)]
public uint IdOffset;

/// <summary> Number of Sub-Groups </summary>

[FieldOffset(4)]
public uint ChildCount;

/// <summary> Amount of bytes GroupInfo ocupies </summary>

[FieldOffset(8)]
public uint GroupInfoLength;

// ctor

public ManifestCompositeInfo(uint offset, uint childs, uint infoLen)
{
IdOffset = offset;

ChildCount = childs;
GroupInfoLength = infoLen;
}

// Read ManifestInfo

public static ManifestCompositeInfo Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestCompositeInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ManifestInfo

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[12];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
IdOffset = BinaryPrimitives.ReverseEndianness(IdOffset);

ChildCount = BinaryPrimitives.ReverseEndianness(ChildCount);
GroupInfoLength = BinaryPrimitives.ReverseEndianness(GroupInfoLength);
}

}

}