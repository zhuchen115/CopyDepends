using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CopyDepends
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, List<string>> arg = ParseArgs(args);
            if(arg.ContainsKey("help"))
            {
                ShowHelp();
                return;
            }
            if((!arg.ContainsKey("input")) || (!arg.ContainsKey("tdir")))
            {
                ShowHelp();
                return;
            }
            string target = arg.ContainsKey("output") ? arg["output"][0] : "";
            bool overwite = arg.ContainsKey("overwrite");
            CopyDll(arg["input"][0], arg["tdir"],target,overwite);
            Console.WriteLine("Done");
        }

        static void ShowHelp()
        {
            Console.WriteLine("Dependencies Finder Version: {0}",Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("This program copies the dependency dll to target folder");
            Console.WriteLine("Usage {0} [OPTIONS] FILE", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Options:");
            Console.WriteLine("-d --directory \t DLL search path");
            Console.WriteLine("-t --target \t Target directory");
            //Console.WriteLine("-v --verbose \t Show copy dlls");
            Console.WriteLine("-i --input \t The input execution");
            Console.WriteLine("-h --help \t Show this help");
            Console.WriteLine("-f --overwrite \t Force overwrite the target file");
        }

        static void CopyDll(string inputpe, List<string> searchdir, string target ="", bool overwrite = false)
        {
            PEModel model = new PEModel();
            model.LoadPE(inputpe);
            if (model.IsManaged == CompilationMode.Invalid)
                Console.WriteLine("The input file is not a valid PE file");
            else
            {
                List<string> dependlist = new List<string>();
                if (model.IsManaged == CompilationMode.Native)
                {
                    NativeDepend nd = new NativeDepend();
                    nd.Init(inputpe);
                    List<string> dllneedcpy = nd.FindMissingDll();
                    Tuple<List<string>, List<string>> findresult = SearchNativeDllRecur(dllneedcpy,searchdir,model.Arch);
                    if(findresult.Item2.Count()>0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("The following dependencies cannot be found");
                    }
                    foreach (string dllmiss in findresult.Item2)
                    {
                        Console.WriteLine("{0}", dllmiss);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    foreach (string dllfound in findresult.Item1)
                    {
                        Console.WriteLine("Found:\t{0} in {1}", Path.GetFileName(dllfound), dllfound);
                    }
                    Console.WriteLine("Copying files to {0}",Path.GetDirectoryName(inputpe));
                    DllCopy copy = new DllCopy
                    {
                        TargetDir = (target == "") ? Path.GetDirectoryName(inputpe) : target 
                    };
                    foreach (string dllfound in findresult.Item1)
                    {
                        Console.WriteLine("Copying\t{0} to {1}", Path.GetFileName(dllfound), copy.TargetDir);
                        copy.CopyDll(Path.GetFileName(dllfound), Path.GetDirectoryName(dllfound), overwrite);
                    }
                }
            }
        }

        static Tuple<List<string>, List<string>> SearchNativeDllRecur(List<string>dllname,List<string>searchpath,MachineType mt)
        {
            // Determine the dll need to find
            List<string> needtofind = new List<string>();
            needtofind.AddRange(dllname);
            List<string> dllfound = new List<string>();
            List<string> dllnotfound = new List<string>();
            do
            {
                Tuple<List<string>, List<string>> findresult = SearchNativeDll(needtofind, searchpath, mt);
                // These are not found dlls in the determined path
                dllnotfound.AddRange(findresult.Item2);
                dllfound.AddRange(findresult.Item1);
                var nf = from name in dllfound.Union(dllnotfound)
                         where needtofind.Contains(Path.GetFileName(name))
                         select Path.GetFileName(name);
                needtofind = needtofind.Except(nf).ToList();
                foreach (string dll in findresult.Item1)
                {
                    NativeDepend nd = new NativeDepend();
                    nd.Init(dll);
                    List<string> depend = nd.FindMissingDll(true);
                    nf = from name in dllfound.Union(dllnotfound)
                         where depend.Contains(Path.GetFileName(name))
                         select Path.GetFileName(name);
                    var t = depend.Except(nf);
                    needtofind.AddRange(t);
                    needtofind = needtofind.Distinct().ToList();
                }

            } while (needtofind.Count() != 0);
            return new Tuple<List<string>, List<string>>(dllfound, dllnotfound);
        }

        static Tuple<List<string>,List<string>> SearchNativeDll(List<string>dllname,List<string> searchpath,MachineType mt)
        {
            bool found = false;
            List<string> dllrpath = new List<string>();
            List<string> dllnotfound = new List<string>();
            foreach (string dll in dllname)
            {
                found = false;
                foreach(string path in searchpath)
                {
                    string rpath = Path.Combine(path, dll);
                    if (File.Exists(rpath))
                    {
                        PEModel pemd = new PEModel();
                        pemd.LoadPE(rpath);
                        if (pemd.Arch == mt)
                        {
                            found = true;
                            dllrpath.Add(rpath);
                            break;
                        }
                    }
                }
                if (!found)
                    dllnotfound.Add(dll);
            }
            return new Tuple<List<string>, List<string>>(dllrpath, dllnotfound);
        }
        static Dictionary<string,List<string>> ParseArgs(string[] args)
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for(int i = 0; i < args.Length; i++)
            {
                if(args[i].StartsWith("-",StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    switch (args[i])
                    {
                        case "-d":
                        case "--directory":
                        case "/d":
                        case "/directory":
                            if (!dict.ContainsKey("tdir"))
                                dict.Add("tdir", new List<string>());
                            dict["tdir"].Add(args[i + 1]);
                            i++;
                            break;
                        case "-h":
                        case "/?":
                        case "/help":
                        case "--help":
                            if (!dict.ContainsKey("help"))
                                dict.Add("help", new List<string>());
                            break;
                        case "-v":
                        case "--verbose":
                        case "/verbose":
                            if (!dict.ContainsKey("verbose"))
                                dict.Add("verbose", new List<string>());
                            break;
                        case "-t":
                        case "--target":
                        case "/outdir":
                            if (!dict.ContainsKey("output"))
                                dict.Add("output", new List<string>());
                            dict["output"].Clear();
                            dict["output"].Add(args[i + 1]);
                            i++;
                            break;
                        case "-i":
                        case "--input":
                        case "/file":
                            if (!dict.ContainsKey("input"))
                                dict.Add("input", new List<string>());
                            dict["input"].Clear();
                            dict["input"].Add(args[i + 1]);
                            i++;
                            break;
                        case "-f":
                        case "--overwrite":
                            if (!dict.ContainsKey("overwrite"))
                                dict.Add("overwite", new List<string>());
                            break;
                    }
                }
                else
                {
                    if (!dict.ContainsKey("input"))
                        dict.Add("input", new List<string>());
                    dict["input"].Clear();
                    dict["input"].Add(args[i]);
                    
                }
            }
            return dict;
        }
    }
}
