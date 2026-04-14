using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Descriptor for a PTX Image inside a RSB </summary>

[StructLayout(LayoutKind.Explicit, Size = 24)]

public struct RsbTextureDescriptor
{
/// <summary> Texture Width </summary>

[FieldOffset(0)]
public uint Width;

/// <summary> Texture Height </summary>

[FieldOffset(4)]
public uint Height;

/// <summary> Texture Pitch </summary>

[FieldOffset(8)]
public uint Pitch;

/// <summary> PTX Format </summary>

[FieldOffset(12)]
public uint Format;

/// <summary> Amount of bytes used in Alpha Channel  </summary>

[FieldOffset(16)]
public uint AlphaSize;

/// <summary> Image scale  </summary>

[FieldOffset(20)]
public uint Scale;

// ctor

public RsbTextureDescriptor(uint width, uint height, uint pitch, uint format, uint aSize, uint scale)
{
Width = width;
Height = height;

Pitch = pitch;
Format = format;

AlphaSize = aSize;
Scale = scale;
}

// Read PtxDescriptor

public static RsbTextureDescriptor Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[24];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbTextureDescriptor>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write PtxDescriptor

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
Width = BinaryPrimitives.ReverseEndianness(Width);
Height = BinaryPrimitives.ReverseEndianness(Height);

Pitch = BinaryPrimitives.ReverseEndianness(Pitch);
Format = BinaryPrimitives.ReverseEndianness(Format);

AlphaSize = BinaryPrimitives.ReverseEndianness(AlphaSize);
Scale = BinaryPrimitives.ReverseEndianness(Scale);
}

}

}