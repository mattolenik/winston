using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Winston
{
    public class Wrapper : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        struct Options
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Magic;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string AppPath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string WorkingDir;

            [MarshalAs(UnmanagedType.Bool)]
            public bool WaitForCompletion;

        }

        readonly Stream wrapExe;
        readonly Options opts;

        public Wrapper(Stream exe, string appPath, string workingDir, bool waitForCompletion)
        {
            wrapExe = exe;
            opts = new Options
            {
                Magic = "24cf2af931624d70b7972221e1fa1df",
                AppPath = appPath,
                WorkingDir = workingDir,
                WaitForCompletion = waitForCompletion
            };
        }
        public void Wrap()
        {
            var key = Encoding.Unicode.GetBytes(opts.Magic);
            var optsData = RawSerialize(opts);
            ReplaceAtKey(wrapExe, key, optsData);
        }

        void ReplaceAtKey(Stream stream, byte[] key, byte[] data)
        {
            var pos = FindPosition(stream, key);
            if (pos < 0)
            {
                throw new InvalidDataException("Could not find key in stream");
            }
            stream.Position = pos;
            stream.Write(data, 0, data.Length);
        }

        static byte[] RawSerialize(object anything)
        {
            int rawSize = Marshal.SizeOf(anything);
            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            Marshal.StructureToPtr(anything, buffer, false);
            var rawDatas = new byte[rawSize];
            Marshal.Copy(buffer, rawDatas, 0, rawSize);
            Marshal.FreeHGlobal(buffer);
            return rawDatas;
        }

        long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
            {
                return -1;
            }

            var buffer = new byte[byteSequence.Length];

            int i;
            while ((i = stream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
            {
                if (byteSequence.SequenceEqual(buffer))
                {
                    return stream.Position - byteSequence.Length;
                }
                stream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
            }

            return -1;
        }

        static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                var aux1 = new byte[n];
                var aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }

        public void Dispose()
        {
            if (wrapExe != null)
            {
                wrapExe.Dispose();
            }
        }
    }
}
