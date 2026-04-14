using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Extracts content from a PopCap RSG Stream. </summary>

public static class RsgUnpacker
{
// Read Path fragment

private static void ReadFragment(Stream reader, Endianness endian, out string fragment, out int structOffset)
{
using var fOwner = reader.ReadString(1, EncodingType.UTF8); // UTF-8 char

fragment = fOwner.ToString();
structOffset = reader.ReadInt24(endian) * 4;
}

// Append path fragment

private static void AppendFragment(ReadOnlySpan<char> fragment, int structOffset, NativeString rawPaths,
                                   ref int curLength, Dictionary<int, string> pendingOffsets)
{

if(structOffset != 0)
pendingOffsets[structOffset] = rawPaths.Substring(0, curLength);

foreach(char c in fragment)
rawPaths[curLength++] = c;

}

// Read entry

private static void ReadEntry(Stream reader, Endianness endian, ResourceMap resMap, string path)
{
uint rawType = reader.ReadUInt32(endian);
var type = (ResourceType)rawType;

switch(type)
{
case ResourceType.Common:
var residentInfo = RsgResidentInfo.Read(reader, endian);

resMap.AddResident(path, residentInfo);
break;

case ResourceType.Texture:
var textureInfo = RsgTextureInfo.Read(reader, endian);

resMap.AddTexture(path, textureInfo);
break;

default:
TraceLogger.WriteError($"Unknown resource: {path} ({rawType:X8}) at Pos: {reader.Position}");
break;
}

}

// Process Res

private static void ProcessRes(Stream reader, Endianness endian, ref int curLength, NativeString rawPaths,
                               Dictionary<int, string> pendingOffsets, long baseOffset, ResourceMap resMap)
{

if(curLength > 0)
{
string path = rawPaths.Substring(0, curLength);
ReadEntry(reader, endian, resMap, path);

foreach(var p in pendingOffsets)
{
int pathOffset = p.Key;
long expectedPos = baseOffset + pathOffset;

if(expectedPos == reader.Position)
{
string oldPath = p.Value;
int oldLen = oldPath.Length;

if(rawPaths.Length < oldLen)
rawPaths.Realloc(oldLen * 2);
			
oldPath.CopyTo(rawPaths.AsSpan() );
curLength = oldLen;

pendingOffsets.Remove(pathOffset);
break;
}

}

}

}

// Log Resources embedded

private static void LogResCount(int fileCount, int textureCount)
{

if(fileCount > 0 && textureCount > 0)
{
int total = fileCount + textureCount;

TraceLogger.WriteInfo($"Resources embedded: {total} ({fileCount} files, {textureCount} textures)");
}

else if(fileCount > 0)
TraceLogger.WriteInfo($"Resources embedded: {fileCount} files");
    
else if(textureCount > 0)
TraceLogger.WriteInfo($"Resources embedded: {textureCount} textures");
    
else
TraceLogger.WriteInfo("Resources embedded: <none>");
 
}

// Load ResourceMap

private static ResourceMap LoadResMap(Stream reader, uint offset, uint listSize, Endianness endian)
{
TraceLogger.WriteActionStart("Loading ResMap...");
reader.Seek(offset, SeekOrigin.Begin);

Dictionary<int, string> pendingOffsets = new();
ResourceMap resMap = new();

using NativeString rawPaths = new(256);
int curLength = 0;

long end = offset + listSize;

while(reader.Position < end)
{
ReadFragment(reader, endian, out var fragment, out var structOffset);

if(fragment == "\0")
ProcessRes(reader, endian, ref curLength, rawPaths, pendingOffsets, offset, resMap);

else
AppendFragment(fragment, structOffset, rawPaths, ref curLength, pendingOffsets);

}

TraceLogger.WriteActionEnd();

LogResCount(resMap.FileCount, resMap.TextureCount);

return resMap;
}

// Check if Extraction can be made to RAM Disk

private static void RedirectGlobalOutput(ref string outputDir, ResourceMap map)
{
int fileCount = map.FileCount + map.TextureCount;

long residentBytes = map.ResidentFiles.Sum(e => e.Value.Size);
long textureBytes = map.TextureFiles.Sum(e => e.Value.Size);

long totalBytes = residentBytes + textureBytes;

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes);
}

// Log Part Size

private static void LogPartSize(uint rawSize, uint sizeCompressed, bool compressed)
{
string partSize = SizeT.FormatSize(rawSize);

if(compressed)
{
string partSizeZl = SizeT.FormatSize(sizeCompressed);

TraceLogger.WriteInfo($"Part Size: {partSize} | Compressed: {partSizeZl}");
}

else
TraceLogger.WriteInfo($"Part Size: {partSize}");

}

