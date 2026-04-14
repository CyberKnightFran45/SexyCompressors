using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SexyCompressors.ResourceStreamGroup
{
// Helper functions for RGB

public static class RsgHelper
{
// Store Paths to Packets

private static readonly Dictionary<(string Dir, string File), string> _packetMap = new();

// Build Path for Package

public static string BuildFilePath(string baseDir, string groupName)
{
var key = (baseDir, groupName);

if(_packetMap.TryGetValue(key, out var cachedPath) )
return cachedPath;

string path = Path.Combine(baseDir, "Packets", groupName + ".rsg");
_packetMap[key] = path;

return path;
}

// Store Paths to RSG Info

private static readonly Dictionary<(string Dir, string File), string> _infoMap = new();

// Build Path for Info

public static string BuildInfoPath(string baseDir, string groupName)
{
var key = (baseDir, groupName);

if(_infoMap.TryGetValue(key, out var cachedPath) )
return cachedPath;

string path = Path.Combine(baseDir, "PacketInfo", groupName + ".json");
_infoMap[key] = path;

return path;
}

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

// Store Paths to GPU Info

private static readonly ConcurrentDictionary<(string Dir, string File), string> _ptxMap = new();

// Build Path for GPU Images (Part1)

public static string BuildGpuPath(string baseDir, string resName)
{
var key = (baseDir, resName);

if(_ptxMap.TryGetValue(key, out var cachedPath) )
return cachedPath;

string relative = Path.ChangeExtension(resName, ".json");
string path = Path.Combine(baseDir, "Metadata", "GPU", relative);

_ptxMap[key] = path;

return path;
}

// Calculate padding for RSG blocks.
// Files: padIfAligned = false (no padding added when already aligned)
// Internal RSG data: padIfAligned = true (force full block on alignment)

public static int ComputePadding(int length, bool padIfAligned)
{
const int FILE_ALIGNMENT = 4096;

int required = length % FILE_ALIGNMENT;

if(required == 0)
return padIfAligned ? FILE_ALIGNMENT : 0;

return FILE_ALIGNMENT - required;
}

}

}