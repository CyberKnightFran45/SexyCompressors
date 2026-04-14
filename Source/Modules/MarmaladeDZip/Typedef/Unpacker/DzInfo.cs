using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.MarmaladeDZip
{
/// <summary> Represents some info for a DZip file. </summary>

[StructLayout(LayoutKind.Explicit, Size = 5)]

public readonly struct DzInfo
{
/// <summary> Number of entries in NamesTable. </summary>

[FieldOffset(0)]
public readonly ushort FileCount;

/// <summary> Number of entries in DirsTable. </summary>

[FieldOffset(2)]
public readonly ushort DirCount;
  
/// <summary> File version. </summary>

[FieldOffset(4)]
public readonly byte Version;

// Read DzInfo

public static DzInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[5];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<DzInfo>(rawData);
}

}

}