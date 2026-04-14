using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.ArcVPackage
{
/// <summary> Info for a Resource inside a ArcV binary. </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public readonly struct ResInfo
{
/// <summary> File offset. </summary>

[FieldOffset(0)]
public readonly uint Offset;

/// <summary> File size. </summary>

[FieldOffset(4)]
public readonly uint Size;

/// <summary> File ID (obtained as a CRC-32 checksum). </summary>

[FieldOffset(8)]
public readonly uint ID;

// Read ResInfo

public static ResInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ResInfo>(rawData);
}

}

}