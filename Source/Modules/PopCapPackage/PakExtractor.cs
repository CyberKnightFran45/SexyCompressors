using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BlossomLib.Modules.Compression;

namespace SexyCompressors.PopCapPackage
{
/// <summary> Unpacks the Content of a PopCap Package (PAK). </summary>

public static class PakExtractor
{
// Read info

private static PakPlatform ReadInfo(Stream reader)
{
TraceLogger.WriteActionStart("Reading header...");

var magic = reader.ReadUInt32();

PakPlatform platform = magic switch
{
PakConstants.MAGIC => PakPlatform.Xbox360,
0x4D37BD37 => PakPlatform.Windows,
0x04034B50 => PakPlatform.TV,
0x0FF512ED => PakPlatform.XMEM,
_ => (PakPlatform)(-1)
};

TraceLogger.WriteActionEnd();

return platform;
}

// Read PAK Entries

private static Dictionary<string, ResInfo> ReadEntries(Stream reader, out long totalBytes)
{
Dictionary<string, ResInfo> entries = new();
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

while(true)
{
byte contentType = reader.ReadUInt8();

if( (contentType & PakConstants.ENTRIES_END) != 0)
break;

using var pOwner = reader.ReadStringByLen8(PakConstants.ENCODING);
string resName = pOwner.ToString();

var info = ResInfo.Read(reader);
entries.Add(resName, info);

totalBytes += info.Size;
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Resources embedded: {entries.Count}");

return entries;
}

// Read PAK Entries (ZLib variant)

private static Dictionary<string, ResInfoZlib> ReadEntriesZl(Stream reader, out long totalBytes)
{
Dictionary<string, ResInfoZlib> entries = new();
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

while(true)
{
byte contentType = reader.ReadUInt8();

if( (contentType & PakConstants.ENTRIES_END) != 0)
break;

using var pOwner = reader.ReadStringByLen8(PakConstants.ENCODING);
string resName = pOwner.ToString();

var info = ResInfoZlib.Read(reader);
entries.Add(resName, info);

totalBytes += info.RawSize;
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteInfo($"Resources embedded: {entries.Count}");

return entries;
}

// Get relative offset

private static long GetOffset(NativeBuffer buffer, bool checkAlignment, long dataStart,
                              bool isPtx, long baseOffset, uint size)
{
long blobPos = dataStart + baseOffset;
bool alignData = checkAlignment && PakHelper.IsAligned(blobPos, isPtx);

long offset = 0;

if(alignData)
{
ushort alignSize = buffer.GetUInt16(baseOffset);

offset += 2;
offset += alignSize;
}

offset += size;

return offset;
}

// Precalculate offsets

private static Dictionary<string, long> ComputeOffsets(long dataStart, NativeBuffer buffer,
                                                       bool checkAlignment,
                                                       Dictionary<string, ResInfo> entries)
{
Dictionary<string, long>  map = new(entries.Count);
long offset = 0;

foreach(var e in entries)
{
var name = e.Key;
map.Add(name, offset);

bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);
var info = e.Value;

offset += GetOffset(buffer, checkAlignment, dataStart, isPtx, offset, info.Size);
}

return map;
}

// Precalculate offsets (ZLib variant)

private static Dictionary<string, long> ComputeOffsetsZl(long dataStart, NativeBuffer buffer,
                                                         bool checkAlignment,
                                                         Dictionary<string, ResInfoZlib> entries)
{
Dictionary<string, long>  map = new(entries.Count);
long offset = 0;

foreach(var e in entries)
{
var name = e.Key;
map.Add(name, offset);

bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);
var info = e.Value;

offset += GetOffset(buffer, checkAlignment, dataStart, isPtx, offset, info.SizeCompressed);
}

return map;
}

// Decompress ZLib Stream

private static void DecompressStream(Stream target, ReadOnlySpan<byte> data)
{
using ChunkedMemoryStream compressed = new();

compressed.Write(data);
compressed.Seek(0, SeekOrigin.Begin);

using ZLibStream decompressor = new(compressed, CompressionMode.Decompress);

ZLibCompressor.DecompressStream(decompressor, target); 
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer, long offset, string path,
                             int sizeCompressed, int rawSize, long creationTime)
{
using var resStream = FileManager.OpenWrite(path);
resStream.SetLength(rawSize);

if(sizeCompressed > 0)
{
var zlData = buffer.GetView(offset, sizeCompressed);

DecompressStream(resStream, zlData);
}

else
{
var rawData = buffer.GetView(offset, rawSize);

resStream.Write(rawData);
}

var timeUtc = DateTime.FromFileTimeUtc(creationTime);
File.SetLastWriteTimeUtc(path, timeUtc);
}

// Extract res

private static void ExtractRes(NativeBuffer buffer, long offset, string path, in ResInfo info)
{
var size = (int)Math.Min(info.Size, int.MaxValue);

WriteRes(buffer, offset, path, 0, size, info.CreationTime);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir, long dataStart, NativeBuffer buffer, bool checkAlignment,
                                 Dictionary<string, ResInfo> entries)
{
var offsetMap = ComputeOffsets(dataStart, buffer, checkAlignment, entries);

foreach(var entry in entries)
{
string resName = entry.Key;
string resPath = PakHelper.BuildResourcePath(outputDir, resName);

ExtractRes(buffer, offsetMap[resName], resPath, entry.Value);
}

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                           bool checkAlignment,
                                           Dictionary<string, ResInfo> entries)
{
var offsetMap = ComputeOffsets(dataStart, buffer, checkAlignment, entries);
var batches = BatchHelper.Batch(entries, entries.Count, buffer.Size);

foreach(var batch in batches)
{

Parallel.ForEach(batch, entry =>
{
string resName = entry.Key;
string resPath = PakHelper.BuildResourcePath(outputDir, resName);

ExtractRes(buffer, offsetMap[resName], resPath, entry.Value);
} ); 

}

}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                     bool checkAlignment,
                                     Dictionary<string, ResInfo> entries)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 64;

TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractFiles(outputDir, dataStart, buffer, checkAlignment, entries);

else
ExtractFilesInParallel(outputDir, dataStart, buffer, checkAlignment, entries);

TraceLogger.WriteActionEnd();
}

