using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace WinCleaner
{
    public sealed class DiskFileResult
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        public string FileSize { get; set; }
        public DateTime LastModifiedDate { get; set; }
        internal long SizeBytes { get; set; }
    }

    public static class DiskAnalyzer
    {
        private const long BytesPerMegabyte = 1024L * 1024L;

        public static Task<List<DiskFileResult>> ScanAsync(string rootPath, double thresholdMegabytes = 100, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("A drive path is required.", nameof(rootPath));
            if (double.IsNaN(thresholdMegabytes) || double.IsInfinity(thresholdMegabytes) || thresholdMegabytes < 0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMegabytes));

            var fullPath = System.IO.Path.GetFullPath(rootPath);
            if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException("The selected drive or directory does not exist.");

            var thresholdBytes = thresholdMegabytes * BytesPerMegabyte;
            return Task.Run(() =>
            {
                var results = new List<DiskFileResult>();
                var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ScanDirectory(fullPath, thresholdBytes, results, visitedDirectories, cancellationToken);
                results.Sort((left, right) => right.SizeBytes.CompareTo(left.SizeBytes));
                return results;
            }, cancellationToken);
        }

        private static void ScanDirectory(string directoryPath, double thresholdBytes, List<DiskFileResult> results, HashSet<string> visitedDirectories, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedPath;
            try
            {
                normalizedPath = System.IO.Path.GetFullPath(directoryPath);
                if (!visitedDirectories.Add(normalizedPath)) return;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
            {
                return;
            }

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(normalizedPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var file = new FileInfo(filePath);
                        var sizeBytes = file.Length;
                        if (sizeBytes > thresholdBytes)
                        {
                            results.Add(new DiskFileResult
                            {
                                FileName = file.Name,
                                Path = file.FullName,
                                FileSize = FormatFileSize(sizeBytes),
                                LastModifiedDate = file.LastWriteTime,
                                SizeBytes = sizeBytes
                            });
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                    {
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException || ex is SecurityException)
            {
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var childPath in Directory.EnumerateDirectories(normalizedPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if ((File.GetAttributes(childPath) & FileAttributes.ReparsePoint) != 0) continue;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException || ex is SecurityException)
                    {
                        continue;
                    }

                    ScanDirectory(childPath, thresholdBytes, results, visitedDirectories, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException || ex is SecurityException)
            {
            }
        }

        private static string FormatFileSize(long sizeBytes)
        {
            if (sizeBytes >= BytesPerMegabyte * 1024L)
                return (sizeBytes / (1024d * 1024d * 1024d)).ToString("N2", CultureInfo.CurrentCulture) + " GB";

            return (sizeBytes / (double)BytesPerMegabyte).ToString("N2", CultureInfo.CurrentCulture) + " MB";
        }
    }
}
