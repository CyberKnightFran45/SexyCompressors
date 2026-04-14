using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

// Helper used for calling imdisk.exe

public static class ImDiskHelper
{
// Path to Program

private static string _programPath => GetProgramPath();

// Get Program Path

private static string GetProgramPath()
{
var sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

return Path.Combine(sys32, "imdisk.exe");
}

// Check if ImDisk is installed

public static bool IsInstalled => File.Exists(_programPath);

// Check if Drive exists

private static bool DriveExists(char letter)
{
string root = $"{letter}:\\";
var drives = Directory.GetLogicalDrives();

return drives.Any(d => string.Equals(d, root, StringComparison.OrdinalIgnoreCase) );
}

// Format RAM Disk

private static void FormatDisk(char driveLetter, string fileSystem, string label)
{
string args = $"/c format {driveLetter}: /fs:{fileSystem} /v:{label} /q /y";

using var process = ProcessHelper.CreateNew("cmd.exe", args, false, true); 
process.StartInfo.Verb = "runas"; // Disk format requires admin privileges

process.Start();
process.WaitForExit();
}

// Expand RAM Disk

public static void ExpandDisk(char driveLetter, int newSizeMB, string fileSystem, string label = "")
{
Dictionary<string, NativeMemoryOwner<byte>> backup = new();

string driveName = $"{driveLetter}:\\";
var files = Directory.GetFiles(driveName, "*.*", SearchOption.AllDirectories);

foreach(var file in files)
{
var relativePath = file[3..];

using var reader = FileManager.OpenRead(file);
var owner = reader.ReadPtr();

backup[relativePath] = owner;
}

RemoveRamDisk(driveLetter);
Thread.Sleep(1000); // Wait until Disk is removed (VERY IMPORTANT)

CreateRamDisk(newSizeMB, driveLetter, fileSystem, label);

foreach(var kvp in backup)
{
var fullPath = Path.Combine(driveName, kvp.Key);
using var writer = FileManager.OpenWrite(fullPath);

var owner = kvp.Value;
writer.Write(owner.AsSpan() );

owner.Dispose();
}

}

// Create and format RAM Disk

public static void CreateRamDisk(int sizeInMB, char driveLetter, string fileSystem, string label = "")
{

if(DriveExists(driveLetter) )
{
DriveInfo drive = new($"{driveLetter}");

long driveSize = drive.TotalSize / SizeT.ONE_MEGABYTE;
long freeSpace = drive.TotalFreeSpace / SizeT.ONE_MEGABYTE;

if(freeSpace <= sizeInMB)
{
int newSizeMB = (int)(driveSize + (sizeInMB * 2) );

ExpandDisk(driveLetter, newSizeMB, fileSystem, label);
}

return;
}

string createArgs = $"-a -s {sizeInMB}M -m {driveLetter}:";

using var diskCreator = ProcessHelper.StartNew(_programPath, createArgs);
diskCreator.WaitForExit();

FormatDisk(driveLetter, fileSystem, label);
}

// Remove RAM Disk

public static void RemoveRamDisk(char driveLetter)
{
string removeArgs = $"-D -m {driveLetter}:";

using var diskRmv = ProcessHelper.StartNew(_programPath, removeArgs);
diskRmv.WaitForExit();
}

}