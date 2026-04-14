namespace SexyCompressors.XboxPackedResource
{
/// <summary> Constants used in the XPR Format. </summary>

public static class XprConstants
{
/// <summary> XPR identifier. </summary>

public const uint MAGIC = 0x58505232;

/// <summary> Aligment between files </summary>

public const int FILE_ALIGMENT = 16;

/// <summary> Encoding used </summary>

public static readonly EncodingType ENCODING = EncodingType.ISO_8859_1;
}

}