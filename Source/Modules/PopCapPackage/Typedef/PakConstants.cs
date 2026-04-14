namespace SexyCompressors.PopCapPackage
{
/// <summary> Constants used in the PAK Format. </summary>

public static class PakConstants
{
/** <summary> The Identifier of a PAK File. </summary>

<remarks> <c>0x0FF512ED</c> for XMEM files (not supported, use xbdecompress instead) </remarks> */

public const uint MAGIC = 0xBAC04AC0;

/// <summary> The Version of a PAK File. </summary>

public const uint VERSION = 0;

/// <summary> The Key used for Encrypting Data. </summary>

public const byte KEY = 0xF7;

/// <summary> Identifier that marks the End of Entries section </summary>

public const byte ENTRIES_END = 0x80;

/// <summary> The Encoding used </summary>

public const EncodingType ENCODING = EncodingType.ANSI;
}

}