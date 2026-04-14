namespace SexyCompressors.ArcVPackage
{
/// <summary> Constants used in the ARC-V Format. </summary>

public static class ArcvConstants
{
/// <summary> ARCV identifier. </summary>

public const uint MAGIC = 0x41524356;

/// <summary> Aligment between files </summary>

public const int FILE_ALIGMENT = 4;

/// <summary> Padding byte used </summary>

public const byte PADDING_BYTE = 0xAC;
}

}