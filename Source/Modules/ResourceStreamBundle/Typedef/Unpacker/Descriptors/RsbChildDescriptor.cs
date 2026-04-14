using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Descriptor for a RSB Child </summary>

[StructLayout(LayoutKind.Explicit, Size = 8)]

public struct RsbChildDescriptor
{
/// <summary> ResGroup index </summary>

[FieldOffset(0)]
public uint GroupIndex;

/// <summary> Art resolution </summary>

[FieldOffset(4)]
public uint ArtResolution;

// ctor

public RsbChildDescriptor(uint index, uint artRes)
{
GroupIndex = index;
ArtResolution = artRes;
}

// Reverse Endianness

public void SwapEndian()
{
GroupIndex = BinaryPrimitives.ReverseEndianness(GroupIndex);
ArtResolution = BinaryPrimitives.ReverseEndianness(ArtResolution);
}

}

}