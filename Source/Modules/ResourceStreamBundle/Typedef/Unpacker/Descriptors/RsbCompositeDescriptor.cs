using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ResourceStreamBundle
{
/// <summary> Descriptor for a Composite RSB Group </summary>

[StructLayout(LayoutKind.Explicit, Size = 644)]

public unsafe struct RsbCompositeDescriptor
{
/// <summary> Composite name </summary>

[FieldOffset(0)]
public fixed byte Name[128];

/// <summary> Childs for this Group (max 64 slots) </summary>

[FieldOffset(128)]
public fixed byte Childs[512];

/// <summary> Number of SubGroups used </summary>

[FieldOffset(640)]
public uint ChildCount;

// ctor

public RsbCompositeDescriptor(uint childCount)
{
ChildCount = Math.Min(childCount, 64);
}

// Read CompositeDescriptor

public static RsbCompositeDescriptor Read(Stream reader, Endianness endian)
{
using NativeMemoryOwner<byte> rOwner = new(644);
var rawData = rOwner.AsSpan();

reader.ReadExactly(rawData);

var info = MemoryMarshal.Read<RsbCompositeDescriptor>(rawData);

if(endian == Endianness.BigEndian)
info.SwapEndian();

return info;
}

// Write CompositeDescriptor

public void Write(Stream writer, Endianness endian)
{
using NativeMemoryOwner<byte> rOwner = new(644);
var rawData = rOwner.AsSpan();

if(endian == Endianness.BigEndian)
SwapEndian();

MemoryMarshal.Write(rawData, this);

writer.Write(rawData);
}

// Reverse Endianness

private void SwapEndian()
{
uint childCount = BinaryPrimitives.ReverseEndianness(ChildCount);
childCount = Math.Min(childCount, 64);

ChildCount = childCount;

fixed(byte* pChildren = Childs)
{
var childPtr = (RsbChildDescriptor*)pChildren;

for(uint i = 0; i < childCount; i++)
(childPtr + i)->SwapEndian();

}

}

}

}