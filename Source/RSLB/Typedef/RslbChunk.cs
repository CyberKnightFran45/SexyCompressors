using System.IO;

namespace SexyCompressors.RSLB
{
/// <summary> Wrapper to a compressed RSLB chunk </summary>

public sealed class RslbChunk
{
/// <summary> Block size before compression </summary>

public int RawSize;

/// <summary> Stream containing data </summary>

public MemoryStream Source;

// ctor

public RslbChunk(int rawSize, MemoryStream source)
{
RawSize = rawSize;
Source = source;
}

}

}