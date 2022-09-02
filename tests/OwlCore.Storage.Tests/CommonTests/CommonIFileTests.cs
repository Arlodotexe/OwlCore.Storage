using OwlCore.Extensions;

namespace OwlCore.Storage.Tests.CommonTests
{
    public abstract class CommonIFileTests
    {
        /// <summary>
        /// Call the constructor using valid input parameters.
        /// </summary>
        public abstract Task<IFile> CreateFileAsync();
        
        [TestMethod]
        public Task ConstructorCall_ValidParameters()
        {
            // Shouldn't throw when constructor is called.
            return CreateFileAsync();
        }

        [TestMethod]
        public async Task IdNotNullOrWhiteSpace()
        {
            var file = await CreateFileAsync();

            Assert.IsFalse(string.IsNullOrWhiteSpace(file.Id));
        }
        
        [TestMethod]
        [AllEnumFlagCombinations(typeof(FileAccess))]
        public async Task OpenStreamAndTryEachAccessMode(FileAccess accessMode)
        {
            var file = await CreateFileAsync();

            if (accessMode == 0)
            {
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode));
                return;
            }

            await using var stream = await file.OpenStreamAsync(accessMode);

            if (accessMode.HasFlag(FileAccess.Read))
            {
                var bytes = await stream.ToBytesAsync();
                Assert.AreEqual(stream.Length, bytes.Length);
            }

            if (accessMode.HasFlag(FileAccess.Write))
            {
                stream.WriteByte(0);
            }
        }
        
        [TestMethod]
        [AllEnumFlagCombinations(typeof(FileAccess))]
        public async Task OpenStreamWithEachAccessModeAndCancelToken(FileAccess accessMode)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var file = await CreateFileAsync();

            if (accessMode == 0)
            {
                var task = Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode, cancellationTokenSource.Token));
                cancellationTokenSource.Cancel();

                await task;
                return;
            }

            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => file.OpenStreamAsync(accessMode, cancellationTokenSource.Token));
        }
    }

}