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

// Compress data

private static void CompressData(Stream input, uint inputLen, ChunkedMemoryStream output,
                                 CompressionLevel level,
                                 Action<long, long> progressCallback)
{
string fileSize = SizeT.FormatSize(inputLen);

TraceLogger.WriteActionStart($"Compressing data... ({fileSize})");

ZLibCompressor.CompressStream(input, output, level, -1, progressCallback);
output.Seek(0, SeekOrigin.Begin);

TraceLogger.WriteActionEnd();
}

// Write header

private static void WriteHeader(Stream writer, uint rawSize, uint sizeCompressed)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(MAGIC);
writer.WriteUInt32(COMPRESSION_FLAGS);
writer.WriteUInt32(rawSize);
writer.WriteUInt32(sizeCompressed);
writer.WriteUInt32(VERSION);

TraceLogger.WriteActionEnd();
}

// Get SOE Stream

public static void Compress(Stream input, Stream output, CompressionLevel level,
                            Action<long, long> progressCallback = null)
{
var inputLen = (uint)input.Length;

TraceLogger.WriteLine("Step #1 - Compress Stream:");
TraceLogger.WriteLine();

using ChunkedMemoryStream buffer = new();
CompressData(input, inputLen, buffer, level, progressCallback);

TraceLogger.WriteLine("Step #2 - Write Metadata:");
TraceLogger.WriteLine();

var sizeCompressed = (uint)buffer.Length;
WriteHeader(output, inputLen, sizeCompressed);

FileManager.Process(buffer, output, sizeCompressed);
}

/** <summary> Compresses a SOE File by using ZLIB Compression. </summary>

<param name = "inputPath"> The Path where the File to be Compressed is Located. </param>
<param name = "outputPath"> The Location where the Compressed File will be Saved. </param>
<param name = "compressionLvl"> The Compression Level to be Used. </param> */

public static void CompressFile(string inputPath, string outputPath, CompressionLevel level,
                                Action<long, long> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("SOE Compression Started");

try
{
PathHelper.AddExtension(ref outputPath, ".soe");
TraceLogger.WriteDebug($"{inputPath} --> {outputPath} (Level: {level})");

TraceLogger.WriteActionStart("Opening files...");

using var inFile = FileManager.OpenRead(inputPath);
using var outFile = FileManager.OpenWrite(outputPath);

TraceLogger.WriteActionEnd();

Compress(inFile, outFile, level, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Compress file");
}

TraceLogger.WriteLine("SOE Compression Finished");

var outSize = FileManager.GetFileSize(outputPath);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

// Get RSB Stream

public static void Decompress(Stream input, Stream output,
                              Action<long, long> progressCallback = null)
{
const string ERROR_INVALID_FLAGS = "Invalid identifier: {0:X8}, expected: {1:X8}";

TraceLogger.WriteLine("Step #1 - Read Metadata:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Reading header...");

uint flags = input.ReadUInt32();

if(flags != MAGIC)
{
TraceLogger.WriteError(string.Format(ERROR_INVALID_FLAGS, flags, MAGIC) );

return;
}

uint comprFlags = input.ReadUInt32();

if(comprFlags != COMPRESSION_FLAGS)
{
TraceLogger.WriteError($"File was Compressed with an Unsupported algorithm.");

return;
}

long rawSize = input.ReadUInt32();
long sizeCompressed = input.ReadUInt32();

uint inputVer = input.ReadUInt32();

if(inputVer != VERSION)
TraceLogger.WriteWarn($"Unknown version: v{inputVer} - Expected: v{VERSION}");

output.SetLength(rawSize);

TraceLogger.WriteActionEnd();

TraceLogger.WriteLine("Step #2 - Decompress Stream:");
TraceLogger.WriteLine();

string fileSize = SizeT.FormatSize(sizeCompressed);

TraceLogger.WriteActionStart($"Decompressing data... ({fileSize})");
ZLibCompressor.DecompressStream(input, output, sizeCompressed, progressCallback);

TraceLogger.WriteActionEnd();
}

/** <summary> Decompresses a SOE File by using ZLIB Compression. </summary>

<param name = "inputPath" > The Path where the File to be Decompressed is Located. </param>
<param name = "outputPath" > The Location where the Decompressed File will be Saved. </param> */

public static void DecompressFile(string inputPath, string outputPath,
                                  Action<long, long> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("SOE Decompression Started");

try
{
PathHelper.RemoveExtension(ref outputPath);
TraceLogger.WriteDebug($"{inputPath} --> {outputPath}");

TraceLogger.WriteActionStart("Opening files...");

using var inFile = FileManager.OpenRead(inputPath);
using var outFile = FileManager.OpenWrite(outputPath);

TraceLogger.WriteActionEnd();

Decompress(inFile, outFile, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Decompress file");
}

TraceLogger.WriteLine("SOE Decompression Finished");

var outSize = FileManager.GetFileSize(outputPath);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}