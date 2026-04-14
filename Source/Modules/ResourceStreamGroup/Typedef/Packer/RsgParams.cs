using System.Text.Json.Serialization;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Defines some Params for Packing a RSG Stream </summary>

public class RsgParams
{
/// <summary> Gets or Sets the Endianess </summary>

public Endianness Endian{ get; set; }

/// <summary> Gets or Sets the Major Version </summary>

public RsgMajorVersion MajorVersion{ get; set; }

/// <summary> Gets or Sets the Minor Version </summary>

public RsgMinorVersion MinorVersion{ get; set; }

/// <summary> Gets or Sets the Compression Flags </summary>

public uint CompressionFlags{ get; set; }

// ctor

public RsgParams()
{
}

// ctor 2

public RsgParams(Endianness endian, RsgMajorVersion majVer, RsgMinorVersion minVer, uint flags)
{
Endian = endian;
MajorVersion = majVer;

MinorVersion = minVer;
CompressionFlags = flags;
}

public static readonly JsonSerializerContext Context = new RsgParamsContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(RsgParams) ) ]

public partial class RsgParamsContext : JsonSerializerContext
{
}

}