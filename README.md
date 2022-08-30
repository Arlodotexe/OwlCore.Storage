# OwlCore.Storage

### This package enables early adoption of CommunityToolkit.Storage, a package in the [proposal](https://github.com/CommunityToolkit/Labs-Windows/discussions/229) stage in [Toolkit Labs](https://github.com/CommunityToolkit/Labs-Windows). 

When the Labs experiment is merged, this repo will be archived and succeeded by the Labs package. The namespaces will change, and adapter classes with 100% compatability will be provided.

Below is a recent copy of the proposal.

---

# Motivation
## Too many file systems. No standard abstractions.

Listing a few off the top of my head: (click to expand)
<details>
<summary>On-disk APIs</summary>

- System.IO
- StorageFile
- Win32

</details>

<details>
<summary>Cloud storage</summary>

- OneDrive
- Google Drive
- Dropbox
- Box
- Mega

</details>

<details>
<summary>Network protocol</summary>

- FTP
- SMB
- DLNA
- IPFS
- WebDav

</details>

<details>
<summary>Blob storage</summary>

- Azure Blob
- Amazon S3
- Firebase
- Google Cloud
- Oracle Cloud

</details>

Plus countless **custom** HTTP based file systems. These are usually created in-house for a specific project, but they still count. 

## The problem with this

- These are all file systems, and there isn't a single good abstraction between them.
- Lack of a standard means a _lot_ of tools and packages that don't work with each other.
- If you write code that touches one file system, it can't be swapped out for another without changing all your code.
- You can't easily mock a file system for unit tests, especially when code is hardcoded to a specific API.

