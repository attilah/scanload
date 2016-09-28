using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace ScanLoad
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();

            p.Run(args);
        }

        private void Run2(string[] args)
        {
            //
            // Setup scanner as early as possible
            //

            var fileScannerOptionsCore = new FileScannerOptions()
                .AddDirectory(".", SearchOption.AllDirectories)
                .AddDirectory("..", SearchOption.AllDirectories);

            var fileScannerOptions = Options.Create(fileScannerOptionsCore);

            var fileScanner = (IFileScanner)new FileScanner(fileScannerOptions);

            var fileScanResult = fileScanner.ScanBinaries();

            //
            // Setup assembly catalog and loader
            //

            var assemblyLoadingOptions = Options.Create(new AssemblyLoadingOptions
            {

            });

            var assemblyCatalog = (IAssemblyCatalog)new AssemblyCatalog(assemblyLoadingOptions);

            //
            // Populate assembly catalog
            //

            assemblyCatalog.ProcessFileEntries(fileScanResult.Items);

            var assemblyLoader = (IAssemblyLoader)new AssemblyLoader(assemblyCatalog);

            //
            // Use catalog as source of assemblies and types, TBD the API needed here, this would be the surfacing type, but not
            // sure if needed for the average developer
            //

            assemblyCatalog.GetTypes(t => t.IsAssignableFrom(typeof(IProvider)));
        }

        private IHostingEnvironment hostingEnvironment = new HostingEnvironment();

        private void Run(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: IsPureIl <filename or path>");
            }

            var input = args[0];

            input = @"C:\Program Files (x86)";

            if (Directory.Exists(input))
            {
                AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs) =>
                {
                    Console.WriteLine($"Loaded: {eventArgs.LoadedAssembly.GetName().Name}");
                };

                AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
                {
                    return null;
                };

                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, eventArgs) =>
                {
                    return null;
                };

                try
                {
                    var filenames = Directory.EnumerateFiles(input, "*.dll", SearchOption.AllDirectories);

                    foreach (var filename in filenames)
                    {
                        ProcessFile(filename);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access denied: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected exception during file scanning: {ex}");
                }
            }
            else if (File.Exists(input))
            {
                ProcessFile(input);
            }
            else
            {
                Console.WriteLine($"Invalid path: {input}");
            }
        }

        private void ProcessFile(string filename)
        {
            try
            {
                using (var peImage = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var peReader = new PEReader(peImage))
                    {
                        if (peReader.HasMetadata)
                        {
                            var metadata = CreateMetadata(peReader.PEHeaders);

                            if (metadata.IsLoadable)
                            {
                                // At this point the file is "safe" to load
                                Console.WriteLine($"Loading: {Path.GetFileName(filename)}");

                                Assembly.LoadFile(filename);
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (BadImageFormatException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception)
            {
            }
        }

        private Metadata CreateMetadata(PEHeaders peHeaders)
        {
            var processorArchitecture = ProcessorArchitecture.MSIL;

            var isPureIL = (peHeaders.CorHeader.Flags & CorFlags.ILOnly) != 0;

            if (peHeaders.PEHeader.Magic == PEMagic.PE32Plus)
                processorArchitecture = ProcessorArchitecture.Amd64;
            else if ((peHeaders.CorHeader.Flags & CorFlags.Requires32Bit) != 0 || !isPureIL)
                processorArchitecture = ProcessorArchitecture.X86;

            var isManaged = isPureIL || processorArchitecture == ProcessorArchitecture.MSIL;

            return new Metadata(peHeaders.CorHeader.MajorRuntimeVersion, peHeaders.CorHeader.MinorRuntimeVersion,
                processorArchitecture, isManaged, hostingEnvironment.Is64BitProcess);
        }
    }

    // Basic info about the hosting environment, currently only encapsulates Environment.Is64BitProcess, to be used by
    internal interface IHostingEnvironment
    {
        bool Is64BitProcess { get; }
    }

    internal class HostingEnvironment : IHostingEnvironment
    {
        public HostingEnvironment()
        {
            Is64BitProcess = Environment.Is64BitProcess;
        }

        public bool Is64BitProcess { get; }
    }

    internal struct Metadata
    {
        internal Metadata(ushort majorRuntimeVersion, ushort minorRuntimeVersion, ProcessorArchitecture processorArchitecture, bool isPureIL, bool is64Process)
        {
            MajorRuntimeVersion = majorRuntimeVersion;
            MinorRuntimeVersion = minorRuntimeVersion;

            IsLoadable = isPureIL && processorArchitecture == ProcessorArchitecture.MSIL ||
                         (
                             !isPureIL &&
                             (
                                 (is64Process && processorArchitecture == ProcessorArchitecture.Amd64) ||
                                 (!is64Process && processorArchitecture == ProcessorArchitecture.X86)
                             )
                         );
        }

        public ushort MajorRuntimeVersion { get; }
        public ushort MinorRuntimeVersion { get; }
        public bool IsLoadable { get; }
    }

    // Dummy interface to make it compile
    internal interface IProvider
    {

    }
}
