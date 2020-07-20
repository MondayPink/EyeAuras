using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Windows;
using CommonServiceLocator;
using dnlib.DotNet;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Modularity;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;
using IModule = Prism.Modularity.IModule;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class SharedModuleCatalog : ModuleCatalog, IAppModuleLoader, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SharedModuleCatalog));
        private static readonly string PrismModuleInterfaceName = typeof(IDynamicModule).FullName;
        private static readonly string ModulesFolderName = "modules";
        private static readonly HashSet<string> SupportedArchives = new HashSet<string>() { ".zip", ".7z" };

        private IModuleCatalog moduleCatalog;
        private IModuleManager manager;
        private Collection<string> defaultModuleList;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public SharedModuleCatalog()
        {
            ModulesDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModulesFolderName));
            Log.Debug($"Creating {nameof(SharedModuleCatalog)}, modulesDirectory: {ModulesDirectory}");
            AssemblyLoadContext.Default.Resolving += DefaultOnResolving;
            Disposable.Create(() => AssemblyLoadContext.Default.Resolving -= DefaultOnResolving);
            Disposable.Create(() => Log.Info($"Disposed {nameof(SharedModuleCatalog)}"));
        }
        private const string NeutralCultureName = "neutral";
        private Assembly? DefaultOnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (!string.IsNullOrEmpty(assemblyName.CultureName) &&
                !string.Equals(assemblyName.CultureName, NeutralCultureName, StringComparison.OrdinalIgnoreCase))
            {
                // resource resolution
                return null;
            }

            var modules = moduleCatalog.Modules.Select(x => x.Ref)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(Path.GetDirectoryName)
                .Where(x => !string.IsNullOrEmpty(x) && Directory.Exists(x))
                .Distinct()
                .ToArray();

            foreach (var moduleDirectory in modules)
            {
                if (string.IsNullOrEmpty(moduleDirectory))
                {
                    continue;
                }
                var assemblyDllName = $"{assemblyName.Name}.dll";
                var assemblyCandidatePath = Path.Combine(moduleDirectory, assemblyDllName);
                if (!File.Exists(assemblyCandidatePath))
                {
                    continue;
                }

                return context.LoadFromAssemblyPath(assemblyCandidatePath);
            }

            return null;
        }

        /// <summary>
        ///     Directory containing modules to search for.
        /// </summary>
        public DirectoryInfo ModulesDirectory { get; }
        
        public DirectoryInfo AppDomainDirectory { get; } = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        /// <summary>
        ///     Drives the main logic of building the child domain and searching for the assemblies.
        /// </summary>
        protected override void InnerLoad()
        {
            Log.Debug($"Initializing {nameof(SharedModuleCatalog)}, service locator: {ServiceLocator.Current} (isSet: {ServiceLocator.IsLocationProviderSet})");
            moduleCatalog = ServiceLocator.Current.GetInstance<IModuleCatalog>();
            manager = ServiceLocator.Current.GetInstance<IModuleManager>();
            defaultModuleList = new Collection<string>(moduleCatalog.Modules.Select(x => x.ModuleName).ToArray());
            Log.Debug($"Default modules list:\r\n\t {defaultModuleList.DumpToTable()}");
            LoadModuleCatalog();
        }
        
        public DirectoryInfo UnzipCompressedModule(string moduleName, byte[] zipBytes)
        {
            Log.Debug($"Unzipping compressed module {moduleName} of size {zipBytes.Length}b into directory {ModulesDirectory}");
            var archivePath = Path.Combine(ModulesDirectory.FullName, moduleName) + ".zip";
            var modulePath = BuildModulePath(moduleName);
            File.WriteAllBytes(archivePath, zipBytes);
            UnpackModule(moduleName, new FileInfo(archivePath), modulePath);
            return modulePath;
        }

        private void UnpackModules()
        {
            if (!ModulesDirectory.Exists)
            {
                throw new InvalidOperationException($"Directory {ModulesDirectory} could not be found.");
            }

            Log.Info($"Enumerating modules in directory '{ModulesDirectory.Name}'");
            var compressedModules = ModulesDirectory.GetFiles("*", SearchOption.TopDirectoryOnly).Where(x => SupportedArchives.Contains(x.Extension)).ToArray();
            Log.Debug($"Found {compressedModules.Length} compressed modules in directory {ModulesDirectory}: {compressedModules.Select(x => x.Name).DumpToTextRaw()}");
            foreach (var compressedModule in compressedModules)
            {
                Log.Debug($"Processing compressed module {compressedModule}");
                var moduleName = Path.GetFileNameWithoutExtension(compressedModule.Name);
                var modulePath = BuildModulePath(moduleName);
                if (modulePath.Exists)
                {
                    Log.Info($"Removing existing module {moduleName} directory {modulePath}");                    
                    modulePath.Delete(true);
                    modulePath.Refresh();
                    if (modulePath.Exists)
                    {
                        throw new ApplicationException($"Failed to remove module directory {modulePath}");
                    }
                }

                UnpackModule(moduleName, compressedModule, modulePath);
            }
        }

        private DirectoryInfo BuildModulePath(string moduleName)
        {
            return new DirectoryInfo(Path.Combine(ModulesDirectory.FullName, moduleName));
        }

        private void UnpackModule(string moduleName, FileInfo compressedModule, DirectoryInfo modulePath)
        {
            Log.Info($"Unpacking module {moduleName}");
            Log.Debug($"Unpacking compressed module {moduleName} into {moduleName}");
            ArchiveFactory.WriteToDirectory(compressedModule.FullName, modulePath.FullName, new ExtractionOptions()
            {
                ExtractFullPath = true,
                PreserveFileTime = true,
                Overwrite = true
            });
            modulePath.Refresh();
            if (!modulePath.Exists)
            {
                throw new ApplicationException($"Failed to unpack module {moduleName} to directory {modulePath}");
            }
                
            Log.Debug($"Removing Module {compressedModule}");
            compressedModule.Delete();
            Log.Info($"Module {moduleName} unpacked");
        }

        private void LoadModuleCatalog()
        {
            var loadedAssemblies = GetLoadedAssemblies();
            Log.Debug($"Loaded assembly list:\n\t{loadedAssemblies.Select(x => new {x.FullName, x.Location}).DumpToTable()}");

            var existingModules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Default Modules list:\n\t{existingModules.Select(x => new {x.ModuleName, x.ModuleType, x.Ref, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            if (!ModulesDirectory.Exists)
            {
                Log.Warn($"Directory {ModulesDirectory} could not be found.");
            }
            else
            {
                UnpackModules();
            }
            
            var potentialModules = (
                from dllFile in new[]
                {
                    AppDomainDirectory.GetFiles("*.dll", SearchOption.TopDirectoryOnly), 
                    ModulesDirectory.Exists ? ModulesDirectory.GetFiles("*.dll", SearchOption.AllDirectories) : Enumerable.Empty<FileInfo>()
                }.SelectMany(x => x)
                let loadedModules = loadedAssemblies.Where(x => !string.IsNullOrEmpty(x.Location))
                    .Select(x => new FileInfo(x.Location))
                    .Where(x => x.Exists)
                    .ToArray()
                where !loadedModules.Contains(dllFile)
                let moduleContext = ModuleDef.CreateModuleContext()
                let dllFileData = File.ReadAllBytes(dllFile.FullName)
                let module = LoadModuleDef(dllFileData, moduleContext, dllFile.FullName)
                where module != null
                select new {module, dllFile}).ToArray();

            var discoveredModules = (
                from item in potentialModules
                from prismBootstrapper in GetPrismBootstrapperTypes(item.module)
                select new {item.dllFile, item.module, prismBootstrapper})
                .ToArray();

            Log.Debug(
                $"Discovered {discoveredModules.Length} modules:\n\t{discoveredModules.Select(x => new {x.dllFile.FullName, x.module.Metadata.VersionString, x.prismBootstrapper.AssemblyQualifiedName}).DumpToTable()}");
            
            foreach (var module in discoveredModules)
            {
                Log.Debug($"Loading modules from file {module.dllFile}");
                var loadedModule = moduleCatalog.Modules.FirstOrDefault(x => x.ModuleType == module.prismBootstrapper.AssemblyQualifiedName);
                if (loadedModule != null)
                {
                    Log.Debug($"Module {loadedModule.ModuleName} is already loaded from {loadedModule.Ref}, ignoring duplicate {module}");
                    continue;
                }

                if (AppArguments.Instance.IsLazyMode)
                {
                    var moduleInfo = PrepareLocalModuleInfo(module.prismBootstrapper, module.dllFile);
                    Log.Debug($"LazyMode is enabled, adding ModuleInfo: {moduleInfo}");
                    moduleCatalog.AddModule(moduleInfo);
                }
                else
                {
                    var assemblyBytes = File.ReadAllBytes(module.dllFile.FullName);
                    LoadModulesFromBytes(assemblyBytes, module.dllFile.FullName);
                }
            }
        }
        
        private ModuleInfo PrepareLocalModuleInfo(IType prismBootstrapperType, FileInfo dllFile)
        {
            var result = new ModuleInfo
            {
                InitializationMode = InitializationMode.OnDemand,
                ModuleName = $"[File] {prismBootstrapperType.FullName}",
                ModuleType = prismBootstrapperType.AssemblyQualifiedName,
                Ref = dllFile.FullName,
                DependsOn = defaultModuleList
            };

            return result;
        }
        
        private ModuleInfo PrepareModuleInfo(IType prismBootstrapperType, string moduleRef)
        {
            var result = new ModuleInfo
            {
                InitializationMode = InitializationMode.OnDemand,
                ModuleName = $"[Memory] {prismBootstrapperType.FullName}",
                ModuleType = prismBootstrapperType.AssemblyQualifiedName,
                Ref = moduleRef,
                DependsOn = defaultModuleList
            };

            return result;
        }

        private void LoadModule(ModuleInfo module)
        {
            Log.Debug($"Loading module {new {module.ModuleName, module.ModuleType, module.Ref}}");

            if (moduleCatalog == null)
            {
                throw new InvalidOperationException("Module catalog is not set yet");
            }
            
            var loadedModule = moduleCatalog.Modules.FirstOrDefault(x => x.ModuleName == module.ModuleName || x.ModuleType == module.ModuleType);
            if (loadedModule != null)
            {
                Log.Warn($"Module {loadedModule.DumpToTextRaw()} is already loaded");
            }
            else
            {
                moduleCatalog.AddModule(module);
                manager.LoadModule(module.ModuleName);
            }
        }
        
        private TypeDef[] GetPrismBootstrapperTypes(ModuleDefMD module)
        {
            var types = module.GetTypes().Where(x => x.HasInterfaces).ToArray();
            var prismTypes = types.Where(x => x.IsClass && !x.IsAbstract)
                .Where(x => x.Interfaces.Any(y => y.Interface.FullName == PrismModuleInterfaceName))
                .ToArray();
            return prismTypes;
        }

        private static Assembly[] GetLoadedAssemblies()
        {
            var loadedAssemblies = (
                from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                where !(assembly is AssemblyBuilder) &&
                      assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder" &&
                      !string.IsNullOrEmpty(assembly.Location)
                select assembly).ToArray();
            return loadedAssemblies;
        }
        
        private static ModuleDefMD LoadModuleDef(byte[] assemblyBytes, ModuleContext moduleContext, string fileName = null)
        {
            try
            {
                return ModuleDefMD.Load(assemblyBytes, moduleContext);
            }
            catch (BadImageFormatException e)
            {
                Log.Debug($"Could not load DLL as .NET assembly - native image ?, binary{(string.IsNullOrEmpty(fileName) ? "from memory" : "from file " + fileName)} size: {assemblyBytes.Length} - {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Log.Warn($"Exception occured when tried to parse DLL metadata, binary size: {assemblyBytes.Length}", e);
                return null;
            }
        }

        private Assembly LoadAssembly(AssemblyLoadContext context, byte[] assemblyBytes)
        {
            Log.Debug($"Loading module from memory, binary data size: {assemblyBytes.Length}b...");
            
            using var assemblyStream = new MemoryStream(assemblyBytes);
            
            var assembly = context.LoadFromStream(assemblyStream);
            var assemblyName = assembly.GetName().Name;
            Log.Debug($"Successfully loaded .NET assembly from memory(name: {assemblyName}, size: {assemblyBytes.Length}): {new { assembly.FullName, assembly.EntryPoint, assembly.ImageRuntimeVersion, assembly.IsFullyTrusted  }}");
            
            return assembly;
        }
        
        private Assembly LoadAssembly(FileInfo dllFile)
        {
            Log.Debug($"Loading module from file, binary data size: {dllFile.Length}b...");
            var assembly = Assembly.LoadFrom(dllFile.FullName);
            var assemblyName = assembly.GetName().Name;
            Log.Debug($"Successfully loaded .NET assembly from file(name: {assemblyName}): {new { assembly.FullName, assembly.EntryPoint, assembly.ImageRuntimeVersion, assembly.IsFullyTrusted  }}");
            
            return assembly;
        }
        
        public void LoadModulesFromBytes(byte[] assemblyBytes, [CanBeNull] string modulePath)
        {
            Log.Debug($"Trying to load Prism module definition from byte array, size: {assemblyBytes.Length}");
            var moduleContext = new ModuleContext();
            var moduleDef = LoadModuleDef(assemblyBytes, moduleContext);
            var prismBootstrappers = GetPrismBootstrapperTypes(moduleDef);
            if (!prismBootstrappers.Any())
            {
                throw new InvalidOperationException($"Failed to find any Prism-compatible type implementing {PrismModuleInterfaceName} in assembly {moduleDef.FullName}");
            }
            
            var assembly = LoadAssembly(AssemblyLoadContext.Default, assemblyBytes);
            foreach (var bootstrapperType in prismBootstrappers)
            {
                Log.Debug($"Loading type {bootstrapperType}");
                var moduleInfo = PrepareModuleInfo(bootstrapperType, modulePath);
                LoadModule(moduleInfo);
            }
        }

        public void LoadModules()
        {
            LoadModuleCatalog();
        }

        public void LoadModulesFromBytes(byte[] assemblyBytes, FileInfo moduleRef)
        {
            LoadModulesFromBytes(assemblyBytes, moduleRef.FullName);
        }

        public void Dispose()
        {
            anchors.Dispose();
        }
    }
}