using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using BlossomLib.Modules.Security;

namespace SexyCompressors.ArcVPackage
{
// Useful Tasks for ARC-V Packages

public static class ArcvHelper
{
// Store Res Dirs

private static readonly Dictionary<string, string> _resDirs = new();

// Get Resource Dir

public static string GetResDir(string baseDir)
{

if(_resDirs.TryGetValue(baseDir, out var cachedDir) )
return cachedDir;

string dirName = Path.Combine(baseDir, "NDS");
_resDirs[baseDir] = dirName;

return dirName;
}

// Get ID from File Name

public static uint GetID(ReadOnlySpan<char> name)
{
var rawBytes = BinaryHelper.GetBytes(name, EncodingType.UTF8);

return Crc32.Compute(rawBytes);
}

// Get extension from Flags

private static string GetExtension(uint flags)
{

return flags switch
{
0x4E415243 => ".narc",
0x4E4D4152 => ".nmar",
0x4E4D4352 => ".nmcr",
0x4E534352 => ".nscr",
0x4E434552 => ".ncer",
0x4E434752 => ".ncgr",
0x4E434C52 => ".nclr",
0x4E414E52 => ".nanr",
0x4E465452 => ".nftr",
0x53444154 => ".sdat",
_ => ".dat"
};

}

// Store Resource Paths

private static readonly ConcurrentDictionary<(string Dir, uint ID), string> _resPaths = new();

// Build Resouce Path

public static string BuildResourcePath(string baseDir, uint id, uint flags)
{
var key = (baseDir, id);

if(_resPaths.TryGetValue(key, out var cachedPath) )
return cachedPath;

string fileNumber = id.ToString().PadLeft(10, '0');
string fileExt = GetExtension(flags);

string path = Path.Combine(baseDir, "NDS", fileNumber + fileExt);
_resPaths[key] = path;

return path;
}

}

}