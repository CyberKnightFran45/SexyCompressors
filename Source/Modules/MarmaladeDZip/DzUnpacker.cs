using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SexyCompressors.MarmaladeDZip
{
/// <summary> Unpacks the Content of a DZip file. </summary>

public static class DzUnpacker
{
// Read File Names as C-string

private static List<string> ReadFileNames(Stream reader, int fileCount)
{
TraceLogger.WriteActionStart("Reading file names...");

List<string> resNames = new(fileCount);

for(int i = 0; i < fileCount; i++)
{
using var nOwner = reader.ReadCString(DzConstants.ENCODING);
string name = nOwner;

resNames.Add(name);
}

TraceLogger.WriteActionEnd();

return resNames;
}

// Read Dir Names as C-string

private static List<string> ReadDirNames(Stream reader, int dirCount)
{
TraceLogger.WriteActionStart("Reading dir names...");

List<string> dirNames = new(dirCount);
dirNames.Add(string.Empty); // Root

for(int i = 1; i < dirCount; i++)
{
using var dOwner = reader.ReadCString(DzConstants.ENCODING);
string name = dOwner;

dirNames.Add(name);
}

TraceLogger.WriteActionEnd();

return dirNames;
}

// Read Chunks

private static List<List<ChunkInfo>> ReadChunks(Stream reader, int count)
{
TraceLogger.WriteActionStart("Reading chunks...");

List<List<ChunkInfo>> chunks = new(count);

for(int i = 0; i < count; i++)
{

}

TraceLogger.WriteActionEnd();

return chunks;
}

// Read Chunk Names as C-string

private static List<string> ReadChunkNames(Stream reader, int chunks)
{
TraceLogger.WriteActionStart("Reading chunk names...");

List<string> chunkNames = new(chunks);
chunkNames.Add(null); // Root

for(int i = 1; i < chunks; i++)
{
using var cOwner = reader.ReadCString(DzConstants.ENCODING);
string name = cOwner;

chunkNames.Add(name);
}

TraceLogger.WriteActionEnd();

return chunkNames;
}


// Decompress Stream

public static void Decompress(Stream source, string outputDir)
{
const string ERROR_INVALID_FLAGS = "Invalid identifier: {0:X4}, expected: {1:X4}";

TraceLogger.WriteLine("Step #1 - Read Metadata:");
TraceLogger.WriteLine();

TraceLogger.WriteActionStart("Reading header...");

ushort flags = source.ReadUInt16(Endianness.BigEndian);
ushort expected = DzConstants.MAGIC;

if(flags != expected)
{
TraceLogger.WriteError(string.Format(ERROR_INVALID_FLAGS, flags, expected) );

return;
}

var info = DzInfo.Read(source);

TraceLogger.WriteActionEnd();

var fileCount = (int)info.FileCount;
var dirCount = (int)info.DirCount;

byte version = info.Version;
var expectedVer = DzConstants.VERSION;

if(version != expectedVer)
TraceLogger.WriteWarn($"Unknown version: v{version} - Expected: v{expectedVer}");

TraceLogger.WriteInfo($"Resources embedded: {fileCount}");

TraceLogger.WriteLine("Step #2 - Read Paths Table:");
TraceLogger.WriteLine();

var resNames = ReadFileNames(source, fileCount);
var dirNames = ReadDirNames(source, dirCount);

TraceLogger.WriteLine("Step #3 - Read Chunks:");
TraceLogger.WriteLine();

var pathsTable = ReadChunks(source, fileCount);

// RAMDisk.TryRedirect(ref outputDir, fileCount, totalBytes);

TraceLogger.WriteLine($"Step #4 - Extract Resources:");
TraceLogger.WriteLine();
}

public static void Unpack(string inputFile, string outputDir)
{
TraceLogger.Init();
TraceLogger.WriteLine("DZ Unpacking Started");

try
{
TraceLogger.WriteDebug($"{inputFile} --> {outputDir}");

using var dzStream = FileManager.OpenRead(inputFile);
Decompress(dzStream, outputDir);
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Unpack file");
}

TraceLogger.WriteLine("DZ Unpacking Finished");
}

}

}