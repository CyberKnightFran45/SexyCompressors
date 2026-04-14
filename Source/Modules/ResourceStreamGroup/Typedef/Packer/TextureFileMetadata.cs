using System.Text.Json.Serialization;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Metadata for a Texture inside a RSG </summary>

public class TextureFileMetadata
{
/// <summary> Gets or Sets the Texture ID </summary>

public uint TextureID{ get; set; }

/// <summary> Gets or Sets the Texture Width </summary>

public uint Width{ get; set; }

/// <summary> Gets or Sets the Texture Height </summary>

public uint Height{ get; set; }

// ctor

public TextureFileMetadata()
{
}

// ctor 2

public TextureFileMetadata(uint id, uint width, uint height)
{
TextureID = id;

Width = width;
Height = height;
}

public static readonly JsonSerializerContext Context = new RsgTextureContext(JsonSerializer.Options);
}

// Context for serialization

[JsonSerializable(typeof(TextureFileMetadata) ) ]

public partial class RsgTextureContext : JsonSerializerContext
{
}

}