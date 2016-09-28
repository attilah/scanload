namespace ScanLoad
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Microsoft.Extensions.Options;

    // Enumerate directories and files(catch UnAuthorizedAccess, FileNotFound, other Exception), validates if the PE file contains any CLR Metadata, verifies anycpy/x86/x64 against the running platform  
    //	- Use FileName criterions for Inclusions and Exclusions
    // Needs: Configuration data
    // Result: Produce list of loadable PE files in the form of FileEntry instances
    internal interface IFileScanner
    {
        FileScanResult ScanBinaries();
    }

    internal class FileScanResult
    {
        public FileScanResult(IEnumerable<FileEntry> scanResult)
        {
        }

        public IReadOnlyList<FileEntry> Items { get; }
    }

    internal class FileEntry
    {
        public FileEntry(string fileName, bool isLoadable, IReadOnlyList<string> complaints)
        {
            FileName = fileName;
            IsLoadable = isLoadable;
            Complaints = complaints;
        }

        public string FileName { get; }
        public bool IsLoadable { get; }
        public IReadOnlyList<string> Complaints { get; }
    }

    internal class FileScanner : IFileScanner
    {
        internal FileScanner(IOptions<FileScannerOptions> options)
        {
        }

        public FileScanResult ScanBinaries()
        {
            return default(FileScanResult);
        }
    }

    internal class FileScannerOptions
    {
        private IDictionary<string, SearchOption> directories;

        public FileScannerOptions()
        {
            directories = new ConcurrentDictionary<string, SearchOption>();
        }

        public IReadOnlyDictionary<string, SearchOption> Directories => (IReadOnlyDictionary<string, SearchOption>)directories;
        public AssemblyLoaderPathNameCriterion[] IncludeCriterions { get; }
        public AssemblyLoaderPathNameCriterion[] ExcludeCriterions { get; }

        public FileScannerOptions AddDirectory(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            directories.Add(path, searchOption);

            return this;
        }
    }

    internal interface IAssemblyLoader
    {
        void Load(string fileName);
    }

    // Handle the loading of assembly candidates, subscribe to AppDomain Loading, Resolve events.Handle type discovery based on Reflection/Metadata
    // Result: Populates data back to AssemblyEntry list
    internal class AssemblyLoader : IAssemblyLoader, IDisposable
    {
        public AssemblyLoader(IAssemblyCatalog assemblyCatalog)
        {
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
        }

        public void Load(string fileName)
        {
            // No need to care about the returned assembly, we're processing it thru the AssemblyLoaded event.
            Assembly.Load(fileName);
        }

        protected virtual void AssemblyLoaded(Assembly assembly)
        {
            // Handle catalog insertation after processing
        }

        // Alternatively we can subscribe to AppDomain.ProcessExit instead of IDisposable
        public void Dispose()
        {
        }

        private void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            AssemblyLoaded(args.LoadedAssembly);
        }
    }

    // Container for FileEntry list, produced by IFileScanner, transforms it into AssemblyEntry list.Drives the loading by utilizing IAssemblyLoader
    // - Use Assembly/Type criterion to determine if the binary should be loaded or not
    internal interface IAssemblyCatalog
    {
        void ProcessFileEntries(IReadOnlyList<FileEntry> fileEntries);

        IEnumerable<Type> GetTypes(Predicate<Type> predicate);
    }

    internal class AssemblyCatalog : IAssemblyCatalog
    {
        public AssemblyCatalog(IOptions<AssemblyLoadingOptions> options)
        {
        }

        public void ProcessFileEntries(IReadOnlyList<FileEntry> fileEntries)
        {
        }

        // Some methods from TypeUtils maybe can be migrated over here
        public IEnumerable<Type> GetTypes(Predicate<Type> predicate)
        {
            return null;
        }
    }

    internal class AssemblyLoadingOptions
    {
        public AssemblyLoaderReflectionCriterion[] LoadCriterions { get; }
    }
}
