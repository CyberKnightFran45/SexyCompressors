using System;
using System.Buffers.Binary;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace SexyCompressors.PopCapPackage
{
/// <summary> Info for a Resource that was Compressed with ZLib. </summary>

[StructLayout(LayoutKind.Explicit, Size = 16)]

public readonly struct ResInfoZlib
{
/// <summary> File size (before Compression). </summary>

[FieldOffset(0)]
public readonly uint RawSize;

/// <summary> File size (after Compression). </summary>

[FieldOffset(4)]
public readonly uint SizeCompressed;

/// <summary> Time UTC that indicates when the file was Created. </summary>

[FieldOffset(8)]
public readonly long CreationTime;

// Read ResInfo

public static ResInfoZlib Read(Stream reader)
{
Span<byte> rawData = stackalloc byte[16];
reader.ReadExactly(rawData);

return MemoryMarshal.Read<ResInfoZlib>(rawData);
}

}

}