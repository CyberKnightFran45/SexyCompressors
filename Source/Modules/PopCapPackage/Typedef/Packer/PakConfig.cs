using System.IO.Compression;
using System.Text.Json.Serialization;

namespace SexyCompressors.PopCapPackage
{
/// <summary> Params used for Building a PAK File. </summary>

public class PakConfig
{
/// <summary> Gets or Sets the PAK Platform </summary>

public PakPlatform Platform{ get; set; }

/// <summary> Wheter to use ZLib or not </summary>

public bool UseZlib{ get; set; }

/// <summary> Compression Level for ZLib </summary>

public CompressionLevel? CompressionLvl{ get; set; }

// ctor

public PakConfig()
{ 
}

// ctor2

public PakConfig(PakPlatform platform, bool zlib)
{ 
Platform = platform;
UseZlib = zlib;

CompressionLvl = zlib ? CompressionLevel.Optimal : null;
}

public static readonly JsonSerializerContext Context = new PakContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(PakConfig) ) ]

public partial class PakContext : JsonSerializerContext
{
}

}