// Load RSG Part into Memory

private static NativeMemoryOwner<byte> ReadPart(Stream reader, uint offset, uint sizeCompressed, 
                                                uint rawSize, bool compressed)
{
NativeMemoryOwner<byte> pOwner = new(rawSize);
var buffer = pOwner.AsSpan();

reader.Seek(offset, SeekOrigin.Begin);

if(!compressed)
{
reader.ReadExactly(buffer);

return pOwner;
}

using SubStream partStream = new(reader, offset, sizeCompressed);
using ZLibStream decompressor = new(partStream, CompressionMode.Decompress);

int totalRead = 0;

while(totalRead < rawSize)
{
int read = decompressor.Read(buffer[totalRead..] );

if(read == 0)
break;

totalRead += read;
}

return pOwner;
}

// Read Part0

private static NativeMemoryOwner<byte> ReadPart0(Stream reader, uint offset, uint sizeCompressed,
                                                 uint rawSize, uint compressionFlags)
{
bool useZlib = (compressionFlags & 2) != 0;

TraceLogger.WriteActionStart("Reading Part0...");
var part0 = ReadPart(reader, offset, sizeCompressed, rawSize, useZlib);

TraceLogger.WriteActionEnd();

LogPartSize(rawSize, sizeCompressed, useZlib);

return part0;
}

// Read Part1

private static NativeMemoryOwner<byte> ReadPart1(Stream reader, uint offset, uint sizeCompressed,
                                                 uint rawSize, uint compressionFlags)
{
bool useZlib = (compressionFlags & 1) != 0;

TraceLogger.WriteActionStart("Reading Part1...");
var part1 = ReadPart(reader, offset, sizeCompressed, rawSize, useZlib);

TraceLogger.WriteActionEnd();

LogPartSize(rawSize, sizeCompressed, useZlib);

return part1;
}

// Write ResData in Chunks

private static void WriteRes(FileStream writer, NativeMemoryOwner<byte> buffer, uint offset, uint size)
{
const int MAX_CHUNK = int.MaxValue; // 2 GB

long remaining = size;
ulong currOffset = offset;

while(remaining > 0)
{
var chunkSize = (int)Math.Min(remaining, MAX_CHUNK);

var resData = buffer.AsSpan(currOffset, chunkSize); 
writer.Write(resData);
    
currOffset += (ulong)chunkSize;
remaining -= chunkSize;
}

}

// Extract Res

private static void ExtractRes(string outDir, NativeMemoryOwner<byte> buffer, string resName,
                               in RsgResidentInfo info)
{
string resPath = RsgHelper.BuildResourcePath(outDir, resName);
using var resStream = FileManager.OpenWrite(resPath);

WriteRes(resStream, buffer, info.Offset, info.Size);
}

// Extract Residents from RSG (single thread)

private static void ExtractResidents(Dictionary<string, RsgResidentInfo> entries,
                                     NativeMemoryOwner<byte> buffer,
                                     string outDir)
{

foreach(var entry in entries)
ExtractRes(outDir, buffer, entry.Key, entry.Value);

}

// Extract Residents from RSG (multi-thread)

private static void ExtractResidentsInParallel(Dictionary<string, RsgResidentInfo> entries,
                                               NativeMemoryOwner<byte> buffer,
                                               string outDir)
{
var batches = BatchHelper.Batch(entries, entries.Count, buffer.Size);

foreach(var batch in batches)
Parallel.ForEach(batch, entry => ExtractRes(outDir, buffer, entry.Key, entry.Value) ); 

}

// Extract Files from Part0

private static void ExtractResidentsCore(Dictionary<string, RsgResidentInfo> entries,
                                         NativeMemoryOwner<byte> buffer,
                                         string outDir)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 64;

TraceLogger.WriteActionStart("Extracting files...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractResidents(entries, buffer, outDir);

else
ExtractResidentsInParallel(entries, buffer, outDir);

TraceLogger.WriteActionEnd();
}

// Extract Ptx

private static void ExtractPtx(string outDir, NativeMemoryOwner<byte> buffer,
                               string name, in RsgTextureInfo info)
{
string resPath = RsgHelper.BuildResourcePath(outDir, name);

using var resStream = FileManager.OpenWrite(resPath);
WriteRes(resStream, buffer, info.Offset, info.Size);

TextureFileMetadata metadata = new(info.TextureID, info.Width, info.Height);
string infoPath = RsgHelper.BuildGpuPath(outDir, name);

using var infoStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(metadata, infoStream, TextureFileMetadata.Context);
}

// Extract Images from RSG (single thread)

