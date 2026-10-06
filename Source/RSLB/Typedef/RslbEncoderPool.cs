using System.Collections.Concurrent;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace SexyCompressors.RSLB
{
/// <summary> RSLB encoder pool </summary>

public class RslbEncoderPool
{
/// <summary> Lzma algortihm <c>(0 = fast - 1 = normal)</c> </summary>

private const int LZMA_ALGORITHM = 0;

/// <summary> Lzma dictionary size </summary>

private const int LZMA_DICT_SIZE = SizeT.ONE_KILOBYTE * 32;

/// <summary> Lzma fast bytes <c>(min: 5 - max: 273)</c> </summary>

private const int LZMA_FAST_BYTES = 5;

/// <summary> Lzma match finder <c>(supported: BT2, BT4)</c> </summary>

private const string LZMA_MATCH_FINDER = "BT2";

// Lzma property IDs

private static readonly CoderPropID[] PropIDs =
[
CoderPropID.Algorithm,
CoderPropID.DictionarySize,
CoderPropID.NumFastBytes,
CoderPropID.MatchFinder
];

// Lzma property values

private static readonly object[] PropValues =
[
LZMA_ALGORITHM,
LZMA_DICT_SIZE,
LZMA_FAST_BYTES,
LZMA_MATCH_FINDER
];

// Encoders

private readonly ConcurrentBag<Encoder> _encoders = new();

// Rent
    
public Encoder Rent()
{

if(_encoders.TryTake(out var encoder) )
return encoder;

return CreateEncoder();
}

// Return new

public void Return(Encoder encoder) => _encoders.Add(encoder);

// Create Encoder

private static Encoder CreateEncoder()
{
Encoder encoder = new();
encoder.SetCoderProperties(PropIDs, PropValues);

return encoder;
}

}

}