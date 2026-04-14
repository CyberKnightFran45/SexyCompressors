using System;
using System.Diagnostics;
using System.IO;
using BlossomLib.Modules.Security;

namespace SexyCompressors.PopCapZLib
{
/// <summary> Allows the Creation of Tags for SMF Files. </summary>

public static class SmfTagCreator
{
// Compute hash

private static string ComputeHash(Stream sourceStream)
{
using var hOwner = GenericDigest.GetString(sourceStream, "MD5", StringCase.Upper);
string hash = new(hOwner.AsSpan() );

return hash + "\x0D\x0A";
}

/** <summary> Saves the SMF Tag from the given RSB Stream. </summary>

<param name = "sourceStream"> The Stream from which the Tag will be Created. </param>
<param name = "targetPath"> The Path where to Save the SMF Tag. </param> */

public static void SaveTag(Stream sourceStream, string targetPath)
{
PathHelper.ChangeExtension(ref targetPath, ".tag.smf");

string tag = ComputeHash(sourceStream);

File.WriteAllText(targetPath, tag);
}

/** <summary> Generates a SMF Tag File in the Specfied Location. </summary>

<param name = "sourcePath"> The Path to the RSB file from which the Tag will be Created. </param>
<param name = "targetPath"> The Path where to Save the SMF Tag. </param> */

public static void CreateTag(string sourcePath, string targetPath)
{

try
{
TraceLogger.WriteActionStart("Generating smf tag...");

using var sourceStream = FileManager.OpenRead(sourcePath);
SaveTag(sourceStream, targetPath);

TraceLogger.WriteActionEnd();
}

catch(Exception error)
{
TraceLogger.WriteError(error, "Failed to Create tag");
}

}

}

}
