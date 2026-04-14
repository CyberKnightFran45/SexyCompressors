using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Stores info related to a PopCap ResGroup. </summary>

[StructLayout(LayoutKind.Explicit, Size = 88)]

public unsafe struct RsgInfo
{
/// <summary> Major Version </summary>

[FieldOffset(0)]
public RsgMajorVersion MajorVersion;

/// <summary> Minor Version </summary>

[FieldOffset(4)]
public RsgMinorVersion MinorVersion;

/// <summary> Unknown field </summary>

[FieldOffset(8)]
private readonly int Reserved;

/// <summary> Compression Flags </summary>

[FieldOffset(12)]
public uint CompressionFlags;

/** <summary> Amount of bytes Metadata Section ocupies </summary>

<remarks> Includes: <c>Header</c> + <c>ResorceMap</c> + <c>Padding</c> **/

[FieldOffset(16)]
public uint SectionLength;

/// <summary> Offset to Resident Data (also known as Part0) </summary>

[FieldOffset(20)]
public uint ResidentDataOffset;

/// <summary> ResidentData Size (after Compression) </summary>

[FieldOffset(24)]
public uint ResidentDataSizeCompressed;

/// <summary> ResidentData Size (before Compression) </summary>

[FieldOffset(28)]
public uint ResidentDataSize;

/// <summary> Unknown field </summary>

[FieldOffset(32)]
private readonly int Reserved2;

/// <summary> Offset to GPU Data (also known as Part1) </summary>

[FieldOffset(36)]
public uint GPUDataOffset;

/// <summary> GPUData Size (after Compression) </summary>

[FieldOffset(40)]
public uint GPUDataSizeCompressed;

/// <summary> GPUData Size (before Compression) </summary>

[FieldOffset(44)]
public uint GPUDataSize;

/// <summary> Some padding </summary>

[FieldOffset(48)]
private fixed int Padding[5];

/// <summary> Amount of bytes ResMap ocupies </summary>

[FieldOffset(68)]
public uint ResMapLength;

/// <summary> Offset to ResMap (usually goes after this header) </summary>

[FieldOffset(72)]
public uint ResMapOffset;

/// <summary> More padding </summary>

[FieldOffset(76)]
private fixed int Padding2[3];

// Read RsgInfo

public static RsgInfo Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[88];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsgInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write RsgInfo

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[88];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
MajorVersion = (RsgMajorVersion)BinaryPrimitives.ReverseEndianness( (uint)MajorVersion);
MinorVersion = (RsgMinorVersion)BinaryPrimitives.ReverseEndianness( (uint)MinorVersion);

CompressionFlags = BinaryPrimitives.ReverseEndianness(CompressionFlags);
SectionLength = BinaryPrimitives.ReverseEndianness(SectionLength);

ResidentDataOffset = BinaryPrimitives.ReverseEndianness(ResidentDataOffset);
ResidentDataSizeCompressed = BinaryPrimitives.ReverseEndianness(ResidentDataSizeCompressed);
ResidentDataSize = BinaryPrimitives.ReverseEndianness(ResidentDataSize);

GPUDataOffset = BinaryPrimitives.ReverseEndianness(GPUDataOffset);
GPUDataSizeCompressed = BinaryPrimitives.ReverseEndianness(GPUDataSizeCompressed);
GPUDataSize = BinaryPrimitives.ReverseEndianness(GPUDataSize);

ResMapLength = BinaryPrimitives.ReverseEndianness(ResMapLength);
ResMapOffset = BinaryPrimitives.ReverseEndianness(ResMapOffset);
}

}

}