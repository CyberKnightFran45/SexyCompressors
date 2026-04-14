using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SexyCompressors.ArcVPackage
{
/// <summary> Packs the Content of a Directory as a ARC-V Package. </summary>

public static class ArcvPacker
{
// Get files

private static List<KeyValuePair<uint, string>> GetFiles(string baseDir, bool ignoreNames,
                                                         out List<string> names)
{
names = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = ArcvHelper.GetResDir(baseDir);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

Dictionary<uint, string> res = new();

foreach(string path in files)
{
string name = Path.GetFileNameWithoutExtension(path);

if(!ignoreNames)
names.Add(name);

uint id = ArcvHelper.GetID(name);
res.Add(id, path);
}

TraceLogger.WriteActionEnd();

TraceLogger.WriteActionStart("Sorting files...");
var sorted = res.OrderBy(static kvp => kvp.Key).ToList();

TraceLogger.WriteActionEnd();

return sorted;
}

// Write single entry

private static void WriteEntry(ChunkedMemoryStream writer, uint fileOffset, uint fileSize, uint id)
{
writer.WriteUInt32(fileOffset);
writer.WriteUInt32(fileSize);
writer.WriteUInt32(id);
}

// Add Res to Buffer

private static void AddRes(string filePath, ChunkedMemoryStream parent, out long dataPos, out uint size)
{
dataPos = parent.Position;

using var resStream = FileManager.OpenRead(filePath);
size = (uint)resStream.Length;

FileManager.Process(resStream, parent);

int alignment = ArcvConstants.FILE_ALIGMENT;

if(parent.Position % alignment != 0)
parent.Align(alignment, ArcvConstants.PADDING_BYTE);

}

// Add files

private static void AddFiles(List<KeyValuePair<uint, string>> files, List<string> names,
                             ChunkedMemoryStream entries,
							 ChunkedMemoryStream parent,
                             Action<string, int, int> progressCallback)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

int entriesLen = fileCount * 12;
entries.SetLength(entriesLen);

using NativeMemoryOwner<long> dataRelPos = new(fileCount);
using NativeMemoryOwner<uint> sizes = new(fileCount);

// Build ResBlob

for(int i = 0; i < fileCount; i++)
{

if(progressCallback is not null && names.Count > 0)
progressCallback(names[i], i + 1, fileCount);

AddRes(files[i].Value, parent, out dataRelPos[i], out sizes[i]);
}

var dataBlobOffset = (uint)(entriesLen + 12);

// Write entries

for(int i = 0; i < fileCount; i++)
{
var fileOffset = (uint)(dataBlobOffset + dataRelPos[i]);

WriteEntry(entries, fileOffset, sizes[i], files[i].Key);
}

TraceLogger.WriteActionEnd();
}

// Write Header

private static void WriteHeader(Stream writer, int fileCount, uint totalSize)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(ArcvConstants.MAGIC, Endianness.BigEndian);
writer.WriteInt32(fileCount);
writer.WriteUInt32(totalSize);

TraceLogger.WriteActionEnd();
}

// Write ARCV Entries

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

// Compress Stream

public static void Compress(string sourceDir, Stream target,
                            Action<string, int, int> progressCallback = null)
{
TraceLogger.WriteLine("Step #1 - List Files:");
TraceLogger.WriteLine();

bool ignoreNames = progressCallback is null;
var files = GetFiles(sourceDir, ignoreNames, out var names);

int fileCount = files.Count;

if(fileCount == 0)
{
TraceLogger.WriteError($"'{sourceDir}' has no Files.");

return;
}

TraceLogger.WriteInfo($"Found: {fileCount} files");

TraceLogger.WriteLine("Step #2 - Add Files:");
TraceLogger.WriteLine();

using ChunkedMemoryStream entries = new();
using ChunkedMemoryStream resBlob = new();

AddFiles(files, names, entries, resBlob, progressCallback);

TraceLogger.WriteLine("Step #3 - Write Metadata:");
TraceLogger.WriteLine();

var totalSize = (uint)(entries.Length + resBlob.Length);
WriteHeader(target, fileCount, totalSize);

TraceLogger.WriteLine("Step #4 - Write Resource Entries:");
TraceLogger.WriteLine();

WriteEntries(target, entries);

TraceLogger.WriteLine("Step #5 - Pack Resources:");
TraceLogger.WriteLine();

WriteRes(target, resBlob);
}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile,
                        Action<string, int, int> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("ARC-V Build Started");

try
{
PathHelper.ChangeExtension(ref targetFile, ".arcv");

TraceLogger.WriteDebug($"{sourceDir} --> {targetFile}");

using var arcvStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, arcvStream, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Pack folder");
}

TraceLogger.WriteLine("ARC-V Build Finished");

var outSize = FileManager.GetFileSize(targetFile);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}