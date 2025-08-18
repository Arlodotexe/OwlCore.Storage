# OwlCore.Storage

The most flexible file system abstraction for .NET, built in partnership with the UWP Community. This is a C# library project that provides interfaces and implementations for working with files and folders across different storage systems (Memory, System.IO, HTTP, Zip archives, etc.).

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Bootstrap, build, and test the repository:
- **CRITICAL: Install .NET 9 SDK first** - this project requires .NET 9:
  - `wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && chmod +x dotnet-install.sh`
  - `./dotnet-install.sh --channel 9.0`
  - `export PATH="/home/runner/.dotnet:$PATH"`
- **Restore dependencies**: `dotnet restore` -- takes ~1.2 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- **Build**: `dotnet build` -- takes ~2.5 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- **Test**: `dotnet test` -- takes ~3 seconds, runs 391 tests (2 may fail due to network connectivity). NEVER CANCEL. Set timeout to 60+ seconds.

### Code formatting and validation:
- **Format code**: `dotnet format` -- fixes whitespace and formatting issues automatically. Takes ~9 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- **Check formatting**: `dotnet format --verify-no-changes` -- validates code formatting (will show errors if formatting needed)
- **Build with warnings**: `dotnet build --verbosity minimal` -- shows the single expected warning about TruncatedStream.DisposeAsync()

### Expected build outputs:
- Build succeeds with 1 warning (TruncatedStream.DisposeAsync() method hiding inherited member - this is expected)
- Tests: 389 pass, 2 fail (HttpFile tests fail due to network connectivity - this is expected in sandboxed environments)
- Build artifacts: `src/bin/Debug/netstandard2.0/OwlCore.Storage.dll` and `src/bin/Debug/net9.0/OwlCore.Storage.dll`

## Validation

- **ALWAYS run build and tests after making changes** to ensure functionality works correctly
- **ALWAYS run `dotnet format`** before committing changes or the CI (.github/workflows/build.yml) will fail due to formatting issues
- **Manual validation scenarios**: After making changes, test file operations by creating small test programs using:
  - `new SystemFile("somefile.txt")` for local file system operations
  - `new MemoryFile(stream)` for in-memory file operations 
  - Basic operations like `OpenStreamAsync()`, `GetItemsAsync()`, `CreateFileAsync()`
- **Example validation test**: Create a console app that tests basic functionality:
  ```csharp
  var memoryFile = new MemoryFile("test", "test.txt", new MemoryStream());
  await memoryFile.WriteTextAsync("Hello World");
  var content = await memoryFile.ReadTextAsync(); // Should read back "Hello World"
  ```

## Project Architecture

### Key interfaces (located in src/):
- **IFile**: Basic file operations (`OpenStreamAsync`)
- **IFolder**: Basic folder operations (`GetItemsAsync`) 
- **IModifiableFolder**: File/folder creation and deletion
- **IStorable**: Base interface with `Id` and `Name` properties
- **IChildFile/IChildFolder**: Files/folders that have a parent
- **IStorableChild**: Items that can navigate to their parent

### Major implementations:
- **System.IO**: `SystemFile`, `SystemFolder` (src/System/IO/) - wraps System.IO File/Directory APIs
- **Memory**: `MemoryFile`, `MemoryFolder` (src/Memory/) - fully in-memory implementations 
- **HTTP**: `HttpFile` (src/System/Net/Http/) - read-only HTTP file access
- **Zip**: Archive support via System.IO.Compression (src/System/IO/Compression/)

### Test structure (tests/OwlCore.Storage.Tests/):
- **CommonTests**: Shared test logic via OwlCore.Storage.CommonTests NuGet package
- **Implementation-specific tests**: SystemIO/, Memory/, Archive/ folders
- **Feature tests**: CreateRelativeStorageExtensionsTests, FileRangeReadWriteTests, etc.

## Common Tasks

### Repository root structure:
```
.github/          # GitHub workflows and configuration
src/              # Main library source code  
  System/         # System.IO, HTTP, and Compression implementations
  Memory/         # In-memory implementations
  Extensions/     # Extension methods for interfaces
  *.cs            # Core interfaces (IFile, IFolder, etc.)
tests/            # MSTest unit tests
OwlCore.Storage.sln  # Visual Studio solution file
README.md         # Project documentation with proposal details
```

### Building for different targets:
- Project multi-targets `netstandard2.0` and `net9.0`
- Build creates separate assemblies for each target framework
- Always use `dotnet build` (not `msbuild` or Visual Studio) for reliable cross-platform builds

### Working with the abstractions:
- **Start with interfaces**: Always code against IFile/IFolder, not concrete implementations
- **Use extension methods**: Many operations (copy, move, relative paths) are provided as extensions in src/Extensions/
- **Handle async properly**: All operations are async - use proper cancellation tokens
- **Example basic usage**:
  ```csharp
  // File system access
  var file = new SystemFile("/path/to/file.txt");
  using var stream = await file.OpenStreamAsync(FileAccess.Read);
  
  // In-memory testing
  var memoryFile = new MemoryFile(new MemoryStream());
  await memoryFile.WriteTextAsync("Hello World");
  ```

### Key extension methods to know:
- `CreateCopyOfAsync()` - copy files between different storage systems
- `MoveFromAsync()` - move files between storage systems  
- `GetItemByRelativePathAsync()` - navigate using relative paths ("../folder/file.txt")
- `CreateFolderByRelativePathAsync()` - create folder structures from relative paths
- File I/O: `ReadTextAsync()`, `WriteTextAsync()`, `ReadBytesAsync()`, `WriteBytesAsync()`

### Common pitfalls to avoid:
- **Don't assume file paths exist** - many implementations (Memory, HTTP, Zip) don't have traditional file paths
- **Use proper FileAccess** - specify Read, Write, or ReadWrite explicitly in OpenStreamAsync()
- **Handle FileNotFoundException** - thrown when files/folders don't exist
- **Dispose streams properly** - always use `using` statements or proper disposal
- **Don't hardcode separators** - use relative path APIs instead of string manipulation

This is a foundational library that enables file system abstraction across many different storage backends. Focus on interface-based programming and leveraging the rich extension method ecosystem.