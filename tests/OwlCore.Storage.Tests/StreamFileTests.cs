using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests
{
    [TestClass]
    public class StreamFileTests : CommonIFileTests
    {
        // Required for base class to perform common tests.
        public override async Task<IFile> CreateFileAsync()
        {
            var randomData = GenerateRandomData(256_000);
            using var tempStr = new MemoryStream(randomData);

            var memoryStream = new MemoryStream();
            await tempStr.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new StreamFile(memoryStream);

            static byte[] GenerateRandomData(int length)
            {
                var rand = new Random();
                var b = new byte[length];
                rand.NextBytes(b);

                return b;
            }
        }

        [TestMethod]
        public void ShouldDispose_DefaultValue_IsFalse()
        {
            // Arrange
            var memoryStream = new MemoryStream();

            // Act
            var streamFile = new StreamFile(memoryStream);

            // Assert
            Assert.IsFalse(streamFile.ShouldDispose);
        }

        [TestMethod]
        public void ShouldDispose_Constructor_SetsCorrectValue()
        {
            // Arrange
            var memoryStream = new MemoryStream();

            // Act
            var streamFileTrue = new StreamFile(memoryStream, true);
            var streamFileFalse = new StreamFile(memoryStream, false);

            // Assert
            Assert.IsTrue(streamFileTrue.ShouldDispose);
            Assert.IsFalse(streamFileFalse.ShouldDispose);
        }

        [TestMethod]
        public void ShouldDispose_ConstructorWithIdAndName_SetsCorrectValue()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var id = "test-id";
            var name = "test-name";

            // Act
            var streamFileTrue = new StreamFile(memoryStream, id, name, true);
            var streamFileFalse = new StreamFile(memoryStream, id, name, false);

            // Assert
            Assert.IsTrue(streamFileTrue.ShouldDispose);
            Assert.IsFalse(streamFileFalse.ShouldDispose);
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeTrue_ReturnsUnderlyingStream()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamFile = new StreamFile(memoryStream, true);

            // Act
            var resultStream = await streamFile.OpenStreamAsync();

            // Assert
            Assert.AreSame(memoryStream, resultStream);
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeFalse_ReturnsWrappedStream()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamFile = new StreamFile(memoryStream, false);

            // Act
            var resultStream = await streamFile.OpenStreamAsync();

            // Assert
            Assert.AreNotSame(memoryStream, resultStream);
            Assert.AreEqual(typeof(OwlCore.Storage.Memory.NonDisposableStreamWrapper), resultStream.GetType());
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeTrue_DisposingReturnedStreamDisposesUnderlying()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamFile = new StreamFile(memoryStream, true);

            // Act
            var resultStream = await streamFile.OpenStreamAsync();
            resultStream.Dispose();

            // Assert
            Assert.ThrowsException<ObjectDisposedException>(() => memoryStream.ReadByte());
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeFalse_DisposingReturnedStreamDoesNotDisposeUnderlying()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3, 4, 5 };
            var memoryStream = new MemoryStream(data);
            var streamFile = new StreamFile(memoryStream, false);

            // Act
            var resultStream = await streamFile.OpenStreamAsync();
            resultStream.Dispose();

            // Assert - underlying stream should still be usable
            memoryStream.Position = 0;
            Assert.AreEqual(1, memoryStream.ReadByte());
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeTrue_MultipleOpensReturnSameStream()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamFile = new StreamFile(memoryStream, true);

            // Act
            var stream1 = await streamFile.OpenStreamAsync();
            var stream2 = await streamFile.OpenStreamAsync();

            // Assert
            Assert.AreSame(stream1, stream2);
            Assert.AreSame(memoryStream, stream1);
        }

        [TestMethod]
        public async Task OpenStreamAsync_ShouldDisposeFalse_MultipleOpensReturnDifferentWrappers()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamFile = new StreamFile(memoryStream, false);

            // Act
            var stream1 = await streamFile.OpenStreamAsync();
            var stream2 = await streamFile.OpenStreamAsync();

            // Assert
            Assert.AreNotSame(stream1, stream2);
            Assert.AreEqual(typeof(OwlCore.Storage.Memory.NonDisposableStreamWrapper), stream1.GetType());
            Assert.AreEqual(typeof(OwlCore.Storage.Memory.NonDisposableStreamWrapper), stream2.GetType());
        }
    }
}