using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ArcVPackage
{
/// <summary> Stores info related to a ArcV binary. </summary>

[StructLayout(LayoutKind.Explicit, Size = 8)]

public readonly struct ArcvInfo
{
/// <summary> Amount of files embedded </summary>

[FieldOffset(0)]
public readonly uint FileCount;

/// <summary> Total file size </summary>

[FieldOffset(4)]
public readonly uint TotalSize;

// Read ArcvInfo

public static ArcvInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[8];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ArcvInfo>(rawData);
}

}

}