using System;
using System.Collections.Generic;

// Creates Batch of Tasks

public static class BatchHelper
{
// Calculate optimal batch size

private static int ComputeBatchSize(int fileCount, ulong totalBytes)
{
const int MAX_FILES_PER_BATCH = 1000;
const long MAX_BYTES_PER_BATCH = SizeT.ONE_MEGABYTE * 500;

int batchSize = Math.Min(fileCount, MAX_FILES_PER_BATCH);

if(totalBytes > MAX_BYTES_PER_BATCH)
{
var approxBatches = (int)Math.Ceiling( (double)totalBytes / MAX_BYTES_PER_BATCH);

batchSize = Math.Max(1, fileCount / approxBatches);
}

return batchSize;
}

// Create batches

public static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> source, int batchSize)
{
List<T> batch = new(batchSize);

foreach(var item in source)
{
batch.Add(item);

if(batch.Count >= batchSize)
{
yield return batch; 

batch = new(batchSize);
}

}

if(batch.Count > 0)
yield return batch;

}

// Create batches (optimal)

public static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> source, int fileCount, ulong totalBytes)
{
int batchSize = ComputeBatchSize(fileCount, totalBytes);

return Batch(source, batchSize);
}

}