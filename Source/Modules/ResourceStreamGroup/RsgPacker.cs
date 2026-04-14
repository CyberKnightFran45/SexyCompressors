using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using BlossomLib.Modules.Compression;

namespace SexyCompressors.ResourceStreamGroup
{
/// <summary> Packs the Content of a Directory as a PopCap ResGroup (RSG). </summary>

public static class RsgPacker
{
// Get files

private static void GetFiles(string baseDir, out List<string> res, out List<string> textures,
                             out List<string> resNames, out List<string> ptxNames,
                             out List<string> ptxInfos)
{
res = new();
textures = new();
resNames = new();
ptxNames = new();
ptxInfos = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = RsgHelper.GetResDir(baseDir);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

foreach(string path in files)
{
string resName = Path.GetRelativePath(resDir, path);
bool isPtx = resName.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);

if(isPtx)
{
string infoPath = RsgHelper.BuildGpuPath(baseDir, resName);

if(!Path.Exists(infoPath) )
{
TraceLogger.WriteWarn($"Missing config for '{resName}', this resource will be Omited.");

continue;
}

textures.Add(path);

ptxNames.Add(resName);
ptxInfos.Add(infoPath);
}

else
{
res.Add(path);

resNames.Add(resName);
}

}

TraceLogger.WriteActionEnd();
}

// Add single file and Align if required

private static void AddFile(FileStream source, int size, ChunkedMemoryStream target)
{
FileManager.Process(source, target);

int padding = RsgHelper.ComputePadding(size, false);
target.Fill(padding);
}

// Add Res to Buffer

private static RsgResidentInfo AddRes(FileStream res, ChunkedMemoryStream part0)
{
var offset = (uint)part0.Position;
var size = (int)res.Length;

AddFile(res, size, part0);

return new(offset, (uint)size);
}

// Add files

private static void AddFiles(List<string> files, List<string> names, ChunkedMemoryStream part0,
                             Dictionary<string, RsgResidentInfo> entries,
                             Action<string, int, int> progressCallback)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

for(int i = 0; i < fileCount; i++)
{
string file = files[i];
string name = names[i];

progressCallback?.Invoke(name, i + 1, fileCount);

using var resStream = FileManager.OpenRead(file);

var residentInfo = AddRes(resStream, part0);
entries.Add(name, residentInfo);
}

TraceLogger.WriteActionEnd();
}

// Add Texture to Buffer

private static RsgTextureInfo AddPtx(FileStream res, string infoPath, ChunkedMemoryStream part1)
{
using var infoStream = FileManager.OpenRead(infoPath);
var metadata = JsonSerializer.DeserializeObject<TextureFileMetadata>(infoStream, TextureFileMetadata.Context);

var offset = (uint)part1.Position;
var size = (int)res.Length;

AddFile(res, size, part1);

return new(offset, (uint)size, metadata.TextureID, metadata.Width, metadata.Height);
}

// Add textures

private static void AddTextures(List<string> files, List<string> names, List<string> infos,
                                ChunkedMemoryStream part1,
								Dictionary<string, RsgTextureInfo> entries,
                                Action<string, int, int> progressCallback)
{
TraceLogger.WriteActionStart("Adding textures...");

int fileCount = files.Count;

for(int i = 0; i < fileCount; i++)
{
string file = files[i];
string name = names[i];

progressCallback?.Invoke(name, i + 1, fileCount);

using var resStream = FileManager.OpenRead(file);

string infoPath = infos[i];
var textureInfo = AddPtx(resStream, infoPath, part1);

entries.Add(name, textureInfo);
}

TraceLogger.WriteActionEnd();
}

// Write fragment

private static void WriteFragment(ChunkedMemoryStream writer, ReadOnlySpan<char> fragment,
                                  int structOffset, Endianness endian)
{
writer.WriteString(fragment, EncodingType.UTF8);

writer.WriteInt24(structOffset / 4, endian);
}

// Split path and write offsets

private static void EncodePath(ChunkedMemoryStream writer, string path, Endianness endian,
                               ref string prevPath, ref int position)
{
int firstDiff = 0;
int maxLen = Math.Max(prevPath.Length, path.Length);

for(int i = 0; i < maxLen; i++)
{

if(i >= prevPath.Length || i >= path.Length || prevPath[i] != path[i])
{
firstDiff = i;

break;
}

}

for(int i = firstDiff; i < path.Length; i++)
{
char c = path[i];
var singleChar = MemoryMarshal.CreateReadOnlySpan(in c, 1);

WriteFragment(writer, singleChar, position, endian);
position++;
}

WriteFragment(writer, "\0", 0, endian);

prevPath = path;
}

// Build ResMap

private static void BuildResMap(ChunkedMemoryStream writer, ResourceMap entries, Endianness endian)
{
TraceLogger.WriteActionStart("Building ResMap...");

string prevPath = string.Empty;
int position = 0;

var residents = entries.ResidentFiles.OrderBy(r => r.Key, StringComparer.Ordinal);

foreach(var res in residents)
{
string path = res.Key;
var info = res.Value;

EncodePath(writer, path, endian, ref prevPath, ref position);

writer.WriteUInt32(0);
info.Write(writer, endian);
}

var textures = entries.TextureFiles.OrderBy(t => t.Key, StringComparer.Ordinal);

foreach(var img in textures)
{
string path = img.Key;
var info = img.Value;

EncodePath(writer, path, endian, ref prevPath, ref position);

writer.WriteUInt32(1, endian);
info.Write(writer, endian);
}

TraceLogger.WriteActionEnd();
}

// Save ResMap

private static void SaveMap(Stream writer, ChunkedMemoryStream resMap)
{
TraceLogger.WriteActionStart("Saving ResMap...");

resMap.Seek(0, SeekOrigin.Begin);
FileManager.Process(resMap, writer);

TraceLogger.WriteActionEnd();
}

