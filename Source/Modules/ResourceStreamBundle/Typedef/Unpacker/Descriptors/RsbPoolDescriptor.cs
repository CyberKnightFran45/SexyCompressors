using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Descriptor for a ResPool inside a RSB  </summary>

[StructLayout(LayoutKind.Explicit, Size = 152)]

public unsafe struct RsbPoolDescriptor
{
/// <summary> Pool name </summary>

[FieldOffset(0)]
public fixed byte Name[128];

/// <summary> Amount of memory allocated for ResidentData (RSG Info + Raw data) </summary>

[FieldOffset(128)]
public uint ResidentDataMemorySize;

/// <summary> Amount of memory allocated for GPUData </summary>

[FieldOffset(132)]
public uint GPUDataMemorySize;

/// <summary> Number of instances </summary>

[FieldOffset(136)]
public uint NumInstances;

/// <summary> Pool flags </summary>

[FieldOffset(140)]
public uint Flags;

/// <summary> Amount of textures embedded (used in V1 only) </summary>

[FieldOffset(144)]
public uint TextureCount;

/// <summary> Offset to Texture descriptor (used in V1 only) </summary>

[FieldOffset(148)]
public uint PtxDescriptorOffset;

// ctor

public RsbPoolDescriptor(uint dataLen, uint gpuLen, uint instances,
                         uint flags, uint textures, uint ptxOffset)
{
ResidentDataMemorySize = dataLen;
GPUDataMemorySize = gpuLen;

NumInstances = instances;
Flags = flags;

TextureCount = textures;
PtxDescriptorOffset = ptxOffset;
}

// Read PoolDescriptor

public static RsbPoolDescriptor Read(Stream reader, Endianness endian)
{
Span<byte> rawData = stackalloc byte[152];
reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbPoolDescriptor>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write PoolDescriptor

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[152];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
ResidentDataMemorySize = BinaryPrimitives.ReverseEndianness(ResidentDataMemorySize);
GPUDataMemorySize = BinaryPrimitives.ReverseEndianness(GPUDataMemorySize);

NumInstances = BinaryPrimitives.ReverseEndianness(NumInstances);
Flags = BinaryPrimitives.ReverseEndianness(Flags);

TextureCount = BinaryPrimitives.ReverseEndianness(TextureCount);
PtxDescriptorOffset = BinaryPrimitives.ReverseEndianness(PtxDescriptorOffset);
}

}

}