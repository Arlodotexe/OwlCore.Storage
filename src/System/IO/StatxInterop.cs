using System;
using System.Runtime.InteropServices;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// Provides P/Invoke access to the Linux <c>statx(2)</c> syscall for reading file birth time (btime).
/// </summary>
/// <remarks>
/// <para>
/// .NET's native PAL uses <c>stat()</c>/<c>lstat()</c> on Linux, which does not expose birth time.
/// <c>File.GetCreationTimeUtc</c> falls back to <c>Min(mtime, ctime)</c> — a fabricated value.
/// This class calls <c>statx(2)</c> directly to retrieve the real birth time when the filesystem supports it.
/// </para>
/// <para>
/// <c>statx(2)</c> requires Linux kernel ≥ 4.11. If the syscall is unavailable or the filesystem
/// does not support birth time, the methods return <c>null</c>.
/// </para>
/// </remarks>
internal static class StatxInterop
{
    private const int AT_FDCWD = -100;
    private const int AT_STATX_SYNC_AS_STAT = 0x0000;
    private const uint STATX_BTIME = 0x00000800u;

    // Offsets within struct statx (from kernel UAPI header linux/stat.h):
    // stx_mask  at 0x00 (4 bytes, __u32)
    // stx_btime at 0x50 (16 bytes, struct statx_timestamp: tv_sec __s64 + tv_nsec __u32 + __reserved __s32)
    private const int STATX_SIZE = 256;
    private const int OFFSET_STX_MASK = 0;
    private const int OFFSET_STX_BTIME_SEC = 0x50;

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int statx(int dirfd, string pathname, int flags, uint mask, byte[] statxbuf);

    /// <summary>
    /// Gets the birth time (btime) of a file via <c>statx(2)</c>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The birth time as <see cref="DateTime"/> (Local), or <c>null</c> if unavailable.</returns>
    public static DateTime? GetBirthTime(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;

        var buf = new byte[STATX_SIZE];

        int result = statx(AT_FDCWD, path, AT_STATX_SYNC_AS_STAT, STATX_BTIME, buf);
        if (result != 0)
            return null;

        uint mask = BitConverter.ToUInt32(buf, OFFSET_STX_MASK);
        if ((mask & STATX_BTIME) == 0)
            return null;

        long btimeSec = BitConverter.ToInt64(buf, OFFSET_STX_BTIME_SEC);
        return DateTimeOffset.FromUnixTimeSeconds(btimeSec).LocalDateTime;
    }

    /// <summary>
    /// Gets the birth time (btime) of a file via <c>statx(2)</c> as a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The birth time as <see cref="DateTimeOffset"/> (UTC, zero offset), or <c>null</c> if unavailable.</returns>
    public static DateTimeOffset? GetBirthTimeOffset(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;

        var buf = new byte[STATX_SIZE];

        int result = statx(AT_FDCWD, path, AT_STATX_SYNC_AS_STAT, STATX_BTIME, buf);
        if (result != 0)
            return null;

        uint mask = BitConverter.ToUInt32(buf, OFFSET_STX_MASK);
        if ((mask & STATX_BTIME) == 0)
            return null;

        long btimeSec = BitConverter.ToInt64(buf, OFFSET_STX_BTIME_SEC);
        return DateTimeOffset.FromUnixTimeSeconds(btimeSec);
    }
}
