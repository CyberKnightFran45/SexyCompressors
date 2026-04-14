using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SexyCompressors.PopCapPackage
{
// Provides useful Tasks for PAK Files

public static class PakHelper
{
// Store Res Dirs

private static readonly Dictionary<string, string> _resDirs = new();

// Get Resource Dir

public static string GetResDir(string baseDir)
{

if(_resDirs.TryGetValue(baseDir, out var cachedDir) )
return cachedDir;

string dirName = Path.Combine(baseDir, "Resources");
_resDirs[baseDir] = dirName;

return dirName;
}

// Store Resource Paths

private static readonly ConcurrentDictionary<(string Dir, string File), string> _resPaths = new();

// Build Path for Resource

public static string BuildResourcePath(string baseDir, string resName)
{
var key = (baseDir, resName);

if(_resPaths.TryGetValue(key, out var cachedPath) )
return cachedPath;

string path = Path.Combine(baseDir, "Resources", resName);
_resPaths[key] = path;

return path;
}

// Get file alignment

private static int GetAlign(bool isPtx) => isPtx ? 4096 : 8;

// Check if Res is aligned

public static bool IsAligned(long pos, bool isPtx)
{
int align = GetAlign(isPtx);

return pos % align == 0;
}

// Align PAK Data

public static void AlignStream(Stream target, bool isPtx)
{
int align = GetAlign(isPtx);
long pos = target.Position + 2;

var misalignment = (int)(pos % align);
int padding = misalignment == 0 ? 0 : align - misalignment;

target.WriteUInt16( (ushort)padding);
target.Fill(padding);
}

}

}