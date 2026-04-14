using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using BlossomLib.Modules.Compression;

namespace SexyCompressors.PopCapPackage
{
/// <summary> Packs the Content of a Directory as a PopCap Package (PAK). </summary>

public static class PakBuilder
{
// Sanitize Path

private static void SanitizePath(ref string target, PakPlatform platform)
{
char separator = platform == PakPlatform.Windows ? '\\' : '/';

using NativeString pathBuilder = new(target.Length);
var span = pathBuilder.AsSpan();

for(int i = 0; i < target.Length; i++)
{
char c = target[i];

span[i] = (c == '\\' || c == '/') ? separator : c;
}

ReadOnlySpan<char> sanitized = span;

if(sanitized.Length >= 2 && sanitized[0] == '.' && (sanitized[1] == '\\' || sanitized[1] == '/') )
sanitized = sanitized[2..];

target = new(sanitized);
}

// Get files

private static List<string> GetFiles(string baseDir, PakPlatform platform, out List<string> resNames)
{
resNames = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = PakHelper.GetResDir(baseDir);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

List<string> res = new();

foreach(string path in files)
{
string resName = Path.GetRelativePath(resDir, path);
SanitizePath(ref resName, platform);

res.Add(path);
resNames.Add(resName);
}

TraceLogger.WriteActionEnd();

return res;
}

// Write Header

private static void WriteHeader(Stream writer)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(PakConstants.MAGIC);
writer.WriteUInt32(PakConstants.VERSION);

TraceLogger.WriteActionEnd();
}

// Add Res to Buffer

private static void AddRes(string path, ReadOnlySpan<char> name, bool useZlib, CompressionLevel level,
                           ChunkedMemoryStream parent, ChunkedMemoryStream entries)
{
using var resStream = FileManager.OpenRead(path);

entries.WriteByte(0x00);
entries.WriteStringByLen8(name, PakConstants.ENCODING);

var rawSize = (uint)resStream.Length;
entries.WriteUInt32(rawSize);

if(useZlib)
{
long start = parent.Position;
ZLibCompressor.CompressStream(resStream, parent, level);

var sizeCompressed = (uint)(parent.Position - start);
entries.WriteUInt32(sizeCompressed);
}

else
FileManager.Process(resStream, parent);

long fileTime = DateTime.Now.ToFileTimeUtc();
entries.WriteInt64(fileTime);
}

// Add files

private static void AddFiles(List<string> files, List<string> names, bool alignData,
                             bool useZlib, CompressionLevel compressionLvl,
                             ChunkedMemoryStream parent, ChunkedMemoryStream entries,
                             Action<string, int, int> progressCallback)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

for(int i = 0; i < fileCount; i++)
{
string file = files[i];
string name = names[i];

progressCallback?.Invoke(name, i + 1, fileCount);

if(alignData)
{
bool isPtx = name.EndsWith(".ptx", StringComparison.OrdinalIgnoreCase);

PakHelper.AlignStream(parent, isPtx);
}

AddRes(file, name, useZlib, compressionLvl, parent, entries);
}

entries.WriteByte(PakConstants.ENTRIES_END);

TraceLogger.WriteActionEnd();
}

// Write PAK Entries

private static void WriteEntries(Stream writer, ChunkedMemoryStream entries)
{
TraceLogger.WriteActionStart("Writing entries...");

entries.Seek(0, SeekOrigin.Begin);
FileManager.Process(entries, writer);

TraceLogger.WriteActionEnd();
}

// Write Res data

private static void WriteRes(Stream writer, ChunkedMemoryStream data)
{
TraceLogger.WriteActionStart("Packing resources...");

data.Seek(0, SeekOrigin.Begin);
FileManager.Process(data, writer);

TraceLogger.WriteActionEnd();
}

// Pack content

private static void PackContent(string sourceDir, Stream target, PakPlatform platform,
                                bool useZlib, CompressionLevel compressionLvl,
                                Action<string, int, int> progressCallback)
{
TraceLogger.WriteLine("Step #2 - List Files:");
TraceLogger.WriteLine();

var files = GetFiles(sourceDir, platform, out var resNames);
int fileCount = files.Count;

if(fileCount == 0)
{
TraceLogger.WriteError($"'{sourceDir}' has no Files.");

return;
}

TraceLogger.WriteInfo($"Found: {fileCount} files");

TraceLogger.WriteLine("Step #3 - Write Metadata:");
TraceLogger.WriteLine();

WriteHeader(target);

TraceLogger.WriteLine("Step #4 - Add Files:");
TraceLogger.WriteLine();

using ChunkedMemoryStream resBlob = new();
using ChunkedMemoryStream entries = new();

bool alignData = platform == PakPlatform.Xbox360;

AddFiles(files, resNames, alignData, useZlib, compressionLvl, resBlob, entries, progressCallback);

TraceLogger.WriteLine("Step #5 - Write Resource Entries:");
TraceLogger.WriteLine();

WriteEntries(target, entries);

TraceLogger.WriteLine("Step #6 - Pack Resources:");
TraceLogger.WriteLine();

WriteRes(target, resBlob);
}

// Compress Stream

public static void Compress(string sourceDir, Stream target,
                            Action<string, int, int> progressCallback = null)
{
TraceLogger.WriteLine("Step #1 - Load PAK Config:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Loading config...");

string infoPath = Path.Combine(sourceDir, "PakInfo.json");

using var cfgStream = FileManager.OpenRead(infoPath);
var cfg = JsonSerializer.DeserializeObject<PakConfig>(cfgStream, PakConfig.Context);

TraceLogger.WriteActionEnd();

bool useZlib = cfg.UseZlib;
var compressionLvl = cfg.CompressionLvl ?? default;

var platform = cfg.Platform;

switch(platform)
{
case PakPlatform.XMEM:
TraceLogger.WriteError("XMEM is not supported, use xbcompress instead");
break;

case PakPlatform.TV:
ZipCompressor.CompressStream(sourceDir, target, default);
break;

case PakPlatform.Windows:
XorStream encryptor = new(target, PakConstants.KEY);

PackContent(sourceDir, encryptor, platform, useZlib, compressionLvl, progressCallback);
encryptor.Dispose();
break;

default:
PackContent(sourceDir, target, platform, useZlib, compressionLvl, progressCallback);
break;
}

}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile,
                        Action<string, int, int> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("PAK Build Started");

try
{
PathHelper.ChangeExtension(ref targetFile, ".pak");
TraceLogger.WriteDebug($"{sourceDir} --> {targetFile}");

using var pakStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, pakStream, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Pack folder");
}

TraceLogger.WriteLine("PAK Build Finished");

var outSize = FileManager.GetFileSize(targetFile);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}