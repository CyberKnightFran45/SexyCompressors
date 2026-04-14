using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Manifest for GroupInfo </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public struct ManifestGroupInfo
{
/// <summary> Art resolution </summary>

[FieldOffset(0)]
public uint ArtResolution;

/// <summary> Offset to GroupID </summary>

[FieldOffset(4)]
public uint IdOffset;

/// <summary> Number of Resources embedded </summary>

[FieldOffset(8)]
public uint ResCount;

// ctor

public ManifestGroupInfo(uint artRes, uint offset, uint resCount)
{
ArtResolution = artRes;

IdOffset = offset;
ResCount = resCount;
}

// Read ManifestInfo

public static ManifestGroupInfo Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<ManifestGroupInfo>(rawData);

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
ArtResolution = BinaryPrimitives.ReverseEndianness(ArtResolution);

IdOffset = BinaryPrimitives.ReverseEndianness(IdOffset);
ResCount = BinaryPrimitives.ReverseEndianness(ResCount);
}

}

}