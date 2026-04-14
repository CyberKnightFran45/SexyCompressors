using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Property for a Universal file inside ManifestRes </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public struct ManifestResUniversalProperty
{
/// <summary> Key offset </summary>

[FieldOffset(0)]
public uint KeyOffset;

/// <summary> Unknown field </summary>

[FieldOffset(4)]
private uint Reserved;

/// <summary> Value offset </summary>

[FieldOffset(8)]
public uint ValueOffset;

// ctor

public ManifestResUniversalProperty(uint keyOffset, uint valueOffset)
{
KeyOffset = keyOffset;
ValueOffset = valueOffset;
}

// Read UniversalProperty

public static ManifestResUniversalProperty Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestResUniversalProperty>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write UniversalProperty

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
KeyOffset = BinaryPrimitives.ReverseEndianness(KeyOffset);
ValueOffset = BinaryPrimitives.ReverseEndianness(ValueOffset);
}

}

}