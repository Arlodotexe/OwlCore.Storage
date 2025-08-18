using System;
using System.IO;
using System.Linq;
using OwlCore.Storage;

namespace OwlCore.Storage.Tests;

[TestClass]
public class TruncatedStreamTests
{
    [TestMethod]
    public void Read_Respects_MaxLength_And_Stops()
    {
        // Arrange: 100 bytes of data, but limit reads to 50
        var data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        using var ms = new MemoryStream(data);
        using var ts = new TruncatedStream(ms, MaxLength: 50);

        var buffer = new byte[100];

        // Act
        var read1 = ts.Read(buffer, 0, buffer.Length);
        var read2 = ts.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.AreEqual(50, read1, "First read should be capped at MaxLength");
        Assert.AreEqual(0, read2, "Subsequent reads past MaxLength should return 0");

        // Also verify the content matches the first 50 bytes
        CollectionAssert.AreEqual(data.Take(50).ToArray(), buffer.Take(50).ToArray());
    }

    [TestMethod]
    public void Seek_Back_To_Beginning_Should_Reset_Window_For_Reads()
    {
        // Demonstrates the current bug: internal _position doesn't track underlying seek.
        // Expected behavior: After seeking back to 0, you can read up to MaxLength bytes again.
        var data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        using var ms = new MemoryStream(data);
        using var ts = new TruncatedStream(ms, MaxLength: 50);

        var buffer = new byte[100];

        // Consume part of the window
        var preRead = ts.Read(buffer, 0, 10);
        Assert.AreEqual(10, preRead);

        // Seek back to the start of the stream
        ts.Seek(0, SeekOrigin.Begin);

        // Try to read a full window again
        var readAfterSeek = ts.Read(buffer, 0, 50);

        // Expected: should be able to read 50 bytes again from the beginning
        // Current implementation likely returns 40 (50 - 10 consumed) due to independent _position tracking.
        Assert.AreEqual(50, readAfterSeek, "After seeking to beginning, the truncation window should allow MaxLength bytes again.");

        CollectionAssert.AreEqual(data.Take(50).ToArray(), buffer.Take(50).ToArray());
    }

    [TestMethod]
    public void Seek_Current_Beyond_Max_Throws()
    {
        var data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        using var ms = new MemoryStream(data);
        using var ts = new TruncatedStream(ms, MaxLength: 50);

        // Move to local position 10
        ts.Position = 10;

        // Seeking by +40 lands exactly at 50, which should be allowed
        var posAtMax = ts.Seek(40, SeekOrigin.Current);
        Assert.AreEqual(50, posAtMax);

        // Seeking any further should throw
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ts.Seek(1, SeekOrigin.Current));
    }

    [TestMethod]
    public void Seek_End_Bounds_And_Reads_Remaining()
    {
        var data = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        using var ms = new MemoryStream(data);
        using var ts = new TruncatedStream(ms, MaxLength: 50);

        // End is MaxLength; seeking beyond should throw
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ts.Seek(1, SeekOrigin.End));

        // Seek to end exactly; reads should return 0
        var atEnd = ts.Seek(0, SeekOrigin.End);
        Assert.AreEqual(50, atEnd);
        var buf = new byte[10];
        var readAtEnd = ts.Read(buf, 0, buf.Length);
        Assert.AreEqual(0, readAtEnd);

        // Seek to 10 bytes before end and validate remaining
        var beforeEnd = ts.Seek(-10, SeekOrigin.End);
        Assert.AreEqual(40, beforeEnd);
        var read10 = ts.Read(buf, 0, buf.Length);
        Assert.AreEqual(10, read10);
        CollectionAssert.AreEqual(data.Skip(40).Take(10).ToArray(), buf);
    }
}
