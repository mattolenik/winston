using System;
using System.Runtime.InteropServices;

namespace Winston
{
    static class NativeMethods
    {
#pragma warning disable CC0057 // Unused parameters
        // http://www.pinvoke.net/default.aspx/user32.sendmessagetimeout
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint msg,
            UIntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags flags,
            uint timeout,
            out IntPtr result);

        // http://pinvoke.net/default.aspx/Enums/SendMessageTimeoutFlags.html
        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
            SMTO_ERRORONEXIT = 0x20
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms725497(v=vs.85).aspx
        public const uint WM_WININICHANGE = 0x001A;
        public const uint WM_SETTINGCHANGE = WM_WININICHANGE;

        // http://www.pinvoke.net/default.aspx/Constants/HWND.html
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
#pragma warning restore CC0057 // Unused parameters

#pragma warning disable CC0074 // Make field readonly
        /// <summary>
        /// The file or directory is not a reparse point.
        /// </summary>
        public const int ERROR_NOT_A_REPARSE_POINT = 4390;

        /// <summary>
        /// The reparse point attribute cannot be set because it conflicts with an existing attribute.
        /// </summary>
        public const int ERROR_REPARSE_ATTRIBUTE_CONFLICT = 4391;

        /// <summary>
        /// The data present in the reparse point buffer is invalid.
        /// </summary>
        public const int ERROR_INVALID_REPARSE_DATA = 4392;

        /// <summary>
        /// The tag present in the reparse point buffer is invalid.
        /// </summary>
        public const int ERROR_REPARSE_TAG_INVALID = 4393;

        /// <summary>
        /// There is a mismatch between the tag specified in the request and the tag present in the reparse point.
        /// </summary>
        public const int ERROR_REPARSE_TAG_MISMATCH = 4394;

        /// <summary>
        /// Command to set the reparse point data block.
        /// </summary>
        public const int FSCTL_SET_REPARSE_POINT = 0x000900A4;

        /// <summary>
        /// Command to get the reparse point data block.
        /// </summary>
        public const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

        /// <summary>
        /// Command to delete the reparse point data base.
        /// </summary>
        public const int FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

        /// <summary>
        /// Reparse point tag used to identify mount points and junction points.
        /// </summary>
        public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

        /// <summary>
        /// This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        /// path in the virtual file system.
        /// </summary>
        public const string NonInterpretedPathPrefix = @"\??\";

        [Flags]
        public enum EFileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        [Flags]
        public enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
        }

        public enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct REPARSE_DATA_BUFFER
        {
            /// <summary>
            /// Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength +
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved; do not use.
            /// </summary>
            public ushort Reserved;

            /// <summary>
            /// Offset, in bytes, of the substitute name string in the PathBuffer array.
            /// </summary>
            public ushort SubstituteNameOffset;

            /// <summary>
            /// Length, in bytes, of the substitute name string. If this string is null-terminated,
            /// SubstituteNameLength does not include space for the null character.
            /// </summary>
            public ushort SubstituteNameLength;

            /// <summary>
            /// Offset, in bytes, of the print name string in the PathBuffer array.
            /// </summary>
            public ushort PrintNameOffset;

            /// <summary>
            /// Length, in bytes, of the print name string. If this string is null-terminated,
            /// PrintNameLength does not include space for the null character.
            /// </summary>
            public ushort PrintNameLength;

            /// <summary>
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] PathBuffer;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
            IntPtr InBuffer, int nInBufferSize,
            IntPtr OutBuffer, int nOutBufferSize,
            out int pBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            EFileAccess dwDesiredAccess,
            EFileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            ECreationDisposition dwCreationDisposition,
            EFileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

#pragma warning restore CC0074 // Make field readonly
    }
}