// Extract res (ZLib variant)

private static void ExtractResZl(NativeBuffer buffer, long offset, string path, in ResInfoZlib info)
{
var sizeCompressed = (int)Math.Min(info.SizeCompressed, int.MaxValue);
var rawSize = (int)Math.Min(info.RawSize, int.MaxValue);

WriteRes(buffer, offset, path, sizeCompressed, rawSize, info.CreationTime);
}

// Extract Zlib resources (single thread)

private static void ExtractZlFiles(string outputDir, long dataStart, NativeBuffer buffer,
                                   bool checkAlignment,
                                   Dictionary<string, ResInfoZlib> entries)
{
var offsetMap = ComputeOffsetsZl(dataStart, buffer, checkAlignment, entries);

foreach(var entry in entries)
{
string resName = entry.Key;
string resPath = PakHelper.BuildResourcePath(outputDir, resName);

ExtractResZl(buffer, offsetMap[resName], resPath, entry.Value);
}

}

// Extract ZLib resources (multi-thread)

private static void ExtractZlFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                             bool checkAlignment,
                                             Dictionary<string, ResInfoZlib> entries)
{
var offsetMap = ComputeOffsetsZl(dataStart, buffer, checkAlignment, entries);
var batches = BatchHelper.Batch(entries, entries.Count, buffer.Size);

foreach(var batch in batches)
{

Parallel.ForEach(batch, entry =>
{
string resName = entry.Key;
string resPath = PakHelper.BuildResourcePath(outputDir, resName);

ExtractResZl(buffer, offsetMap[resName], resPath, entry.Value);
} ); 

}

}

// Extract ZLib resources (Core)

private static void ExtractZlFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                       bool checkAlignment,
                                       Dictionary<string, ResInfoZlib> entries)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 64;

TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractZlFiles(outputDir, dataStart, buffer, checkAlignment, entries);

else
ExtractZlFilesInParallel(outputDir, dataStart, buffer, checkAlignment, entries);

TraceLogger.WriteActionEnd();
}

// Extract contents

private static void ExtractContent(Stream source, bool alignData, bool useZlib,
                                   ref string outputDir, ref int step)
{
uint version = source.ReadUInt32();
var expectedVer = PakConstants.VERSION;

if(version != expectedVer)
TraceLogger.WriteWarn($"Unknown version: v{version} - Expected: v{expectedVer}");

step++;

TraceLogger.WriteLine($"Step #{step} - Read Resource Entries:");
TraceLogger.WriteLine();

Dictionary<string, ResInfo> entries;
Dictionary<string, ResInfoZlib> entriesZl;

int fileCount;
long totalBytes;

if(useZlib)
{
entries = null;

entriesZl = ReadEntriesZl(source, out totalBytes);
fileCount = entriesZl.Count;
}

else
{
entries = ReadEntries(source, out totalBytes);
fileCount = entries.Count;

entriesZl = null;
}

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes);

step++;

TraceLogger.WriteLine($"Step #{step} - Extract Resources:");
TraceLogger.WriteLine();

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

if(entriesZl is not null)
ExtractZlFilesCore(outputDir, dataStart, resBlob, alignData, entriesZl);

else
ExtractFilesCore(outputDir, dataStart, resBlob, alignData, entries);

}

// Decompress Stream

public static void Decompress(Stream source, string outputDir)
{
int step = 1;

TraceLogger.WriteLine($"Step #{step} - Read Metadata:");
TraceLogger.WriteLine();

var platform = ReadInfo(source);
bool useZlib = false;

switch(platform)
{
case PakPlatform.XMEM:
TraceLogger.WriteError("XMEM is not supported, use xbdecompress instead");
return;

case PakPlatform.TV:
ZipCompressor.DecompressStream(source, outputDir);
break;

case PakPlatform.Xbox360:
useZlib = true;

ExtractContent(source, true, useZlib, ref outputDir, ref step);
return;

case PakPlatform.Windows:
XorStream decryptor = new(source, PakConstants.KEY);

ExtractContent(decryptor, false, useZlib, ref outputDir, ref step);
decryptor.Dispose();
break;

default:
TraceLogger.WriteError("Invalid Package identifier");
return;
}

step++;

TraceLogger.WriteLine($"Step #{step} - Save PAK Config:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Saving config...");

PakConfig cfg = new(platform, useZlib);
string infoPath = Path.Combine(outputDir, "PakInfo.json");

using var cfgStream = FileManager.OpenWrite(infoPath);
JsonSerializer.SerializeObject(cfg, cfgStream, PakConfig.Context);

TraceLogger.WriteActionEnd();
}

// Unpack Stream as Dir

public static void Unpack(string inputFile, string outputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("PAK Extraction Started");

try
{
TraceLogger.WriteDebug($"{inputFile} --> {outputDir}");

using var pakStream = FileManager.OpenRead(inputFile);
Decompress(pakStream, outputDir);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Unpack file");
}

TraceLogger.WriteLine("PAK Extraction Finished");
}

}

}