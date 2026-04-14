using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/** <summary> Descriptor for a RSB Child </summary>

<remarks> Used in V3-V4 of the Algorithm. </remarks> **/

[StructLayout(LayoutKind.Explicit, Size = 16)]

public struct RsbChildDescriptorV3
{
/// <summary> ResGroup index </summary>

[FieldOffset(0)]
public uint GroupIndex;

/// <summary> Art resolution </summary>

[FieldOffset(4)]
public uint ArtResolution;

/// <summary> Language localization </summary>

[FieldOffset(8)]
public uint Localization;

/// <summary> Unknown field </summary>

[FieldOffset(12)]
private readonly uint Reserved;

// ctor

public RsbChildDescriptorV3(uint index, uint artRes, uint locale)
{
GroupIndex = index;

ArtResolution = artRes;
Localization = locale;
}

// Read ChildDescriptor

public static RsbChildDescriptorV3 Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[16];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbChildDescriptorV3>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ChildDescriptor

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[16];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

public void SwapEndian()
{
GroupIndex = BinaryPrimitives.ReverseEndianness(GroupIndex);

ArtResolution = BinaryPrimitives.ReverseEndianness(ArtResolution);
Localization = BinaryPrimitives.ReverseEndianness(Localization);
}

}

}