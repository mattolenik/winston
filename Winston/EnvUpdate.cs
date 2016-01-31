using System.Runtime.InteropServices;

namespace Winston
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct EnvUpdate
    {
        public const string Dll32Name = "EnvUpdate.32.dll";
        public const string Dll64Name = "EnvUpdate.64.dll";

        public static string SharedMemName(uint pid)
        {
            return $"WinstonEnvUpdate-{pid}";
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Operation;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32767)]
        public string Path;

        public static EnvUpdate Prepend(string path)
        {
            return new EnvUpdate { Operation = "prepend", Path = path };
        }
    }
}