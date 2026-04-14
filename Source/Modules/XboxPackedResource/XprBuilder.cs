using System;
using System.Collections.Generic;
using System.IO;

namespace SexyCompressors.XboxPackedResource
{
/// <summary> Packs the Content of a Directory as a Xbox Package (XPR). </summary>

public static class XprBuilder
{
// Split path into Root and Name

private static void SplitPath(string path, out uint dirFlags, out string resName)
{
string[] parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

string rootDir = parts.Length > 1 ? parts[0] : string.Empty;
dirFlags = String32.ToInt(rootDir);

if(parts.Length > 1)
resName = string.Join(Path.DirectorySeparatorChar, parts.AsSpan(1).ToArray());

else
resName = parts[0];

resName = resName.Replace('\\', '/');
}
	
// Get files

private static List<string> GetFiles(string baseDir, out List<string> names, out List<uint> dirs)
{
names = new();
dirs = new();

TraceLogger.WriteActionStart("Obtaining files...");

string resDir = XprHelper.GetResDir(baseDir);
var files = Directory.EnumerateFiles(resDir, "*.*", SearchOption.AllDirectories);

List<string> res = new();

foreach(string path in files)
{
res.Add(path);

string resPath = Path.GetRelativePath(resDir, path);
SplitPath(resPath, out var root, out var name);

dirs.Add(root);
names.Add(name);
}

TraceLogger.WriteActionEnd();

return res;
}

// Write single entry

private static void WriteEntry(ChunkedMemoryStream writer, uint rootDir, uint fileOffset,
                               uint fileSize, uint pathOffset)
{
writer.WriteUInt32(rootDir, Endianness.BigEndian);
writer.WriteUInt32(fileOffset, Endianness.BigEndian);
writer.WriteUInt32(fileSize, Endianness.BigEndian);
writer.WriteUInt32(pathOffset, Endianness.BigEndian);
}

// Add Res to Buffer

private static void AddRes(string filePath, string name,
                           ChunkedMemoryStream paths,
                           ChunkedMemoryStream parent,
                           out long pathPos, out long dataPos, out uint size)
{
pathPos = paths.Position;
dataPos = parent.Position;

using var resStream = FileManager.OpenRead(filePath);
size = (uint)resStream.Length;

paths.WriteCString(name, XprConstants.ENCODING);
FileManager.Process(resStream, parent);

int alignment = XprConstants.FILE_ALIGMENT;

if(parent.Position % alignment != 0)
parent.Align(alignment);

}

// Add files

private static void AddFiles(List<string> files, List<string> names, List<uint> dirs,
                             ChunkedMemoryStream entries,
							 ChunkedMemoryStream paths,
							 ChunkedMemoryStream parent,
                             Action<string, int, int> progressCallback)
{
TraceLogger.WriteActionStart("Adding files...");

int fileCount = files.Count;

int entriesLen = fileCount * 16;
entries.SetLength(entriesLen);

using NativeMemoryOwner<long> pathRelPos = new(fileCount);
using NativeMemoryOwner<long> dataRelPos = new(fileCount);
using NativeMemoryOwner<uint> sizes = new(fileCount);

// Build PathTable and ResBlob

for(int i = 0; i < fileCount; i++)
{
string name = names[i];
progressCallback?.Invoke(name, i + 1, fileCount);

AddRes(files[i], name, paths, parent, out pathRelPos[i], out dataRelPos[i], out sizes[i]);
}

// Calculate offsets

const uint HEADER_LENGTH = 16;

var pathTableOffset = (uint)(HEADER_LENGTH + entriesLen);
var dataBlobOffset = (uint)(pathTableOffset + paths.Position);

// Write entries

for(int i = 0; i < fileCount; i++)
{
var pathOffset = (uint)(pathTableOffset + pathRelPos[i]);
var fileOffset = (uint)(dataBlobOffset + dataRelPos[i]);

WriteEntry(entries, dirs[i], fileOffset, sizes[i], pathOffset);
}

TraceLogger.WriteActionEnd();
}

// Write Header

private static void WriteHeader(Stream writer, uint totalSize, int fileCount)
{
TraceLogger.WriteActionStart("Writing header...");

writer.WriteUInt32(XprConstants.MAGIC, Endianness.BigEndian);
writer.WriteUInt32(totalSize, Endianness.BigEndian);
writer.WriteUInt32(0); // <GPUDataSize> (not used)
writer.WriteInt32(fileCount, Endianness.BigEndian);

TraceLogger.WriteActionEnd();
}

// Write XPR Entries

private static void WriteEntries(Stream writer, ChunkedMemoryStream entries)
{
TraceLogger.WriteActionStart("Writing entries...");

entries.Seek(0, SeekOrigin.Begin);
FileManager.Process(entries, writer);

TraceLogger.WriteActionEnd();
}

// Write PathTable

private static void WritePaths(Stream writer, ChunkedMemoryStream paths)
{
TraceLogger.WriteActionStart("Writing paths...");

paths.Seek(0, SeekOrigin.Begin);
FileManager.Process(paths, writer);

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

// Align stream

private static void AlignStream(Stream target)
{
TraceLogger.WriteActionStart("Aligning stream...");
target.Align(2048);

TraceLogger.WriteActionEnd();
}

// Compress Stream

public static void Compress(string sourceDir, Stream target,
                            Action<string, int, int> progressCallback = null)
{
TraceLogger.WriteLine("Step #1 - List Files:");
TraceLogger.WriteLine();

var files = GetFiles(sourceDir, out var names, out var dirs);
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
using ChunkedMemoryStream pathsTable = new();
using ChunkedMemoryStream resBlob = new();

AddFiles(files, names, dirs, entries, pathsTable, resBlob, progressCallback);

TraceLogger.WriteLine("Step #3 - Write Metadata:");
TraceLogger.WriteLine();

var totalSize = (uint)(entries.Length + pathsTable.Length + resBlob.Length);
WriteHeader(target, totalSize, fileCount);

TraceLogger.WriteLine("Step #4 - Write Resource Entries:");
TraceLogger.WriteLine();

WriteEntries(target, entries);

TraceLogger.WriteLine("Step #5 - Write Paths Table:");
TraceLogger.WriteLine();

WritePaths(target, pathsTable);

TraceLogger.WriteLine("Step #6 - Pack Resources:");
TraceLogger.WriteLine();

WriteRes(target, resBlob);

TraceLogger.WriteLine("Step #7 - Align Stream:");
TraceLogger.WriteLine();

AlignStream(target);
}

// Pack Dir as single stream

public static void Pack(string sourceDir, string targetFile,
                        Action<string, int, int> progressCallback = null)
{
TraceLogger.Init();
TraceLogger.WriteLine("XPR Build Started");

try
{
PathHelper.ChangeExtension(ref targetFile, ".xpr");

TraceLogger.WriteDebug($"{sourceDir} --> {targetFile}");

using var xprStream = FileManager.OpenWrite(targetFile);
Compress(sourceDir, xprStream, progressCallback);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Pack folder");
}

TraceLogger.WriteLine("XPR Build Finished");

var outSize = FileManager.GetFileSize(targetFile);
TraceLogger.WriteInfo($"Output Size: {SizeT.FormatSize(outSize)}", false);
}

}

}