namespace Newton.Core.Downloads;

public interface IDownloadService
{
    event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    ValueTask<DownloadResult> DownloadAsync(DownloadRequest request, CancellationToken cancellationToken = default);
    ValueTask CancelAsync(Guid downloadId);
}

public interface IFileScanner
{
    ValueTask<FileScanResult> ScanAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IDownloadHistory
{
    ValueTask RecordAsync(DownloadRecord record, CancellationToken cancellationToken = default);
    IAsyncEnumerable<DownloadRecord> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record DownloadRequest(Guid Id, Uri Source, string DestinationPath);
public sealed record DownloadResult(Guid Id, string FilePath, FileScanResult ScanResult);
public sealed record DownloadRecord(Guid Id, Uri Source, string FilePath, DateTimeOffset StartedUtc, DownloadStatus Status);
public sealed record FileScanResult(FileScanVerdict Verdict, string? Detail = null);
public sealed class DownloadProgressEventArgs(Guid id, long receivedBytes, long? totalBytes) : EventArgs
{
    public Guid Id { get; } = id;
    public long ReceivedBytes { get; } = receivedBytes;
    public long? TotalBytes { get; } = totalBytes;
}
public enum DownloadStatus { Pending, Downloading, Scanning, Complete, Blocked, Failed, Cancelled }
public enum FileScanVerdict { Unknown, Clean, Suspicious, Malicious }
