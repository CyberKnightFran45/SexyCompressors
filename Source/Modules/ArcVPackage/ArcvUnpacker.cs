using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SexyCompressors.ArcVPackage
{
/** <summary> Unpacks the Content of an ARC-V Package. </summary>

<remarks> This format is used in <c>Nintendo DS</c> ROMs (such as PvZ DS). </remarks> **/

public static class ArcvUnpacker
{
// Read Entries

private static List<ResInfo> ReadEntries(Stream reader, int fileCount, out long totalBytes)
{
List<ResInfo> entries = new(fileCount);
totalBytes = 0;

TraceLogger.WriteActionStart("Reading entries...");

for(int i = 0; i < fileCount; i++)
{
var entry = ResInfo.Read(reader);
entries.Add(entry);

totalBytes += entry.Size;
}

TraceLogger.WriteActionEnd();

return entries;
}

// Write Res Stream

private static void WriteRes(NativeBuffer buffer, long offset, string path, int size)
{
using var resStream = FileManager.OpenWrite(path);
var resData = buffer.AsSpan(offset, size);

resStream.Write(resData);
}

// Extract res

private static void ExtractRes(string outputDir, NativeBuffer buffer, long dataStart, in ResInfo info)
{
var resOffset = info.Offset - dataStart;
var size = (int)Math.Min(info.Size, int.MaxValue);

uint flags = buffer.GetUInt32(resOffset);
string resPath = ArcvHelper.BuildResourcePath(outputDir, info.ID, flags);

WriteRes(buffer, resOffset, resPath, size);
}

// Extract resources (single thread)

private static void ExtractFiles(string outputDir, long dataStart, NativeBuffer buffer,
                                 List<ResInfo> entries)
{

foreach(var entry in entries)
ExtractRes(outputDir, buffer, dataStart, entry);

}

// Extract resources (multi-thread)

private static void ExtractFilesInParallel(string outputDir, long dataStart, NativeBuffer buffer,
                                           List<ResInfo> entries)
{
var batches = BatchHelper.Batch(entries, entries.Count, buffer.Size);

foreach(var batch in batches)
Parallel.ForEach(batch, entry => ExtractRes(outputDir, buffer, dataStart, entry) ); 

}

// Extract resources (Core)

private static void ExtractFilesCore(string outputDir, long dataStart, NativeBuffer buffer,
                                     List<ResInfo> entries)
{
const int MAX_FILES_SINGLE_THREAD = 64;
const long MAX_BYTES_SINGLE_THREAD = SizeT.ONE_MEGABYTE * 64;

TraceLogger.WriteActionStart("Extracting resources...");

if(entries.Count <= MAX_FILES_SINGLE_THREAD && buffer.Size <= MAX_BYTES_SINGLE_THREAD)
ExtractFiles(outputDir, dataStart, buffer, entries);

else
ExtractFilesInParallel(outputDir, dataStart, buffer, entries);

TraceLogger.WriteActionEnd();
}

// Decompress Stream

public static void Decompress(Stream source, string outputDir)
{
const string ERROR_INVALID_FLAGS = "Invalid identifier: {0:X8}, expected: {1:X8}";

TraceLogger.WriteLine("Step #1 - Read Metadata:");
TraceLogger.WriteLine();

uint flags = source.ReadUInt32(Endianness.BigEndian);

if(flags != ArcvConstants.MAGIC)
{
TraceLogger.WriteError(string.Format(ERROR_INVALID_FLAGS, flags, ArcvConstants.MAGIC) );

return;
}

var info = ArcvInfo.Read(source);
TraceLogger.WriteActionEnd();

var fileCount = (int)info.FileCount;

TraceLogger.WriteInfo($"Resources embedded: {fileCount}");

TraceLogger.WriteLine($"Step #2 - Read Resource Entries:");
TraceLogger.WriteLine();

var entries = ReadEntries(source, fileCount, out var totalBytes);

RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes);

TraceLogger.WriteLine($"Step #3 - Extract Resources:");
TraceLogger.WriteLine();

long dataStart = source.Position;
using var resBlob = source.ReadPtr();

ExtractFilesCore(outputDir, dataStart, resBlob, entries);
}

// Unpack Stream as Dir

public static void Unpack(string inputFile, string outputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("ARC-V Extraction Started");

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

TraceLogger.WriteLine("ARC-V Extraction Finished");
}

}

}