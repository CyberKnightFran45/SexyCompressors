using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SexyCompressors.XboxPackedResource
{
/// <summary> Unpacks the Content of a Xbox Package (XPR). </summary>

public static class XprUnpacker
{
// Read XPR Entries

private static List<XResInfo> ReadEntries(Stream reader, int fileCount, out long totalBytes)
{
TraceLogger.WriteActionStart("Reading entries...");

List<XResInfo> entries = new(fileCount);
totalBytes = 0;

for(int i = 0; i < fileCount; i++)
{
var entry = XResInfo.Read(reader);
entries.Add(entry);

totalBytes += entry.FileSize;
}

TraceLogger.WriteActionEnd();

return entries;
}

// Read Paths as C-string

private static List<string> ReadPaths(Stream reader, int fileCount)
{
TraceLogger.WriteActionStart("Reading paths...");

List<string> resPaths = new(fileCount);

for(int i = 0; i < fileCount; i++)
{
using var pOwner = reader.ReadCString(XprConstants.ENCODING);

string basePath = new(pOwner.AsSpan() );
resPaths.Add(basePath);
}

TraceLogger.WriteActionEnd();

return resPaths;
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer, long offset, string filePath, int fileSize)
{
using var resStream = FileManager.OpenWrite(filePath);
var resData = buffer.AsSpan(offset, fileSize);

resStream.Write(resData);
}

// Extract res

private static void ExtractRes(string outputDir, NativeBuffer buffer, long dataStart,
                               string path, in XResInfo info)
{
var resOffset = info.FileOffset - dataStart;
var size = (int)Math.Min(info.FileSize, int.MaxValue);

string resPath = XprHelper.BuildResourcePath(outputDir, info.RootDir, path);

WriteRes(buffer, resOffset, resPath, size);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir, long dataStart, NativeBuffer buffer,
                                 List<XResInfo> entries, List<string> paths)
{

for(int i = 0; i < entries.Count; i++)
ExtractRes(outputDir, buffer, dataStart, paths[i], entries[i]);

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                           List<XResInfo> entries, List<string> paths)
{
var indexList = Enumerable.Range(0, entries.Count);
var batches = BatchHelper.Batch(indexList, entries.Count, buffer.Size);

foreach(var batch in batches)
{

Parallel.ForEach(batch, idx =>
{
ExtractRes(outputDir, buffer, dataStart, paths[idx], entries[idx]);
} );

}

}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                     List<XResInfo> entries, List<string> paths)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 64;

TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractFiles(outputDir, dataStart, buffer, entries, paths);

else
ExtractFilesInParallel(outputDir, dataStart, buffer, entries, paths);

TraceLogger.WriteActionEnd();
}

// Decompress Stream

public static void Decompress(Stream source, string outputDir)
{
const string ERROR_INVALID_FLAGS = "Invalid identifier: {0:X8}, expected: 'XPR2'";

TraceLogger.WriteLine("Step #1 - Read Metadata:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Reading header...");

uint flags = source.ReadUInt32(Endianness.BigEndian);

if(flags != XprConstants.MAGIC)
{
TraceLogger.WriteError(string.Format(ERROR_INVALID_FLAGS, flags) );

return;
}

var info = XprInfo.Read(source);
TraceLogger.WriteActionEnd();

var fileCount = (int)info.FileCount;

TraceLogger.WriteInfo($"Resources embedded: {fileCount}");

TraceLogger.WriteLine($"Step #2 - Read Resource Entries:");
TraceLogger.WriteLine();

var entries = ReadEntries(source, fileCount, out var totalBytes);

TraceLogger.WriteLine("Step #3 - Read Paths Table:");
TraceLogger.WriteLine();

var pathsTable = ReadPaths(source, fileCount);

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes);

TraceLogger.WriteLine($"Step #4 - Extract Resources:");
TraceLogger.WriteLine();

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

ExtractFilesCore(outputDir, dataStart, resBlob, entries, pathsTable);
}

public static void Unpack(string inputFile, string outputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("XPR Unpacking Started");

try
{
TraceLogger.WriteDebug($"{inputFile} --> {outputDir}");

using var xprStream = FileManager.OpenRead(inputFile);
Decompress(xprStream, outputDir);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Unpack file");
}

TraceLogger.WriteLine("XPR Unpacking Finished");
}

}

}