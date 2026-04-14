using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Stores info about a GPU Texture inside a ResGroup. </summary>

[StructLayout(LayoutKind.Explicit, Size = 28)]

public unsafe struct RsgTextureInfo
{
/// <summary> Offset to Texture file </summary>

[FieldOffset(0)]
public uint Offset;

/// <summary> Size in bytes </summary>

[FieldOffset(4)]
public uint Size;

/// <summary> Texture ID </summary>

[FieldOffset(8)]
public uint TextureID;

/// <summary> Some padding </summary>

[FieldOffset(12)]
private fixed int Padding[2];

/// <summary> Texture Width </summary>

[FieldOffset(20)]
public uint Width;

/// <summary> Texture Height </summary>

[FieldOffset(24)]
public uint Height;

// ctor

public RsgTextureInfo(uint offset, uint size, uint id, uint width, uint height)
{
Offset = offset;

Size = size;
TextureID = id;

Width = width;
Height = height;
}

// Read GPUInfo

public static RsgTextureInfo Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[28];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsgTextureInfo>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write GPUInfo

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
Offset = BinaryPrimitives.ReverseEndianness(Offset);

Size = BinaryPrimitives.ReverseEndianness(Size);
TextureID = BinaryPrimitives.ReverseEndianness(TextureID);

Width = BinaryPrimitives.ReverseEndianness(Width);
Height = BinaryPrimitives.ReverseEndianness(Height);
}

}

}