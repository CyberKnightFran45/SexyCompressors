using System.IO;

// Allows writting files to a RAM Disk (by the moment only supports imdisk.exe)

public static class RAMDisk
{
// Compute Disk Size (NTFS)

private static int ComputeDiskSize(int fileCount, long totalBytes)
{
const long NTFS_OVERHEAD = SizeT.ONE_MEGABYTE * 16;
const long OVERHEAD_PER_FILE = SizeT.ONE_KILOBYTE * 4; 

long fileOverhead = fileCount * OVERHEAD_PER_FILE;
var extraMargin = (long)(totalBytes * 0.5);

long diskSize = totalBytes + NTFS_OVERHEAD + fileOverhead + extraMargin;

return (int)(diskSize / SizeT.ONE_MEGABYTE);
}

// Redirect output to RAM Disk if posible

public static void TryRedirect(ref string outDir, int fileCount, long totalBytes)
{
bool useRamDisk = PlatformHelper.IsWindows && ImDiskHelper.IsInstalled;

if(!useRamDisk)
return;

string originalOut = outDir;

try
{
int diskSizeMB = ComputeDiskSize(fileCount, totalBytes);
ImDiskHelper.CreateRamDisk(diskSizeMB, 'R', "NTFS", "RamDisk");

string oldRoot = Path.GetPathRoot(outDir)?.ToUpperInvariant();
string fixedOut = outDir.ToUpperInvariant();

if(oldRoot != null && fixedOut.StartsWith(oldRoot) )
{
outDir = "R:" + outDir[oldRoot.Length..];

PathHelper.CheckDuplicatedPath(ref outDir);
}

}

catch
{
outDir = originalOut;
}

}

}