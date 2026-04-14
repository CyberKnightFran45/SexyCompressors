using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SexyCompressors.XboxPackedResource
{
// Provides useful Tasks for XPR Files

public static class XprHelper
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

public static string BuildResourcePath(string baseDir, uint root, string resName)
{
string rootDir = String32.FromInt(root);
string outDir = PathHelper.SafeCombine(baseDir, "Resources", rootDir);

var key = (outDir, resName);

if(_resPaths.TryGetValue(key, out var cachedPath) )
return cachedPath;

string path = PathHelper.SafeCombine(outDir, resName);
_resPaths[key] = path;

return path;
}

}

}