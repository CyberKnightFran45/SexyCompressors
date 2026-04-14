using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Manifest for GroupInfo (used in V3-V4) </summary>

[StructLayout(LayoutKind.Explicit, Size = 16)]

public struct ManifestGroupInfoV3
{
/// <summary> Art resolution </summary>

[FieldOffset(0)]
public uint ArtResolution;

/// <summary> Language localization </summary>

[FieldOffset(4)]
public uint Localization;

/// <summary> Offset to GroupID </summary>

[FieldOffset(8)]
public uint IdOffset;

/// <summary> Number of Resources embedded </summary>

[FieldOffset(12)]
public uint ResCount;

// ctor

public ManifestGroupInfoV3(uint artRes, uint locale, uint offset, uint resCount)
{
ArtResolution = artRes;
Localization = locale;

IdOffset = offset;
ResCount = resCount;
}

// Read ManifestInfo

public static ManifestGroupInfoV3 Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[16];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestGroupInfoV3>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write ManifestInfo

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[16];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
ArtResolution = BinaryPrimitives.ReverseEndianness(ArtResolution);
Localization = BinaryPrimitives.ReverseEndianness(Localization);

IdOffset = BinaryPrimitives.ReverseEndianness(IdOffset);
ResCount = BinaryPrimitives.ReverseEndianness(ResCount);
}

}

}