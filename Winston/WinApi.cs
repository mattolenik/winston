﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Winston
{
    static class WinApi
    {
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
    }
}