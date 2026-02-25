using System;
using System.Runtime.InteropServices;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// Provides P/Invoke access to the Linux <c>utimensat(2)</c> syscall for setting file timestamps.
/// </summary>
/// <remarks>
/// <para>
/// .NET's <see cref="System.IO.FileInfo.LastAccessTime"/> setter calls <c>utimensat(2)</c> with both
/// atime and mtime, preserving the cached mtime from the <see cref="System.IO.FileInfo"/> instance.
/// This causes an inotify <c>IN_ATTRIB</c> event to be raised for the mtime field (even though its
/// value does not change), which .NET's <see cref="System.IO.FileSystemWatcher"/> surfaces as a
/// <c>Changed</c> event under <see cref="System.IO.NotifyFilters.LastWrite"/>.
/// </para>
/// <para>
/// This class calls <c>utimensat(2)</c> directly using <c>UTIME_NOW</c> for atime and <c>UTIME_OMIT</c>
/// for mtime, so that only atime is updated and no spurious mtime inotify event is raised.
/// </para>
/// </remarks>
internal static class UtimensatInterop
{
    private const int AT_FDCWD = -100;
    private const long UTIME_NOW = 0x3FFFFFFF;
    private const long UTIME_OMIT = 0x3FFFFFFE;

    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec
    {
        public long tv_sec;
        public long tv_nsec;
    }

    [DllImport("libc", EntryPoint = "utimensat", SetLastError = true)]
    private static extern int utimensat(int dirfd, string pathname, Timespec[] times, int flags);

    /// <summary>
    /// Sets only the access time (atime) of a file to the current time, leaving mtime unchanged.
    /// Falls back to <see cref="System.IO.File.SetLastAccessTimeUtc"/> on non-Linux platforms.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    public static void SetAccessTimeOnly(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            global::System.IO.File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
            return;
        }

        var times = new Timespec[2];
        times[0] = new Timespec { tv_sec = 0, tv_nsec = UTIME_NOW };  // atime = now
        times[1] = new Timespec { tv_sec = 0, tv_nsec = UTIME_OMIT }; // mtime = leave unchanged

        utimensat(AT_FDCWD, path, times, 0);
        // Errors are silently ignored — atime update is best-effort and non-critical.
    }
}
