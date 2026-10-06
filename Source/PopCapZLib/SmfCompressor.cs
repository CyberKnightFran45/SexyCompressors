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
                                  ProgressCallback progress = null)
{
long inputLen = input.Length;

if(inputLen > uint.MaxValue)
{
output.WriteUInt64(MAGIC);
output.WriteInt64(inputLen);
}

else
{
output.WriteUInt32(MAGIC);
output.WriteUInt32( (uint)inputLen);
}

ZLibCompressor.CompressStream(input, output, level, -1, progress);
}

// Compress transform

private static void CompressTransform(Stream input, Stream output, TraceContext ctx,
                                      CompressionLevel level, bool generateTag,
									  ProgressCallback progress,
									  out string hash)
{
hash = null;
ctx.ShowRatio = true;

CompressStream(input, output, level, progress);

if(generateTag)
{
output.Seek(0, SeekOrigin.Begin);

hash = SmfTagCreator.ComputeHash(output);
}

}

// Compress internal

private static void CompressInternal(TraceContext ctx, string inputPath, string outputPath,
                                     CompressionLevel level, bool generateTag,
                                     ProgressCallback progress)
{
PathHelper.AddExtension(ref outputPath, ".smf");

string tag = null;

TraceFileSteps.Run(ctx,
                   inputPath,
				   outputPath,
                   "Compressing data...",
				   (i, o, c) => CompressTransform(i, o, c, level, generateTag, progress, out tag)
);

if(tag != null)
{
PathHelper.ChangeExtension(ref outputPath, ".tag.smf");

File.WriteAllText(outputPath, tag);
}

}

/** <summary> Compresses the Contents of a RSB File as a SMF File. </summary>

<param name = "inputPath"> The Path where the File to be Compressed is Located. </param>
<param name = "outputPath"> The Location where the Compressed File will be Saved. </param>
<param name = "compressionLvl"> The Compression Level to be Used. </param> */

public static void CompressFile(string inputPath, string outputPath, CompressionLevel level,
                                bool generateTag = true,
                                ProgressCallback progress = null)
{

TraceExecutor.Run("SMF Compression", 
                  ctx => CompressInternal(ctx, inputPath, outputPath, level, generateTag, progress),
                  ("InputPath", inputPath),
                  ("OutputPath", outputPath),
				  ("CompressionLevel", level),
				  ("GenerateSmfTag", generateTag)
);

}

// Get RSB Stream

public static void DecompressStream(Stream input, Stream output,
                                    ProgressCallback progress = null)
{
Span<byte> rawMagic = stackalloc byte[8];
input.ReadExactly(rawMagic);

ulong inputMagic = BinaryPrimitives.ReadUInt64LittleEndian(rawMagic);
bool is64BitVariant = inputMagic >> 32 == 0;

long sizeBeforeCompr;

if(is64BitVariant)
sizeBeforeCompr = input.ReadInt64();

else
{
inputMagic = BinaryPrimitives.ReadUInt32LittleEndian(rawMagic[.. 4] );

sizeBeforeCompr = BinaryPrimitives.ReadUInt32LittleEndian(rawMagic.Slice(4, 4) );
}

if(inputMagic != MAGIC)
throw new Exception($"Invalid magic: 0x{inputMagic:X8}, expected: 0x{MAGIC:X8}");

output.SetLength(sizeBeforeCompr);

ZLibCompressor.DecompressStream(input, output, -1, progress);
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
				   (i, o, _) => DecompressStream(i, o, progress)
);

}

/** <summary> Decompresses the Contents of a SMF File as a RSB File. </summary>

<param name = "inputPath" > The Path where the File to be Decompressed is Located. </param>
<param name = "outputPath" > The Location where the Decompressed File will be Saved. </param> */

public static void DecompressFile(string inputPath, string outputPath, 
                                  bool removeSmfExt = true,
                                  ProgressCallback progress = null)
{

TraceExecutor.Run("SMF Decompression", 
                  ctx => DecompressInternal(ctx, inputPath, outputPath, removeSmfExt, progress),
                  ("InputPath", inputPath),
                  ("OutputPath", outputPath),
				  ("RemoveSmfExtension", removeSmfExt)
);

}

}

}