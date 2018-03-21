using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CopyDepends
{
    class DllCopy
    {
        public string TargetDir
        {
            get;
            set;
        }

        /// <summary>
        /// Copy DLL to target directory
        /// </summary>
        /// <param name="dllname"></param>
        /// <param name="origin"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public bool CopyDll(string dllname,string origin,bool overwrite = false)
        {
            if (!dllname.EndsWith(".dll",StringComparison.OrdinalIgnoreCase))
                dllname = dllname + ".dll";
            string origdll = Path.Combine(origin, dllname);
            string destdll = Path.Combine(TargetDir, dllname);
            if (!File.Exists(origdll))
            {
                throw new FileNotFoundException("Cannot Locate DLL File", dllname);
            }
            if (File.Exists(destdll) && (!overwrite))
                return false;
            File.Copy(origdll, destdll, overwrite);
            return true;
        }
    }
}
