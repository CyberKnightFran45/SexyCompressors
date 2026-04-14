using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using BlossomLib.Modules.Compression;

namespace SexyCompressors.PopCapZLib
{
/// <summary> Initializes Compression Tasks for SMF Files. </summary>

public static class SmfCompressor
{
/// <summary> The Identifier of a SMF File. </summary>

private const uint MAGIC = 0xDEADFED4;

// Get SMF Stream

public static void CompressStream(Stream input, Stream output, CompressionLevel level,
                                  Action<long, long> progressCallback = null)
{
long inputLen = input.Length;

SmfPlatform targetPlatform;
string platformFlags;

if(inputLen > uint.MaxValue)
{
targetPlatform = SmfPlatform.x64;
platformFlags = "64-bits";
}

else
{
targetPlatform = SmfPlatform.x32;
platformFlags = "32-bits";
}

TraceLogger.WriteActionStart($"Writing header... ({platformFlags} Variant)");

if(targetPlatform == SmfPlatform.x64)
{
output.WriteUInt64(MAGIC);
output.WriteInt64(inputLen);
}

else
{
output.WriteUInt32(MAGIC);
output.WriteUInt32( (uint)inputLen);
}

TraceLogger.WriteActionEnd();

string fileSize = SizeT.FormatSize(inputLen);

TraceLogger.WriteActionStart($"Compressing data... ({fileSize})");
ZLibCompressor.CompressStream(input, output, level, -1, progressCallback);

TraceLogger.WriteActionEnd();
}

/** <summary> Compresses the Contents of a RSB File as a SMF File. </summary>

<param name = "inputPath"> The Path where the File to be Compressed is Located. </param>
<param name = "outputPath"> The Location where the Compressed File will be Saved. </param>
<param name = "compressionLvl"> The Compression Level to be Used. </param> */

public static void CompressFile(string inputPath, string outputPath, CompressionLevel level,
                                Action<long, long> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("SMF Compression Started");

try
{
PathHelper.AddExtension(ref outputPath, ".smf");
TraceLogger.WriteDebug($"{inputPath} --> {outputPath} (Level: {level})");

TraceLogger.WriteActionStart("Opening files...");

using var inFile = FileManager.OpenRead(inputPath);
using var outFile = FileManager.OpenWrite(outputPath);

TraceLogger.WriteActionEnd();

CompressStream(inFile, outFile, level, progressCallback);
outFile.Seek(0, SeekOrigin.Begin);

TraceLogger.WriteActionStart("Saving Tag...");
SmfTagCreator.SaveTag(outFile, outputPath);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Compress file");
}

TraceLogger.WriteLine("SMF Compression Finished");

var outSize = FileManager.GetFileSize(outputPath);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

// Get RSB Stream

public static void DecompressStream(Stream input, Stream output,
                                    Action<long, long> progressCallback = null)
{
const string ERROR_INVALID_MAGIC = "Invalid magic: {0:X8}, expected: {1:X8}";

TraceLogger.WriteActionStart("Reading header...");

using var mOwner = input.ReadPtr(8);
var rawMagic = mOwner.AsSpan();

ulong inputMagic = BinaryPrimitives.ReadUInt64LittleEndian(rawMagic);
SmfPlatform targetPlatform;

if(inputMagic >> 32 == 0)
targetPlatform = SmfPlatform.x64;

else
{
inputMagic = BinaryPrimitives.ReadUInt32LittleEndian(rawMagic[.. 4] );

targetPlatform = SmfPlatform.x32;
}

if(inputMagic != MAGIC)
{
var msg = string.Format(ERROR_INVALID_MAGIC, inputMagic, MAGIC);
TraceLogger.WriteError(msg);

return;
}

long sizeBeforeCompr;
int headerLen;

string platformFlags;

if(targetPlatform == SmfPlatform.x64)
{
sizeBeforeCompr = input.ReadInt64();
headerLen = 16;

platformFlags = "64-bits";
}

else
{
sizeBeforeCompr = BinaryPrimitives.ReadUInt32LittleEndian(rawMagic.Slice(4, 4) );
headerLen = 8;

platformFlags = "32-bits";
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Platform detected: {platformFlags}");

TraceLogger.WriteActionStart("Reserving space for Output Stream...");
output.SetLength(sizeBeforeCompr);

TraceLogger.WriteActionEnd();

long compressedLen = input.Length - headerLen;
string fileSize = SizeT.FormatSize(compressedLen);

TraceLogger.WriteActionStart($"Decompressing data... ({fileSize})");
ZLibCompressor.DecompressStream(input, output, -1, progressCallback);

TraceLogger.WriteActionEnd();
}

/** <summary> Decompresses the Contents of a SMF File as a RSB File. </summary>

<param name = "inputPath" > The Path where the File to be Decompressed is Located. </param>
<param name = "outputPath" > The Location where the Decompressed File will be Saved. </param> */

public static void DecompressFile(string inputPath, string outputPath, bool removeSmfExt = true,
                                  Action<long, long> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("SMF Decompression Started");

try
{

if(removeSmfExt)
PathHelper.RemoveExtension(ref outputPath);

TraceLogger.WriteDebug($"{inputPath} --> {outputPath}");

TraceLogger.WriteActionStart("Opening files...");

using var inFile = FileManager.OpenRead(inputPath);
using var outFile = FileManager.OpenWrite(outputPath);

TraceLogger.WriteActionEnd();

DecompressStream(inFile, outFile, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Decompress file");
}

TraceLogger.WriteLine("SMF Decompression Finished");

var outSize = FileManager.GetFileSize(outputPath);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}