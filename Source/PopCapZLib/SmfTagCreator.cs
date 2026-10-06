using System;
using System.IO;
using BlossomLib.Modules.Security;

namespace SexyCompressors.PopCapZLib
{
/// <summary> Allows the Creation of Tags for SMF Files. </summary>

public static class SmfTagCreator
{
// Compute hash

public static string ComputeHash(Stream source)
{
using var hOwner = GenericDigest.GetString(source, "MD5", StringCase.Upper);
string hash = new(hOwner.AsSpan() );

return hash + "\x0D\x0A";
}

// Hash stream

private static void HashStream(Stream input, Stream output, TraceContext ctx)
{
string tag = ComputeHash(input);

output.WriteString(tag);
}

// Create tag internal

private static void CreateTagInternal(TraceContext ctx, string srcPath, string targetPath)
{
PathHelper.ChangeExtension(ref targetPath, ".tag.smf");

TraceFileSteps.Run(ctx, srcPath, targetPath, "Generating smf tag...", HashStream, true, false);
}

/** <summary> Generates a SMF Tag File in the Specfied Location. </summary>

<param name = "sourcePath"> The Path to the RSB file from which the Tag will be Created. </param>
<param name = "targetPath"> The Path where to Save the SMF Tag. </param> */

public static void CreateTag(string sourcePath, string targetPath)
{
	
TraceExecutor.Run("SMF Tag Generation", 
                  ctx => CreateTagInternal(ctx, sourcePath, targetPath),
                  ("SourcePath", sourcePath),
                  ("TargetPath", targetPath)
);

}

}

}