using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HydraEngine.Protection.Packer
{
    public class BitDotNet
    {
       public static void ProtectAssembly(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                // Paso 1: Leer el Offset del PE Header (e_lfanew)
                stream.Position = 0x3C;
                uint peHeaderOffset = reader.ReadUInt32();
                stream.Position = peHeaderOffset;

                // Paso 2: Verificar la firma PE ("PE\0\0")
                uint peSignature = reader.ReadUInt32();
                if (peSignature != 0x00004550) // "PE\0\0" en little-endian
                {
                    throw new InvalidOperationException("No es un archivo PE válido.");
                }

                // Paso 3: Leer el File Header
                ushort machine = reader.ReadUInt16();
                ushort numberOfSections = reader.ReadUInt16();
                uint timeDateStamp = reader.ReadUInt32();
                uint pointerToSymbolTable = reader.ReadUInt32();
                uint numberOfSymbols = reader.ReadUInt32();
                ushort sizeOfOptionalHeader = reader.ReadUInt16();
                ushort characteristics = reader.ReadUInt16();

                // Paso 4: Leer el Optional Header Magic para determinar si es PE32 o PE32+
                ushort magic = reader.ReadUInt16();
                bool isPE32Plus = magic == 0x20B;

                // Saltar campos del Optional Header hasta Data Directories
                if (isPE32Plus)
                {
                    stream.Position = peHeaderOffset + 0x18 + 0x70; // 0x18 es el tamaño fijo del File Header, 0x70 es el offset de Data Directories en PE32+
                }
                else
                {
                    stream.Position = peHeaderOffset + 0x18 + 0x60; // 0x60 es el offset de Data Directories en PE32
                }

                // Paso 5: Leer la ubicación del .NET Metadata (Directory[14])
                const int IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14;
                stream.Position += IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR * 8; // Cada entrada de Data Directory es de 8 bytes (VirtualAddress y Size)
                uint dotNetVirtualAddress = reader.ReadUInt32();
                uint dotNetSize = reader.ReadUInt32();

                if (dotNetVirtualAddress == 0)
                {
                    throw new InvalidOperationException("El ensamblado no contiene un encabezado .NET (no es un ensamblado .NET).");
                }

                // Paso 6: Leer las Section Headers para encontrar la sección que contiene el .NET Metadata
                stream.Position = peHeaderOffset + 0x18 + sizeOfOptionalHeader; // Posición de inicio de las Section Headers
                uint dotNetPointerRaw = 0;

                for (int i = 0; i < numberOfSections; i++)
                {
                    // Leer el Section Header
                    byte[] sectionNameBytes = reader.ReadBytes(8);
                    string sectionName = Encoding.UTF8.GetString(sectionNameBytes).TrimEnd('\0');
                    uint virtualSize = reader.ReadUInt32();
                    uint virtualAddress = reader.ReadUInt32();
                    uint sizeOfRawData = reader.ReadUInt32();
                    uint pointerToRawData = reader.ReadUInt32();
                    uint pointerToRelocations = reader.ReadUInt32();
                    uint pointerToLinenumbers = reader.ReadUInt32();
                    ushort numberOfRelocations = reader.ReadUInt16();
                    ushort numberOfLinenumbers = reader.ReadUInt16();
                    uint characteristicsSection = reader.ReadUInt32();

                    // Determinar si esta sección contiene el .NET Metadata
                    if (dotNetVirtualAddress >= virtualAddress && dotNetVirtualAddress < virtualAddress + virtualSize)
                    {
                        dotNetPointerRaw = pointerToRawData + (dotNetVirtualAddress - virtualAddress);
                        break;
                    }
                }

                if (dotNetPointerRaw == 0)
                {
                    throw new InvalidOperationException("No se pudo localizar el .NET Metadata en las secciones del ensamblado.");
                }

                // Paso 7: Leer y modificar la estructura IMAGE_COR20_HEADER
                stream.Position = dotNetPointerRaw;

                // Leer la estructura IMAGE_COR20_HEADER
                byte[] cor20HeaderBytes = reader.ReadBytes(Marshal.SizeOf(typeof(IMAGE_COR20_HEADER)));
                GCHandle handle = GCHandle.Alloc(cor20HeaderBytes, GCHandleType.Pinned);
                IMAGE_COR20_HEADER cor20Header = (IMAGE_COR20_HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(IMAGE_COR20_HEADER));
                handle.Free();

                // Modificar los campos deseados
                //cor20Header.cb = 0; // Sobrescribir el campo cb (0x00)
                //cor20Header.MetaData.Size = 0; // Sobrescribir el tamaño del Metadata (0x0C)
              
                // Opcional: 
                // cor20Header.Flags = 0;
                // cor20Header.EntryPointToken = 0;

                // Escribir la estructura modificada de vuelta al archivo
                stream.Position = dotNetPointerRaw;
                byte[] modifiedCor20HeaderBytes = StructureToByteArray(cor20Header);
                writer.Write(modifiedCor20HeaderBytes);
            }
        }

        // Método para convertir una estructura a un arreglo de bytes
        static byte[] StructureToByteArray(object obj)
        {
            int length = Marshal.SizeOf(obj);
            byte[] array = new byte[length];
            IntPtr ptr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, array, 0, length);
            Marshal.FreeHGlobal(ptr);
            return array;
        }

        // Definición de la estructura IMAGE_COR20_HEADER
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_COR20_HEADER
        {
            // 0x00 - Tamaño de la estructura
            public uint cb;

            // 0x04 - Versión mayor del runtime
            public ushort MajorRuntimeVersion;

            // 0x06 - Versión menor del runtime
            public ushort MinorRuntimeVersion;

            // 0x08 - Directorio de metadatos
            public IMAGE_DATA_DIRECTORY MetaData;

            // 0x10 - Banderas
            public uint Flags;

            // 0x14 - Token del EntryPoint
            public uint EntryPointToken;

            // 0x18 - Directorio de recursos
            public IMAGE_DATA_DIRECTORY Resources;

            // 0x20 - Firma de nombre fuerte
            public IMAGE_DATA_DIRECTORY StrongNameSignature;

            // 0x28 - Tabla del gestor de código
            public IMAGE_DATA_DIRECTORY CodeManagerTable;

            // 0x30 - Arreglos de VTable
            public IMAGE_DATA_DIRECTORY VTableFixups;

            // 0x38 - Saltos de la tabla de direcciones de exportación
            public IMAGE_DATA_DIRECTORY ExportAddressTableJumps;

            // 0x40 - Encabezado nativo gestionado
            public IMAGE_DATA_DIRECTORY ManagedNativeHeader;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DATA_DIRECTORY
        {
            // 0x00 - Dirección virtual
            public uint VirtualAddress;

            // 0x04 - Tamaño
            public uint Size;
        }

    }
}
