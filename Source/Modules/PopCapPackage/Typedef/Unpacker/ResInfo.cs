using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.PopCapPackage
{
/// <summary> Info for a Resource inside a PopCap Package. </summary>

[StructLayout(LayoutKind.Explicit, Size = 12)]

public readonly struct ResInfo
{
/// <summary> File size. </summary>

[FieldOffset(0)]
public readonly uint Size;

/// <summary> Time UTC that indicates when the file was Created. </summary>

[FieldOffset(4)]
public readonly long CreationTime;

// Read ResInfo

public static ResInfo Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[12];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ResInfo>(rawData);
}

}

}