using System.Runtime.InteropServices;

namespace SexyCompressors.RSLB
{
/// <summary> RSLB Block entry </summary>

[StructLayout(LayoutKind.Explicit, Size = 36)]

public readonly struct RslbBlockEntry
{
 /// <summary> Cumulative uncompressed offset up to this block (used for integrity checks) </summary>

[FieldOffset(0)]
public readonly ulong Cumulative;

/// <summary> Block size before compression </summary>

[FieldOffset(8)]
public readonly int RawSize;

/// <summary> Unknown field </summary>

[FieldOffset(12)]
private readonly uint Reserved;
 
/// <summary> Block offset inside stream </summary>

[FieldOffset(16)]
public readonly long DataOffset;

/// <summary> Block size after compression </summary>

[FieldOffset(24)]
public readonly int SizeCompressed;

/// <summary> Unknown field </summary>

[FieldOffset(28)]
private readonly ulong Reserved2;
}

}