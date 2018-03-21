using System;
using System.Collections.Generic;
using System.IO;
using System.ClrPh;

namespace CopyDepends
{
    /// <summary>
    /// Find the dependencies of Native DLL
    /// </summary>
    public class NativeDepend
    {
        public string filename;
        private static bool _PhInited = false;
        PE localPE = null;
        
        SxsEntries SxsEntriesCache;
        ApiSetSchema ApiSetmapCache;
        public NativeDepend()
        {
            if(!_PhInited)
            {
                Phlib.InitializePhLib();
                _PhInited = true;
            }
        }

        /// <summary>
        /// Initial from a PE File
        /// </summary>
        /// <param name="file">The file name</param>
        public NativeDepend(string file)
        {
            filename = file;
            if (!_PhInited)
            {
                Phlib.InitializePhLib();
                _PhInited = true;
            }
            
        }

        /// <summary>
        /// Initialize the class from a PE file
        /// </summary>
        /// <param name="file"></param>
        public void Init(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException();
            filename = file;
            localPE = new PE(file);
            SxsEntriesCache = SxsManifest.GetSxsEntries(localPE);
            ApiSetmapCache = Phlib.GetApiSetSchema();
        }

        /// <summary>
        /// Get the DLL import name
        /// </summary>
        /// <returns></returns>
        public List<string> GetImportDllName()
        {
            if (localPE == null)
                throw new InvalidOperationException();
            List<PeImportDll> peImports = GetImportDllList();
            List<string> results = new List<string>();
            foreach (PeImportDll importdll in peImports)
            {
                results.Add(importdll.Name);
            }
            return results;
        }

        /// <summary>
        /// Search the missing DLL
        /// </summary>
        /// <param name="ignoreapppath">If true, the current dir will be ignored</param>
        /// <returns>The names of missing DLL</returns>
        public List<string> FindMissingDll(bool ignoreapppath = false)
        {
            if (localPE == null)
            {
                throw new InvalidOperationException();
            }
            List<string> result = new List<string>();
            List<PeImportDll> peImports = GetImportDllList();
            Environment.SpecialFolder WindowsSystemFolder = (localPE.IsWow64Dll()) ?
               Environment.SpecialFolder.SystemX86 :
               Environment.SpecialFolder.System;
            string User32Filepath = Path.Combine(Environment.GetFolderPath(WindowsSystemFolder), "user32.dll");
            foreach(PeImportDll dllImp in peImports)
            {
                Tuple<ModuleSearchStrategy, PE> ResolvedModule = BinaryCache.ResolveModule(localPE, dllImp.Name, SxsEntriesCache);
                ModuleSearchStrategy strategy = ResolvedModule.Item1;
                if (strategy == ModuleSearchStrategy.NOT_FOUND)
                    result.Add(dllImp.Name);
                if (ignoreapppath && (strategy == ModuleSearchStrategy.ApplicationDirectory))
                    result.Add(dllImp.Name);
            }
            return result;
        }

        /// <summary>
        /// Get the list of dependency dll
        /// </summary>
        /// <returns>PE import list</returns>
        public List<PeImportDll> GetImportDllList()
        {
            if (filename == "")
                throw new ArgumentNullException("filename");
            localPE = new PE(filename);
            if (!localPE.Load())
                throw new BadImageFormatException("Cannot Load PE File");
            return localPE.GetImports();
        }

    }
}
