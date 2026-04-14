namespace SexyCompressors.MarmaladeDZip
{
/// <summary> Constants used in the DZip Format. </summary>

public static class DzConstants
{
/// <summary> DZ identifier. </summary>

public const ushort MAGIC = 0x445A;

/// <summary> The Version of a DZip File. </summary>

public const byte VERSION = 0;

/// <summary> Encoding used </summary>

public const EncodingType ENCODING = EncodingType.ANSI;
}

}