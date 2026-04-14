using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Descriptor for a ResGroup inside a RSB  </summary>

[StructLayout(LayoutKind.Explicit, Size = 204)]

public unsafe struct RsbGroupDescriptor
{
/// <summary> Group name </summary>

[FieldOffset(0)]
public fixed byte Name[128];

/// <summary> Offset to RSG </summary>

[FieldOffset(128)]
public uint Offset;

/// <summary> Size in bytes of RSG </summary>

[FieldOffset(132)]
public uint Size;

/// <summary> Group index inside pool </summary>

[FieldOffset(136)]
public uint Index;

/// <summary> Compression flags </summary>

[FieldOffset(140)]
public uint CompressionFlags;

/// <summary> Header length </summary>

[FieldOffset(144)]
public uint HeaderLength;

/// <summary> Offset to ResidentData </summary>

[FieldOffset(148)]
public uint ResidentDataOffset;

/// <summary> ResidentData Size (after Compression) </summary>

[FieldOffset(152)]
public uint ResidentDataSizeCompressed;

/// <summary> ResidentData Size (before Compression) </summary>

[FieldOffset(156)]
public uint ResidentDataSize;

/// <summary> Amount of bytes ocupied by ResidentData inside Pool </summary>

[FieldOffset(160)]
public uint ResidentPoolSize;

/// <summary> Offset to GPUData </summary>

[FieldOffset(164)]
public uint GPUDataOffset;

/// <summary> GPUData Size (after Compression) </summary>

[FieldOffset(168)]
public uint GPUDataSizeCompressed;

/// <summary> GPUData Size (before Compression) </summary>

[FieldOffset(172)]
public uint GPUDataSize;

/// <summary> Amount of bytes ocupied by GPUData inside Pool (always 0) </summary>

[FieldOffset(176)]
public readonly uint GPUPoolSize;

/// <summary> Some padding </summary>

[FieldOffset(180)]
private fixed uint Padding[4];

/// <summary> Amount of textures embedded (used in V3-V4) </summary>

[FieldOffset(196)]
public uint TextureCount;

/// <summary> Offset to Texture descriptor (used in V3-V4) </summary>

[FieldOffset(200)]
public uint PtxDescriptorOffset;

// Read GroupDescriptor

public static RsbGroupDescriptor Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[204];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbGroupDescriptor>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write PoolDescriptor

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[204];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
Offset = BinaryPrimitives.ReverseEndianness(Offset);

Size = BinaryPrimitives.ReverseEndianness(Size);
Index = BinaryPrimitives.ReverseEndianness(Index);

CompressionFlags = BinaryPrimitives.ReverseEndianness(CompressionFlags);
HeaderLength = BinaryPrimitives.ReverseEndianness(HeaderLength);

ResidentDataOffset = BinaryPrimitives.ReverseEndianness(ResidentDataOffset);
ResidentDataSizeCompressed = BinaryPrimitives.ReverseEndianness(ResidentDataSizeCompressed);
ResidentDataSize = BinaryPrimitives.ReverseEndianness(ResidentDataSize);
ResidentPoolSize = BinaryPrimitives.ReverseEndianness(ResidentPoolSize);

GPUDataOffset = BinaryPrimitives.ReverseEndianness(GPUDataOffset);
GPUDataSizeCompressed = BinaryPrimitives.ReverseEndianness(GPUDataSizeCompressed);
GPUDataSize = BinaryPrimitives.ReverseEndianness(GPUDataSize);

TextureCount = BinaryPrimitives.ReverseEndianness(TextureCount);
PtxDescriptorOffset = BinaryPrimitives.ReverseEndianness(PtxDescriptorOffset);
}

}

}