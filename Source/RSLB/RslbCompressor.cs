using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SevenZip.Compression.LZMA;

namespace SexyCompressors.RSLB
{
/// <summary> Supports ResBundle Lzma Streams (RSLB) from PvZ2 China <c>(used in v4.1.5 beta)</c> </summary>

public static class RslbCompressor
{
/// <summary> File identifier </summary>

private const uint MAGIC = 0x424C5352;

/// <summary> File version </summary>

private const uint VERSION = 1;

/// <summary> Start offset to entries table </summary>

private const long TABLE_START = 0x20;

/// <summary> Target uncompressed size per block </summary>

private const int DEFAULT_BLOCK_SIZE = SizeT.ONE_MEGABYTE * 32;

// Encoder pool

private static readonly RslbEncoderPool EncoderPool = new();

// Compute blocks count

private static int ComputeBlocksCount(long rawSize)
{
var maxBlocks = (rawSize + DEFAULT_BLOCK_SIZE - 1) / DEFAULT_BLOCK_SIZE;

return (int)Math.Max(1, maxBlocks);
}

// Get raw chunk size

private static int GetRawChunkSize(long rawSize, int blockIndex)
{
var maxChunkSize = rawSize - (long)blockIndex * DEFAULT_BLOCK_SIZE;

return (int)Math.Min(DEFAULT_BLOCK_SIZE, maxChunkSize);
}

// Dump bytes into MemoryStream

private static MemoryStream DumpBytes(ReadOnlySpan<byte> bytes)
{
MemoryStream stream = new(bytes.Length); 

stream.Write(bytes);
stream.Seek(0, SeekOrigin.Begin);

return stream;
}

// Log block info for debugging

private static void LogBlockInfo(int blockIndex, int numBlocks, long rawSize, long sizeCompressed,
                                 Stopwatch timer)
{
const string FMT = "Block: {0,3}/{1,-3} | Raw: {2,10} | Compressed: {3,10} | Elapsed: {4,8} |";

string rawSizeFmt = SizeT.FormatSize(rawSize);
string sizeCompFmt = SizeT.FormatSize(sizeCompressed);

string elapsed = timer.GetExactTime();

var info = string.Format(FMT, blockIndex + 1, numBlocks, rawSizeFmt, sizeCompFmt, elapsed);

TraceLogger.WriteLine(info);
}

// Print sub head

private static void PrintSubHead(string msg)
{
TraceLogger.WriteLine();

TraceLogger.WriteLine($"• {msg}:");
TraceLogger.WriteLine();
}

// Write header

private static void WriteHeader(Stream writer, ulong rawSize, int numBlocks)
{
writer.WriteUInt32(MAGIC);
writer.WriteUInt32(VERSION);

writer.WriteUInt64(rawSize);

writer.WriteUInt64(0);
writer.WriteUInt32(0);

writer.WriteInt32(numBlocks);
}

// Compress LZMA

private static MemoryStream CompressLzma(ReadOnlySpan<byte> rawBytes, Encoder encoder)
{
using var inStream = DumpBytes(rawBytes);

MemoryStream outStream = new();

encoder.WriteCoderProperties(outStream);
encoder.Code(inStream, outStream, rawBytes.Length, -1, null);

outStream.Seek(0, SeekOrigin.Begin);

return outStream;
}

// Get LZMA stream

private static MemoryStream GetLzmaStream(ReadOnlySpan<byte> rawBlock, Encoder encoder,
                                          int blockIndex, int numBlocks)
{
Stopwatch timer = Stopwatch.StartNew();
var compressed = CompressLzma(rawBlock, encoder);

timer.Stop();

LogBlockInfo(blockIndex, numBlocks, rawBlock.Length, compressed.Length, timer);

return compressed;
}

// Compress single block

private static void CompressBlock(NativeBuffer buffer, int blockIndex, int numBlocks,
                                  long rawSize, RslbChunk[] lzmaBlocks,
								  ProgressCallback progress, ref int done)
{
var encoder = EncoderPool.Rent();

try
{
int chunkSize = GetRawChunkSize(rawSize, blockIndex);
int chunkOffset = blockIndex * DEFAULT_BLOCK_SIZE;

var chunk = buffer.GetView(chunkOffset, chunkSize);
var compressed = GetLzmaStream(chunk, encoder, blockIndex, numBlocks);

lzmaBlocks[blockIndex] = new(chunkSize, compressed);
}

finally
{
EncoderPool.Return(encoder);
}

int current = Interlocked.Increment(ref done);

progress?.Invoke(current, numBlocks);
}

// Compress blocks

private static RslbChunk[] CompressBlocks(NativeBuffer rawBlob, int numBlocks, long rawSize,
                                          ProgressCallback progress)
{
PrintSubHead("Lzma blocks compression");

var lzmaBlocks = new RslbChunk[numBlocks];

int done = 0;

Parallel.For(0, numBlocks, i =>
{
CompressBlock(rawBlob, i, numBlocks, rawSize, lzmaBlocks, progress, ref done);
}

);

TraceLogger.WriteLine();

return lzmaBlocks;
}

// Write chunk entry

private static void WriteEntry(Stream writer, ulong cumulative, uint rawSize,
                               long dataOffset, int sizeCompressed)
{
writer.WriteUInt64(cumulative);
writer.WriteUInt32(rawSize);

writer.WriteUInt32(0);

writer.WriteInt64(dataOffset);
writer.WriteInt32(sizeCompressed);

writer.WriteUInt64(0);
}

// Write entries

private static void WriteEntries(Stream writer, int numBlocks, RslbChunk[] chunks)
{
long dataStart = TABLE_START + numBlocks * 36;

ulong cumulative = 0;
long dataOffset = dataStart;

for(int i = 0; i < numBlocks; i++)
{
var rawChunkSize = (uint)chunks[i].RawSize;
var chunkSizeComp = (int)chunks[i].Source.Length;

WriteEntry(writer, cumulative, rawChunkSize, dataOffset, chunkSizeComp);

cumulative += rawChunkSize;
dataOffset += chunkSizeComp;
}

}

// Write compressed blocks

private static void WriteLzmaBlocks(RslbChunk[] blocks, Stream output)
{

foreach(var b in blocks)
{
var s = b.Source;
FileManager.Process(s, output);

s.Dispose();
b.Source = null;
}

}

// Compress RSLB stream

public static void Compress(Stream input, Stream output,
                            ProgressCallback progress = null)
{
long rawSize = input.Length - input.Position;
int numBlocks = ComputeBlocksCount(rawSize);

WriteHeader(output, (ulong)rawSize, numBlocks);

using var inBuffer = input.ReadPtr();
var lzmaBlocks = CompressBlocks(inBuffer, numBlocks, rawSize, progress);

WriteEntries(output, numBlocks, lzmaBlocks);
WriteLzmaBlocks(lzmaBlocks, output);
}

// Compress internal

private static void CompressInternal(TraceContext ctx, string inputPath, string outputPath,
                                     ProgressCallback progress)
{
PathHelper.AddExtension(ref outputPath, ".smf");

ctx.ShowRatio = true;

TraceFileSteps.Run(ctx,
                   inputPath,
                   outputPath,
                   "Compressing data...",
                   (i, o, _) => Compress(i, o, progress)
);

}

/// <summary> Compresses the contents of a RSB file as RSLB file </summary>

public static void CompressFile(string inputPath, string outputPath,
                                ProgressCallback progress = null)
{

TraceExecutor.Run("RSLB Compression",
                  ctx => CompressInternal(ctx, inputPath, outputPath, progress),
                  ("InputPath",  inputPath),
                  ("OutputPath", outputPath)
);

}

// Read header

private static int ReadHeader(Stream reader)
{
uint flags = reader.ReadUInt32();

if(flags != MAGIC)
throw new Exception($"Invalid identifier: 0x{flags:X8}, expected: 0x{MAGIC:X8}");

uint inputVer = reader.ReadUInt32();

if(inputVer != VERSION)
TraceLogger.WriteWarn($"Unknown version: v{inputVer} - Expected: v{VERSION}");

_ = reader.ReadUInt64(); // Raw RSB size

_ = reader.ReadUInt64(); // Reserved
_ = reader.ReadUInt32(); // Reserved2

int numBlocks = reader.ReadInt32();

return numBlocks;
}

// Read entries

private static RslbBlockEntry[] ReadEntries(Stream reader, int tableSize)
{
using var eOwner = reader.ReadPtr(tableSize);

var tableBytes = eOwner.GetView();
var entries = MemoryMarshal.Cast<byte, RslbBlockEntry>(tableBytes);

return entries.ToArray();
}

// Create Lzma Decoder

private static Decoder GetDecoder(MemoryStream reader)
{
Decoder decoder = new();

byte[] props = new byte[5];
reader.ReadExactly(props);

decoder.SetDecoderProperties(props);

return decoder;
}

// Decompress LZMA

private static MemoryStream DecompressLzma(ReadOnlySpan<byte> lzmaBytes, int rawSize)
{
using var inStream = DumpBytes(lzmaBytes);
var decoder = GetDecoder(inStream);

MemoryStream output = new();
decoder.Code(inStream, output, lzmaBytes.Length - 5, rawSize, null);

output.Seek(0, SeekOrigin.Begin);

return output;
}

// Decompress single block

private static MemoryStream DecompressBlock(NativeBuffer buffer, in RslbBlockEntry entry, long dataStart,
                                            int blockIndex, int numBlocks, 
                                            ProgressCallback progress, ref int done)
{
var relativeOffset = (int)(entry.DataOffset - dataStart);

int rawChunkSize = entry.RawSize;
int chunkSizeComp = entry.SizeCompressed;

var compressed = buffer.GetView(relativeOffset, chunkSizeComp);

Stopwatch timer = Stopwatch.StartNew();
var raw = DecompressLzma(compressed, rawChunkSize);

timer.Stop();

LogBlockInfo(blockIndex, numBlocks, rawChunkSize, chunkSizeComp, timer);

int current = Interlocked.Increment(ref done);

progress?.Invoke(current, numBlocks);

return raw;
}

// Decompress blocks (in Parallel)

private static MemoryStream[] DecompressBlocks(NativeBuffer buffer, RslbBlockEntry[] entries,
                                               int numBlocks, int tableSize,
                                               ProgressCallback progress)
{
PrintSubHead("Lzma blocks decompression");

var rawBlocks = new MemoryStream[numBlocks];

long dataStart = TABLE_START + tableSize;

int done = 0;

Parallel.For(0, numBlocks, i =>
{
var entry = entries[i];

rawBlocks[i] = DecompressBlock(buffer, entry, dataStart, i, numBlocks, progress, ref done);
}

);

TraceLogger.WriteLine();

return rawBlocks;
}

// Write raw blocks

private static void WriteRawBlocks(Stream output, MemoryStream[] blockStreams)
{

for(int i = 0; i < blockStreams.Length; i++)
{
var s = blockStreams[i];

FileManager.Process(s, output);

s.Dispose();
blockStreams[i] = null;
}

}

// Decompress RSLB stream

public static void Decompress(Stream input, Stream output,
                              ProgressCallback progress = null)
{
int numBlocks = ReadHeader(input);

int tableSize = numBlocks * 36;
var entries = ReadEntries(input, tableSize);

using var chunksBlob = input.ReadPtr();

var rawBlocks = DecompressBlocks(chunksBlob, entries, numBlocks, tableSize, progress);

WriteRawBlocks(output, rawBlocks);
}

// Decompress internal

private static void DecompressInternal(TraceContext ctx, string inputPath, string outputPath,
                                       bool removeSmfExt, ProgressCallback progress)
{
if(removeSmfExt)
PathHelper.RemoveExtension(ref outputPath);

TraceFileSteps.Run(ctx,
                   inputPath,
                   outputPath,
                   "Decompressing data...",
                   (i, o, _) => Decompress(i, o, progress)
);

}

/// <summary> Decompresses the contents of a RSLB file as a RSB file. </summary>

public static void DecompressFile(string inputPath, string outputPath,
                                  bool removeSmfExt = true,
                                  ProgressCallback progress = null)
{
TraceExecutor.Run("RSLB Decompression",
                  ctx => DecompressInternal(ctx, inputPath, outputPath, removeSmfExt, progress),
                  ("InputPath",  inputPath),
                  ("OutputPath", outputPath),
                  ("RemoveSmfExtension", removeSmfExt)
);
}

}

}