# History
This started out as AbstractStorage in [OwlCore](https://www.nuget.org/packages/OwlCore/) in 2020. Based on the Windows StorageFile APIs and written in `netstandard2.0`, it was designed to be completely agnostic of any underlying platform or protocol.

It's been incubating for a while in OwlCore, and is used by projects like Fluent Store, Strix Music, Quarrel, ZuneModdingHelper, and more.

I've learned a LOT from using and building for AbstractStorage, and gathering feedback from others who've used it.

# Proposal
We'll use Labs to build a significantly improved version of AbstractStorage under the name `CommunityToolkit.Storage`, and work to bring our experiment to the .NET Community Toolkit.

While this seems easy on the surface, we can't just turn the Windows StorageFile API into a set of ns2.0 interfaces and call it a day. We learned from our first version that this approach has issues once we put them to purpose.

_We can do better_.

> **Note** 
> 
> This proposal has been updated. Based on feedback, some major amendments have been made. For all the details, see:
> [Amendment 1](https://github.com/CommunityToolkit/Labs-Windows/discussions/229#discussioncomment-3283872): Getting folder contents
> [Amendment 2](https://github.com/CommunityToolkit/Labs-Windows/discussions/229#discussioncomment-3391244): Storage properties

## Minimum end goal
Without sacrificing potential functionality or performance, we should strive to:

1. **Maximize compatibility** - with as many file systems as reasonably possible
2. **Minimize effort** - for implementors, consumers and maintainers.

## Specification
- Create the minimal possible interface for files/folders (the "starting point")
- Additional capabilities beyond basics should be done using derived interfaces (see examples below)
  - As separate interfaces, new APIs break nothing. As a derived interface, implementations work with existing code.
  - Breaking changes will affect as few implementations as possible
  - Determining lack of support by checking `if (instance is IMyThing thing)` is faster and more natural than catching thrown `NotSupportException`s.
- Implementors only need to implement what they can do, no more or less.
- Consumers only ask for what's needed, no more or less. Simply ask (or check) for that interface.
- We should have an inbox fallback implementation for certain things (like copy/move) that can work across different file systems.
    - This allows us to align the majority of cross-storage implementations without requiring a base class
    - Removes the need for everyone to do their own implementation
    - Allows for a more performant version if present.
- We should consider file/folder implementations that differ from traditional on-disk systems (e.g. blob storage, mocked files)

## The starting point

Not all file systems are created equal, but they do share some common traits. To maximize compatibility, **this should be our starting point.**

> **Note** These interfaces are named so developers use them by default when building with and for this API, unless they need more functionality.

```csharp
public interface IStorable
{
    /// <summary>
    /// A unique and consistent identifier for this file or folder.
    /// </summary>
    /// <remarks>
    /// Not all files are addressable by path.
    /// Uri paths (like OneDrive) can change entirely when re-authenticating.
    /// Therefore, we need a dedicated resource identifier.
    /// </remarks>
    public string Id { get; }

    /// <summary>
    /// The name of the file or folder, with the extension (if any).
    /// </summary>
    public string Name { get; } 
}

public interface IFile : IStorable
{
    /// <summary>
    /// Opens a new stream to the file.
    /// </summary>
    public Task<Stream> OpenStreamAsync(FileAccessMode accessMode = FileAccessMode.Read, CancellationToken cancellationToken = default); 
}

public interface IFolder : IStorable
{    
    /// <summary>
    /// Retrieves the folders in this directory.
    /// </summary>
    public IAsyncEnumerable<IStorable> GetItemsAsync(StorableKind kind = StorableKind.All, CancellationToken cancellationToken = default);
}

[Flags]
public enum StorableKind
{
    None = 0, // Required because it's a flag
    File = 1, // Only get files
    Folder = 2, // Only get folders
    All = File | Folder, // Get all items.
}
```

> **Note** See conversation below on why we went with a single method + enum for `IFolder`

Even with just this, you can do a _LOT_. Keep in mind, these work with any local disk API, with any Cloud provider, with FTP, SCP, SMB, IPFS, HTTP and pretty much anything else. Plus -- you can mock them for unit tests.

Some examples of things I've made with just this:
- Store and read app settings, user documents, literally anything, in the place of the user's choice.
- Depth-first and breadth-first search tools
- Basic in-app file explorer / folder picker
- Audio file metadata scanner
- Duplicate file finder (across different sources)
- Mock file/folders used to unit test everything above.

Now that we have this, we can build on top of it.


## Adding addressability
### Sometimes, there is no folder

There are 3 ways to obtain a file.
- Get it from a folder
- Get it standalone from other code.
- Create it yourself. Either 100% in memory, or by wrapping another file system.

A file, in its most basic form, must be a unique resource with a data stream and a name. 

This description perfectly matches both in-memory files and some custom file types like blobs - none of which have a "containing" folder.

**Examples of this:**
<details>
<summary>(click to expand):</summary>

- Most blobs in blob storage have the characteristics of a File but aren't location-addressable. No path, no parent folder.
- Unit tests rely on abstractions to mock working implementations for the things you're NOT testing. This abstraction is created entirely in-memory to avoid usage of the actual file system. No path, no parent folder.
- For some libraries like [taglib-sharp](https://github.com/mono/taglib-sharp/blob/6a746346e80abdfc30d23237ebd2c360be1cb7fb/src/TaglibSharp/File.cs#L1718) just need the _concept_ of a file, and (already, without a standard) expect you to implement their `IFileAbstraction`. [You can do this](https://github.com/Arlodotexe/strix-music/blob/1f4da57a19b21eb13ed1164c3ce4a7f02082bfa6/src/Sdk/StrixMusic.Sdk/FileMetadata/Scanners/FileAbstraction.cs) 100% in memory if you need to. No path, no parent folder.

</details>

Even when there's no folder structure built around it, a file can exist standalone. In some edge cases, a file could even exist in multiple folders at once!

For full compatibility, we need to address this.

### Proposed solution
_These interface names are not final, please offer suggestions!_
Current name candidates
- `IAddressableFile`
- `ILocatableFile`

Any APIs meant to be used in the context of an _identifiable_ folder structure should be abstracted to a new interface:

> **Note** `IAddressableFile` and `IAddressableFolder` are included out of the box to avoid requiring a generic check when implementing/consuming.

```csharp
/// <summary>
/// Represents a file or folder that resides within a folder structure.
/// </summary>
public interface IAddressableStorable : IStorable
{
    /// <summary>
    /// A path or uri where the folder resides.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the containing folder for this item, if any.
    /// </summary>
    public Task<IAddressableFolder?> GetParentAsync(CancellationToken cancellationToken = default); 
}

/// <summary>
/// Represents a file that resides within a folder structure.
/// </summary>
public interface IAddressableFile : IFile, IAddressableStorable
{
}


/// <summary>
/// Represents a folder that resides within a folder structure.
/// </summary>
public interface IAddressableFolder  : IFolder, IAddressableStorable
{
}
```

## Storage properties

"Storage properties" are the additional information about a resource provided by most file systems. It's everything from the name to the "Last modified" date to the Thumbnail, and the majority of them can change / be changed.

In AbstractStorage, we simply copied the c# properties and recreated [`StorageFileContentProperties`](https://docs.microsoft.com/en-us/uwp/api/windows.storage.fileproperties.storageitemcontentproperties?view=winrt-22621), and even though we _only_ added `GetMusicPropertiesAsync` and made the property nullable, it caused a lot of confusion for everyone else who implemented it.

We'll continue using our strategy of "separate interfaces" for this. However, there are a LOT of storage properties we can add, so as long as we separate them into different interfaces, can safely leave "which" for later and instead figure out the "how".

There are 3 requirements
- The information must be accessible and always up-to-date.
- We must provide the option to notify the user of changes
- We must provide the option to change the value (separate interface, see mutability vs modifiability below)

This gives us 2 options:
1. Events + properties + `SetThingAsync` methods
   - Property must be nullable if it can't be assigned in the constructor. 
2. Events + `GetThingAsync` Methods + `SetThingAsync` methods

After some brainstorming, we have a basic skeleton that serves as a guide for any property set.
```cs
// The recommended pattern for file properties. 
public interface IStorageProperties<T> : IDisposable
{
    public T Value { get; }

    public event EventHandler<T> ValueUpdated;
}

// Example usage with music properties.
public class MusicData
{
    // todo
}

public interface IMusicProperties
{
    // Must be behind an async method
    public Task<IStorageProperties<MusicData>> GetMusicPropertiesAsync();
}

// If the implementor is capable, they can implement modifying props as well.
public interface IModifiableMusicProperties
{
    public Task UpdateMusicProperties(MusicData newValue);
}
```

Doing it this way means
- There's a standard for getting and tracking groups of properties. Handy for property set designers and enables extensibility scenarios.
- You can easily check if a file/folder supports the interface beforehand, with try/catch or unboxing.
- You won't get name collisions between multiple property sets
- It's behind an async task, meaning it can retrieved asynchronously and easily disposed of, allowing for setup/teardown of sockets, file handles, etc..
- There's no standard for updating properties, it's up to whoever designs the interface. Update one thing at a time, or update the entire object- whatever is most appropriate for the situation (e.g. `UpdateMusicPropertiesAsync()` vs `ChangeNameAsync()`).

Names are not yet final, please offer suggestions if you have them!

## Manipulating folder contents

 Since folders are responsible for holding files, folders should also be responsible for manipulating their own contents. Also, as previously established, a File can exist without the context of a folder - an important detail for Copy and Move operations. 

When designing file system APIs, most implementations don't do this, but given the above, we're 100% sure that we want this method to be implemented on the folder and not the file. 

We'll use that for the interface, and have an extension method that swaps them around to something like this:

```csharp
// Interface method
IFile newFile = await destination.CreateCopyOfAsync(file);

// Extension method
IFile newFile = await file.CopyToAsync(destination);
```

> **Note**
> A folder with contents that change doesn't mean _you_ can change the contents.
> However, if you CAN change the contents, you know there are changes to observe.
> We've separated mutability and modifiability into 2 interfaces to reflect this. 

```csharp
/// <summary>
/// Represents a folder whose content can change.
/// </summary>
public interface IMutableFolder : IFolder
{
    /// <summary>
    /// Raised when the contents of the folder are changed.
    /// TODO: Put behind async method to accommodate for setting up change tracking (websocket, native filesystem, etc). Returned object should be disposable.
    /// </summary>
    public EventHandler<CollectionChangeEventArgs<IStorable>> ContentsChanged;
}

/// <summary>
/// Represents a folder that can be modified.
/// </summary>
public interface IModifiableFolder  : IMutableFolder 
{
    /// <summary>
    /// Deletes the provided storable item from this folder.
    /// </summary>
    public Task DeleteAsync(IStorable item, bool permanantly = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a copy of the provided storable item in this folder.
    /// See above for explainers on why this is here, instead of being on the file.
    /// </summary>
    public Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, CollisionOption collisionOption = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
    /// </summary>
    public Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, CollisionOption collisionOption = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new folder with the desired name inside this folder.
    /// </summary>
    public Task<IFolder?> CreateFolderAsync(string desiredName, CollisionOption collisionOption = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new file with the desired name inside this folder.
    /// </summary>
    public Task<IFile?> CreateFileAsync(string desiredName, CollisionOption collisionOption = default, CancellationToken cancellationToken = default);
}
```

# Wrapping up
Our original requirements aimed to:
1. **Maximize compatibility** - with as many file systems as reasonably possible
2. **Minimize effort** - for implementors, consumers and maintainers.

The above proposal allows for:
- Minimal effort from implementors.
- Minimal breaking changes when they occur.
- No breaking changes to add new features.
- Maximum compatibility using polyfill extension methods.
- Maximum performance when the underlying file system can be more efficient than our polyfills.

Requirements have definitely been met! Now on to the next part.

## Remaining work to do

### File / Folder properties
Notice that we left out implementation details for properties entirely. This is a large, complex space, and now that we know the requirements and options, we can start to gather decent suggestions for a given property or set of properties.

We might skip this entirely for the first PR and just do the basics, since adding a new interface is not a breaking change.

### Gather feedback & refine
(Make sure you've read the full proposal first)

Add your feedback, ask questions, make suggestions. If we can improve this, let's do it!