private static void ExtractTextures(Dictionary<string, RsgTextureInfo> entries,
                                    NativeMemoryOwner<byte> buffer,
                                    string outDir)
{

foreach(var entry in entries)
ExtractPtx(outDir, buffer, entry.Key, entry.Value);

}

// Extract Images from RSG (multi-thread)

private static void ExtractTexturesInParallel(Dictionary<string, RsgTextureInfo> entries,
                                              NativeMemoryOwner<byte> buffer,
									          string outDir)
{
var batches = BatchHelper.Batch(entries, entries.Count, buffer.Size);

foreach(var batch in batches)
Parallel.ForEach(batch, entry => ExtractPtx(outDir, buffer, entry.Key, entry.Value) ); 

}

// Extract Files from Part1 (Core)

private static void ExtractTexturesCore(Dictionary<string, RsgTextureInfo> entries,
                                        NativeMemoryOwner<byte> buffer,
                                        string outDir)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 128;

TraceLogger.WriteActionStart("Extracting textures...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractTextures(entries, buffer, outDir);

else
ExtractTexturesInParallel(entries, buffer, outDir);

TraceLogger.WriteActionEnd();
}

// Decompress RSG Stream

public static void Decompress(Stream source, string outputDir)
{
const string ERROR_INVALID_FLAGS = "Invalid ResGroup identifier: {0:X8}, expected: 'pgsr' ('rsgp' in BigEndian)";

int step = 1;

TraceLogger.WriteLine($"Step #{step} - Read Metadata:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Reading header...");

uint flags = source.ReadUInt32();
Endianness endian;

switch(flags)
{
case RsgConstants.MAGIC:
endian = Endianness.LittleEndian;
break;

case RsgConstants.MAGIC_BE:
endian = Endianness.BigEndian;
break;

default:
TraceLogger.WriteError(string.Format(ERROR_INVALID_FLAGS, flags) );
return;
}

var info = RsgInfo.Read(source, endian);

var majVer = info.MajorVersion;
var minVer = info.MinorVersion;

if(!Enum.IsDefined(majVer) || !Enum.IsDefined(minVer) )
TraceLogger.WriteWarn($"Unknown version: V{majVer}.{minVer}");

TraceLogger.WriteActionEnd();

step++;

TraceLogger.WriteLine($"Step #{step} - Load Resource Map:");
TraceLogger.WriteLine();

var resMap = LoadResMap(source, info.ResMapOffset, info.ResMapLength, endian);
RedirectGlobalOutput(ref outputDir, resMap); // Redirect to RAM Disk if posible

uint compressionFlags = info.CompressionFlags;
uint residentSize = info.ResidentDataSize;

NativeMemoryOwner<byte> part0 = new();

if(residentSize > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Read Part0:");
TraceLogger.WriteLine();

uint residentOffset = info.ResidentDataOffset;
uint residentSizeZl = info.ResidentDataSizeCompressed;

part0 = ReadPart0(source, residentOffset, residentSizeZl, residentSize, compressionFlags);
}

uint gpuSize = info.GPUDataSize;

NativeMemoryOwner<byte> part1 = new();

if(gpuSize > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Read Part1:");
TraceLogger.WriteLine();

uint gpuOffset = info.GPUDataOffset;
uint gpuSizeZl = info.GPUDataSizeCompressed;

part1 = ReadPart1(source, gpuOffset, gpuSizeZl, gpuSize, compressionFlags);
}

if(part0.Size > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Extract Resident Files:");
TraceLogger.WriteLine();

ExtractResidentsCore(resMap.ResidentFiles, part0, outputDir);
}

part0.Dispose();

if(part1.Size > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Extract Texture Files:");
TraceLogger.WriteLine();

ExtractTexturesCore(resMap.TextureFiles, part1, outputDir);
}

resMap.Clear();
part1.Dispose();

step++;

TraceLogger.WriteLine($"Step #{step} - Save RSG Config:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Saving config...");

RsgParams cfg = new(endian, majVer, minVer, info.CompressionFlags);
string infoPath = Path.Combine(outputDir, "PacketInfo.json");

using var cfgStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(cfg, cfgStream, RsgParams.Context);

TraceLogger.WriteActionEnd();
}

/// <summary> Decompress the Content from a RSG Stream. </summary>

public static void Unpack(string inputFile, string outputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("RSG Unpacking Started");

try
{
PathHelper.ChangeExtension(ref outputDir, ".packet");
TraceLogger.WriteDebug($"{inputFile} --> {outputDir}");

using var rsgStream = FileManager.OpenRead(inputFile);
Decompress(rsgStream, outputDir);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Unpack file");
}

TraceLogger.WriteLine("RSG Unpacking Finished");
}

}

}