using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CopyDepends
{
    /// <summary>
    /// The class to read a PE file header
    /// </summary>
    public class PEModel
    {
        /// <summary>
        /// Defines the path to a PE file
        /// </summary>
        public string PEPath
        {
            get;
            set;
        }

        private byte[] dllData = new byte[4096];
        private bool pe_loaded = false;
        private int ptrCoffRead;
        private CoffHeader coff_header = new CoffHeader();
        /// <summary>
        /// Instance with noting
        /// </summary>
        public PEModel()
        { }

        /// <summary>
        /// Instance with filename
        /// </summary>
        /// <param name="filename">The full path of PE file</param>
        public PEModel(string filename)
        {
            if (File.Exists(filename))
            {
                PEPath = filename;
                LoadPE();
            }
            else
            {
                throw new FileNotFoundException("Specified PE File cannot be found");
            }
        }

        /// <summary>
        /// Load PE with filename
        /// </summary>
        /// <param name="filename"></param>
        public PEMagic LoadPE(string filename)
        {
            if(File.Exists(filename))
            {
                PEPath = filename;
                return LoadPE();
            }
            else
            {
                throw new FileNotFoundException("Specified PE File cannot be found");
            }
        }
        /// <summary>
        /// Load PE header
        /// </summary>
        public PEMagic LoadPE()
        {
            FileInfo file = new FileInfo(PEPath);
            Stream fin = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            int byteread = fin.Read(dllData, 0, 4096);
            fin.Close();

            // Verify this is a executable/dll  
            if ((dllData[1] << 8 | dllData[0]) != 0x5a4d)
            {
                pe_loaded = false;
                return PEMagic.Invalid;
            }

            // This will get the address for the PE header  
            int ptrPEHead = dllData[63] << 24 | dllData[62] << 16 | dllData[61] << 8 | dllData[60];

            // The PE header must start with 'PE'
            if ((dllData[ptrPEHead + 3] << 24 | dllData[ptrPEHead + 2] << 16 | dllData[ptrPEHead + 1] << 8 | dllData[ptrPEHead]) != 0x00004550)
            {
                pe_loaded = false;
                return PEMagic.Invalid;
            }
            pe_loaded = true;
            int ptrCoff = ptrPEHead + 4;
            // Now copy the standard PE header to the structure
            ushort machine = (ushort)((dllData[ptrCoff]) | (dllData[ptrCoff + 1] << 8));
            coff_header.Machine = (MachineType)machine;
            ptrCoff += 2;
            coff_header.NumberOfSections = (ushort)((dllData[ptrCoff]) | (dllData[ptrCoff + 1] << 8));
            ptrCoff += 2;
            coff_header.TimeDateStamp = (uint)(dllData[ptrCoff] | (dllData[ptrCoff + 1] << 8) | (dllData[ptrCoff + 2] << 16) | (dllData[ptrCoff + 3] << 24));
            ptrCoff += 4;
            coff_header.NumberOfSymbols = (uint)(dllData[ptrCoff] | (dllData[ptrCoff + 1] << 8) | (dllData[ptrCoff + 2] << 16) | (dllData[ptrCoff + 3] << 24));
            ptrCoff += 4;
            coff_header.PointerToSymbolTable = (uint)(dllData[ptrCoff] | (dllData[ptrCoff + 1] << 8) | (dllData[ptrCoff + 2] << 16) | (dllData[ptrCoff + 3] << 24));
            ptrCoff += 4;
            coff_header.SizeOfOptionalHeader =  (ushort)((dllData[ptrCoff]) | (dllData[ptrCoff + 1]<<8));
            ptrCoff += 2;
            coff_header.Characteristics = (ushort)((dllData[ptrCoff]) | (dllData[ptrCoff + 1] << 8));
            ptrCoff += 2;
            ptrCoffRead = ptrCoff;
            return PEType;
        }

        public PEMagic PEType
        {
            get
            {
                if (!pe_loaded)
                    throw new InvalidOperationException("PE is not loaded");
                if (coff_header.SizeOfOptionalHeader == 0)
                    return PEMagic.Image;
                else
                {
                    ushort pemagic = (ushort)((dllData[ptrCoffRead]) | (dllData[ptrCoffRead + 1] <<8));
                    if (pemagic == 0x10b)
                        return PEMagic.PE32;
                    else if (pemagic == 0x20b)
                        return PEMagic.PE32Plus;
                    else
                        return PEMagic.Invalid;
                }
            }
        }

        public CompilationMode IsManaged
        {
            get
            {
                if (!pe_loaded)
                    throw new InvalidOperationException("PE is not loaded");
                else
                {
                    int ptrCLRHeader;
                    if (PEType == PEMagic.PE32)
                        ptrCLRHeader = ptrCoffRead + 208;
                    else if (PEType == PEMagic.PE32Plus)
                        ptrCLRHeader = ptrCoffRead + 224;
                    else
                        return CompilationMode.Invalid;
                    int sum = 0, top = ptrCLRHeader + 8;
                    for (int i = ptrCLRHeader; i < top; i++)
                        sum |= dllData[i];
                    if (sum == 0)
                        return CompilationMode.Native;
                    else
                        return CompilationMode.CLR;
                }
            }
        }

        public MachineType Arch
        {
            get
            {
                return coff_header.Machine;
            }
        }
        
    }
}
