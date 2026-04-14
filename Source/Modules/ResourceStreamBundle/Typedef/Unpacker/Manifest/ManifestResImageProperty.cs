using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Property for a Image inside ManifestRes </summary>

[StructLayout(LayoutKind.Explicit, Size = 24)]

public struct ManifestResImageProperty
{
/// <summary> Image type (1 for Atlas, 0 for regular) </summary>

[FieldOffset(0)]
public ushort Type;

/// <summary> Atlas flags </summary>

[FieldOffset(2)]
public ushort AtlasFlags;

/// <summary> Offset X </summary>

[FieldOffset(4)]
public ushort X;

/// <summary> Offset Y </summary>

[FieldOffset(6)]
public ushort Y;

/// <summary> Offset X for Atlas </summary>

[FieldOffset(8)]
public ushort AtlasX;

/// <summary> Offset Y for Atlas </summary>

[FieldOffset(10)]
public ushort AtlasY;

/// <summary> Atlas Width </summary>

[FieldOffset(12)]
public ushort AtlasWidth;

/// <summary> Atlas Height </summary>

[FieldOffset(14)]
public ushort AtlasHeight;

/// <summary> Number of Rows </summary>

[FieldOffset(16)]
public ushort Rows;

/// <summary> Number of Columns </summary>

[FieldOffset(18)]
public ushort Cols;

/// <summary> Offset to Parent image </summary>

[FieldOffset(20)]
public uint ParentOffset;

// Read ImgProperty

public static ManifestResImageProperty Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[24];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestResImageProperty>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ImgProperty

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[24];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
Type = BinaryPrimitives.ReverseEndianness(Type);
AtlasFlags = BinaryPrimitives.ReverseEndianness(AtlasFlags);

X = BinaryPrimitives.ReverseEndianness(X);
Y = BinaryPrimitives.ReverseEndianness(Y);

AtlasX = BinaryPrimitives.ReverseEndianness(AtlasX);
AtlasY = BinaryPrimitives.ReverseEndianness(AtlasY);

AtlasWidth = BinaryPrimitives.ReverseEndianness(AtlasWidth);
AtlasHeight = BinaryPrimitives.ReverseEndianness(AtlasHeight);

Rows = BinaryPrimitives.ReverseEndianness(Rows);
Cols = BinaryPrimitives.ReverseEndianness(Cols);

ParentOffset = BinaryPrimitives.ReverseEndianness(ParentOffset);
}

}

}