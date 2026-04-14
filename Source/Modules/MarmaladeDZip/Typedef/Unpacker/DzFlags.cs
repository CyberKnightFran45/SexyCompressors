namespace SexyCompressors.MarmaladeDZip
{
/// <summary> Special flags for DZip Compression </summary>

public enum DzFlags : ushort
{
/// <summary> Store data as is </summary>
Store = 1,

/// <summary> Inner DZip File </summary>
Dz = 4,

/// <summary> Compress data with ZLib </summary>
ZLib = 8,

/// <summary> Compress data with BZip2 </summary>
BZip2 = 16,

/// <summary> MP3 file </summary>
Mp3 = 32,

/// <summary> Jpeg file </summary>
Jpeg = 64,

/// <summary> File contains padding (Zeroes) </summary>
Padding = 128,
 
/// <summary> Data is readonly </summary>
ReadOnly = 256,
  
/// <summary> Compress data with Lzma </summary>
Lzma = 512,
  
/// <summary> RAM buffer </summary>
RandomAccess = 1024
}

}