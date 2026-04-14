using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Header for ManifestRes </summary>

[StructLayout(LayoutKind.Explicit, Size = 28)]

public struct ManifestResHeader
{
/// <summary> Unknown field </summary>

[FieldOffset(0)]
private readonly uint Reserved;

/// <summary> Resource type </summary>

[FieldOffset(4)]
public ushort Type;

/// <summary> Header size </summary>

[FieldOffset(6)]
public ushort HeaderSize;

/// <summary> Offset to Universal Property </summary>

[FieldOffset(8)]
public uint UniversalPropertyOffset;

/// <summary> Offset to Image Property </summary>

[FieldOffset(12)]
public uint ImagePropertyOffset;

/// <summary> Offset to ResID </summary>

[FieldOffset(16)]
public uint IdOffset;

/// <summary> Offset to file Path </summary>

[FieldOffset(20)]
public uint PathOffset;

/// <summary> Number of Universal properties </summary>

[FieldOffset(24)]
public uint UniversalPropertyCount;

// Read ManifestRes

public static ManifestResHeader Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[28];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestResHeader>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ManifestRes

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[28];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
Type = BinaryPrimitives.ReverseEndianness(Type);

HeaderSize = BinaryPrimitives.ReverseEndianness(HeaderSize);
UniversalPropertyOffset = BinaryPrimitives.ReverseEndianness(UniversalPropertyOffset);

ImagePropertyOffset = BinaryPrimitives.ReverseEndianness(ImagePropertyOffset);
IdOffset = BinaryPrimitives.ReverseEndianness(IdOffset);

PathOffset = BinaryPrimitives.ReverseEndianness(PathOffset);
UniversalPropertyCount = BinaryPrimitives.ReverseEndianness(UniversalPropertyCount);
}

}

}