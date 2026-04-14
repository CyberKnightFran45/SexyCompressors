using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Stores info related to a PopCap ResBundle. </summary>

[StructLayout(LayoutKind.Explicit, Size = 112)]

public unsafe struct RsbInfo
{
/// <summary> The Identifier for this Struct </summary>

[FieldOffset(0)]
public readonly uint Magic = 0x72736231;

/// <summary> Major Version </summary>

[FieldOffset(4)]
public RsbMajorVersion MajorVersion;

/// <summary> Minor Version </summary>

[FieldOffset(8)]
public RsbMinorVersion MinorVersion;

/// <summary> Header size </summary>

[FieldOffset(12)]
public uint HeaderSize;

/// <summary> Size of GlobalMap, a section where Files are indexed (-1 if not present). </summary>

[FieldOffset(16)]
public int GlobalMapSize;

/// <summary> Offset to GlobalMap </summary>

[FieldOffset(20)]
public uint GlobalMapOffset;

/// <summary> Some padding </summary>

[FieldOffset(24)]
private fixed uint Padding[3];

/// <summary> Size of GroupMap, a section where Groups are indexed. </summary>

[FieldOffset(32)]
public uint GroupMapSize;

/// <summary> Offset to GroupMap </summary>

[FieldOffset(36)]
public uint GroupMapOffset;

/// <summary> Amount of Groups embedded </summary>

[FieldOffset(40)]
public uint GroupCount;

/// <summary> Offset to GroupDescriptor </summary>

[FieldOffset(44)]
public uint GroupDescriptorOffset;

/// <summary> GroupDescriptor Size </summary>

[FieldOffset(48)]
public uint GroupDescriptorSize;

/// <summary> Amount of Composite Groups embedded </summary>

[FieldOffset(52)]
public uint CompositeCount;

/// <summary> Offset to CompositeDescriptor </summary>

[FieldOffset(56)]
public uint CompositeDescriptorOffset;

/// <summary> CompositeDescriptor Size </summary>

[FieldOffset(60)]
public uint CompositeDescriptorSize;

/// <summary> Size of CompositeMap, a section where Composites are indexed. </summary>

[FieldOffset(64)]
public uint CompositeMapSize;

/// <summary> Offset to CompositeMap </summary>

[FieldOffset(68)]
public uint CompositeMapOffset;

/// <summary> Amount of Pools embedded </summary>

[FieldOffset(72)]
public uint PoolCount;

/// <summary> Offset to PoolDescriptor </summary>

[FieldOffset(76)]
public uint PoolDescriptorOffset;

/// <summary> PoolDescriptor Size </summary>

[FieldOffset(80)]
public uint PoolDescriptorSize;

/// <summary> Amount of Textures embedded </summary>

[FieldOffset(84)]
public uint TextureCount;

/// <summary> Offset to TextureDescriptor </summary>

[FieldOffset(88)]
public uint PtxDescriptorOffset;

/// <summary> TextureDescriptor Size </summary>

[FieldOffset(92)]
public uint PtxDescriptorSize;

/// <summary> Offset to Group Manifest </summary>

[FieldOffset(96)]
public uint ManifestGroupOffset;

/// <summary> Offset to Resource Manifest </summary>

[FieldOffset(100)]
public uint ManifestResOffset;

/// <summary> Offset to StringPool Manifest </summary>

[FieldOffset(104)]
public uint ManifestPoolOffset;

/// <summary> Header size excluding Manifest (used in V4) </summary>

[FieldOffset(108)]
public uint HeaderSizeNoManifest;

// ctor

public RsbInfo()
{
}

// Read RsbInfo

public static RsbInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[112];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<RsbInfo>(rawData);
}

// Write RsbInfo

public void Write(Stream writer, Endianness endian)
{
Span<byte> rawData = stackalloc byte[112];

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

public void SwapEndian()
{
MajorVersion = (RsbMajorVersion)BinaryPrimitives.ReverseEndianness( (uint)MajorVersion);
MinorVersion = (RsbMinorVersion)BinaryPrimitives.ReverseEndianness( (uint)MinorVersion);

HeaderSize = BinaryPrimitives.ReverseEndianness(HeaderSize);

GlobalMapSize = BinaryPrimitives.ReverseEndianness(GlobalMapSize);
GlobalMapOffset = BinaryPrimitives.ReverseEndianness(GlobalMapOffset);

GroupMapSize = BinaryPrimitives.ReverseEndianness(GroupMapSize);
GroupMapOffset = BinaryPrimitives.ReverseEndianness(GroupMapOffset);
GroupCount = BinaryPrimitives.ReverseEndianness(GroupCount);
GroupDescriptorOffset = BinaryPrimitives.ReverseEndianness(GroupDescriptorOffset);
GroupDescriptorSize = BinaryPrimitives.ReverseEndianness(GroupDescriptorSize);

CompositeCount = BinaryPrimitives.ReverseEndianness(CompositeCount);
CompositeDescriptorOffset = BinaryPrimitives.ReverseEndianness(CompositeDescriptorOffset);
CompositeDescriptorSize = BinaryPrimitives.ReverseEndianness(CompositeDescriptorSize);
CompositeMapSize = BinaryPrimitives.ReverseEndianness(CompositeMapSize);
CompositeMapOffset = BinaryPrimitives.ReverseEndianness(CompositeMapOffset);

PoolCount = BinaryPrimitives.ReverseEndianness(PoolCount);
PoolDescriptorOffset = BinaryPrimitives.ReverseEndianness(PoolDescriptorOffset);
PoolDescriptorSize = BinaryPrimitives.ReverseEndianness(PoolDescriptorSize);

TextureCount = BinaryPrimitives.ReverseEndianness(TextureCount);
PtxDescriptorOffset = BinaryPrimitives.ReverseEndianness(PtxDescriptorOffset);
PtxDescriptorSize = BinaryPrimitives.ReverseEndianness(PtxDescriptorSize);

ManifestGroupOffset = BinaryPrimitives.ReverseEndianness(ManifestGroupOffset);
ManifestResOffset = BinaryPrimitives.ReverseEndianness(ManifestResOffset);
ManifestPoolOffset = BinaryPrimitives.ReverseEndianness(ManifestPoolOffset);

HeaderSizeNoManifest = BinaryPrimitives.ReverseEndianness(HeaderSizeNoManifest);
}

}

}