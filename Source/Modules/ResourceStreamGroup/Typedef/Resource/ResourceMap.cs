using System.Collections.Generic;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Maps resources inside a RSG with their Name </summary>

public class ResourceMap
{
// Resident files

public Dictionary<string, RsgResidentInfo> ResidentFiles{ get; } = new();

// GPU files

public Dictionary<string, RsgTextureInfo> TextureFiles{ get; } = new();

// File count

public int FileCount => ResidentFiles.Count;

// Texture count

public int TextureCount => TextureFiles.Count;

// ctor

public ResourceMap()
{
}

// ctor 2

public ResourceMap(int fileCount, int textureCount)
{
ResidentFiles = new(fileCount);
TextureFiles = new(textureCount);
}

// Add resident info

public void AddResident(string path, RsgResidentInfo info) => ResidentFiles.Add(path, info);

// Add texture info

public void AddTexture(string path, RsgTextureInfo info) => TextureFiles.Add(path, info);

// Clear map

public void Clear()
{
ResidentFiles.Clear();
TextureFiles.Clear();
}

}

}