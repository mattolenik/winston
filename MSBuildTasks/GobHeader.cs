using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Winston.MSBuildTasks
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct GobHeader
    {
        [MarshalAs(UnmanagedType.U1)]
        public GobHeaderType Type;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Name;

        [MarshalAs(UnmanagedType.U8)]
        public ulong Size;

        public GobHeader(string name, GobHeaderType type, long size = 0)
        {
            Name = name;
            Type = type;
            Size = (ulong) size;
        }

        byte[] GetBytes()
        {
            var size = Marshal.SizeOf(this);
            var result = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, result, 0, size);
            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public void CopyTo(Stream stream)
        {
            var bytes = GetBytes();
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    public enum GobHeaderType : byte
    {
        File = 0,
        Directory = 1
    }
}