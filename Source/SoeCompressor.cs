using System;
using System.IO;
using System.IO.Compression;
using BlossomLib.Modules.Compression;

namespace SexyCompressors
{
/// <summary> Initializes Compression Tasks for SOE Files. </summary>

public static class SoeCompressor
{
/// <summary> The Identifier of a SOE File. </summary>

private const uint MAGIC = 0x00454F53;

/// <summary> Compression type (ZLib). </summary>

private const uint COMPRESSION_FLAGS = 0x00424C5A;

/// <summary> File version. </summary>

private const uint VERSION = 1;

// Write header

private static void WriteHeader(Stream writer, uint rawSize, uint sizeCompressed)
{
writer.WriteUInt32(MAGIC);
writer.WriteUInt32(COMPRESSION_FLAGS);

writer.WriteUInt32(rawSize);
writer.WriteUInt32(sizeCompressed);

writer.WriteUInt32(VERSION);
}

// Get SOE Stream

public static void Compress(Stream input, Stream output, CompressionLevel level,
                            ProgressCallback progress = null)
{
using ChunkedMemoryStream buffer = new();

ZLibCompressor.CompressStream(input, buffer, level, -1, progress);
buffer.Seek(0, SeekOrigin.Begin);

var rawSize = (uint)input.Length;
var sizeCompressed = (uint)buffer.Length;

WriteHeader(output, rawSize, sizeCompressed);

FileManager.Process(buffer, output, sizeCompressed);
}

// Compress internal

private static void CompressInternal(TraceContext ctx, string inputPath, string outputPath,
                                     CompressionLevel level, ProgressCallback progress)
{
PathHelper.AddExtension(ref outputPath, ".soe");

ctx.ShowRatio = true;

TraceFileSteps.Run(ctx,
                   inputPath,
				   outputPath,
                   "Compressing data...",
				   (i, o, _) => Compress(i, o, level, progress)
);

}

/** <summary> Compresses a SOE File by using ZLIB Compression. </summary>

<param name = "inputPath"> The Path where the File to be Compressed is Located. </param>
<param name = "outputPath"> The Location where the Compressed File will be Saved. </param>
<param name = "compressionLvl"> The Compression Level to be Used. </param> */

public static void CompressFile(string inputPath, string outputPath, CompressionLevel level,
                                ProgressCallback progress = null)
{

TraceExecutor.Run("SOE Compression", 
                  ctx => CompressInternal(ctx, inputPath, outputPath, level, progress),
                  ("InputPath", inputPath),
                  ("OutputPath", outputPath),
				  ("CompressionLevel", level)
);

}

// Get RSB Stream

public static void Decompress(Stream input, Stream output,
                              ProgressCallback progress = null)
{
uint flags = input.ReadUInt32();

if(flags != MAGIC)
throw new Exception($"Invalid identifier: 0x{flags:X8}, expected: 0x{MAGIC:X8}");

uint comprFlags = input.ReadUInt32();

if(comprFlags != COMPRESSION_FLAGS)
throw new NotSupportedException("File was Compressed with an Unsupported algorithm.");

long rawSize = input.ReadUInt32();
long sizeCompressed = input.ReadUInt32();

uint inputVer = input.ReadUInt32();

if(inputVer != VERSION)
TraceLogger.WriteWarn($"Unknown version: v{inputVer} - Expected: v{VERSION}");

output.SetLength(rawSize);

ZLibCompressor.DecompressStream(input, output, sizeCompressed, progress);
}

// Decompress internal

private static void DecompressInternal(TraceContext ctx, string inputPath, string outputPath,
                                       ProgressCallback progress)
{
PathHelper.RemoveExtension(ref outputPath);

TraceFileSteps.Run(ctx,
                   inputPath,
				   outputPath,
                   "Decompressing data...",
				   (i, o, _) => Decompress(i, o, progress)
);

}

/** <summary> Decompresses a SOE File by using ZLIB Compression. </summary>

<param name = "inputPath" > The Path where the File to be Decompressed is Located. </param>
<param name = "outputPath" > The Location where the Decompressed File will be Saved. </param> */

public static void DecompressFile(string inputPath, string outputPath,
                                  ProgressCallback progress = null)
{

TraceExecutor.Run("SOE Decompression", 
                  ctx => DecompressInternal(ctx, inputPath, outputPath, progress),
                  ("InputPath", inputPath),
                  ("OutputPath", outputPath)
);

}

}

}