// Compress Part

public static void CompressPart(ChunkedMemoryStream part, uint flags,
                                out int rawSize, out int sizeCompressed)
{
rawSize = (int)part.Length;

if(flags < 2)
{
sizeCompressed = rawSize;

return; // No compression
}

else
{
CompressionLevel level = flags == 3 ? CompressionLevel.SmallestSize : default;

using ChunkedMemoryStream zlibStream = new();

part.Seek(0, SeekOrigin.Begin);
ZLibCompressor.CompressStream(part, zlibStream, level);

part.SetLength(0);
part.Seek(0, SeekOrigin.Begin);

zlibStream.Seek(0, SeekOrigin.Begin);
FileManager.Process(zlibStream, part);

sizeCompressed = (int)part.Length;

int padding = RsgHelper.ComputePadding(sizeCompressed, true);
part.Fill(padding);
}

}

// Write Part0

private static void WritePart0(Stream writer, ChunkedMemoryStream part0)
{
TraceLogger.WriteActionStart("Writing Part0...");

part0.Seek(0, SeekOrigin.Begin);
FileManager.Process(part0, writer);

TraceLogger.WriteActionEnd();
}

// Write Part1

private static void WritePart1(Stream writer, ChunkedMemoryStream part1)
{
TraceLogger.WriteActionStart("Writing Part1...");

part1.Seek(0, SeekOrigin.Begin);
FileManager.Process(part1, writer);

TraceLogger.WriteActionEnd();
}

// Compress Stream

public static void Compress(string sourceDir, Stream target,
                            Action<string, int, int> progressCallback = null)
{
int step = 1;

TraceLogger.WriteLine($"Step #{step} - Load RSG Config:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Loading config...");

string infoPath = Path.Combine(sourceDir, "PacketInfo.json");

using var cfgStream = FileManager.OpenRead(infoPath);
var cfg = JsonSerializer.DeserializeObject<RsgParams>(cfgStream, RsgParams.Context);

TraceLogger.WriteActionEnd();

step++;

TraceLogger.WriteLine($"Step #{step} - List Files:");
TraceLogger.WriteLine();

GetFiles(sourceDir, out var files, out var textures, out var resNames, out var ptxNames, out var ptxInfos);

int fileCount = files.Count;
int textureCount = textures.Count;

if(fileCount == 0 && textureCount == 0)
{
TraceLogger.WriteError($"'{sourceDir}' has no Files.");

return;
}

TraceLogger.WriteInfo($"Found: {fileCount} files | {textureCount} textures");

step++;

TraceLogger.WriteLine($"Step #{step} - Add Files:");
TraceLogger.WriteLine();

ResourceMap entries = new(fileCount, textureCount);

using ChunkedMemoryStream part0 = new();
using ChunkedMemoryStream part1 = new();

var endian = cfg.Endian;
var compressionFlags = cfg.CompressionFlags;

int residentSize = 0;
int residentSizeZl = 0;

bool hasResidents = fileCount > 0;

if(hasResidents)
{
AddFiles(files, resNames, part0, entries.ResidentFiles, progressCallback);

CompressPart(part0, compressionFlags, out residentSize, out residentSizeZl);
}

int gpuSize = 0;
int gpuSizeZl = 0;

bool hasTextures = textureCount > 0;

if(hasTextures)
{
AddTextures(textures, ptxNames, ptxInfos, part1, entries.TextureFiles, progressCallback);

CompressPart(part1, compressionFlags, out gpuSize, out gpuSizeZl);
}

using ChunkedMemoryStream resMap = new();
BuildResMap(resMap, entries, endian);

int resMapPos = 92;
var resMapLen = (int)resMap.Length;

int entriesBlock = resMapPos + resMapLen;
int mapPadding = RsgHelper.ComputePadding(entriesBlock, true);

resMap.Fill(mapPadding);

var sectionLen = (uint)(entriesBlock + mapPadding);

uint residentPos = sectionLen;
var gpuPos = (uint)(residentPos + part0.Length);

step++;

TraceLogger.WriteLine($"Step #{step} - Write Metadata:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Writing header...");

target.WriteUInt32(RsgConstants.MAGIC, endian);

RsgInfo info = new()
{
MajorVersion = cfg.MajorVersion,
MinorVersion = cfg.MinorVersion,
CompressionFlags = compressionFlags,
SectionLength = sectionLen,
ResidentDataOffset = residentPos,
ResidentDataSizeCompressed = (uint)residentSizeZl,
ResidentDataSize = (uint)residentSize,
GPUDataOffset = gpuPos,
GPUDataSizeCompressed = (uint)gpuSizeZl,
GPUDataSize = (uint)gpuSize,
ResMapLength = (uint)resMapLen,
ResMapOffset = (uint)resMapPos
};

info.Write(target, endian);

TraceLogger.WriteActionEnd();

step++;

TraceLogger.WriteLine($"Step #{step} - Save ResMap:");
TraceLogger.WriteLine();

SaveMap(target, resMap);

if(part0.Length > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Write Part0:");
TraceLogger.WriteLine();

WritePart0(target, part0);
}

if(part1.Length > 0)
{
step++;

TraceLogger.WriteLine($"Step #{step} - Write Part1:");
TraceLogger.WriteLine();

WritePart1(target, part1);
}

}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile,
                        Action<string, int, int> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("RSG Build Started");

try
{
PathHelper.ChangeExtension(ref targetFile, ".rsg");
TraceLogger.WriteDebug($"{sourceDir} --> {targetFile}");

using var rsgStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, rsgStream, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Pack folder");
}

TraceLogger.WriteLine("RSG Build Finished");

var outSize = FileManager.GetFileSize(targetFile);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}