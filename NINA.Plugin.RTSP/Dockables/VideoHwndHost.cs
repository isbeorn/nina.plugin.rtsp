using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace NINA.Plugin.RTSP.Dockables {
    //internal class VideoHwndHost : HwndHost {
    //    protected override HandleRef BuildWindowCore(HandleRef hwndParent) {
    //        // Use WS_EX_TRANSPARENT so underlying siblings are painted first
    //        var windowHandle = User32Wrapper.CreateWindowEx(
    //            User32Wrapper.ExtendedWindow32Styles.WS_EX_TRANSPARENT,
    //            "static",
    //            string.Empty,
    //            User32Wrapper.Window32Styles.WS_CHILD
    //                | User32Wrapper.Window32Styles.WS_VISIBLE,
    //            0, 0, 0, 0,
    //            hwndParent.Handle,
    //            IntPtr.Zero,
    //            IntPtr.Zero,
    //            IntPtr.Zero);

    //        return new HandleRef(this, windowHandle);
    //    }

    //    /// <inheritdoc />
    //    protected override void DestroyWindowCore(HandleRef hwnd) {
    //        User32Wrapper.DestroyWindow(hwnd.Handle);
    //    }
    //}

    public class VideoHwndHost : HwndHost {
        private IntPtr _hwnd;
        private IntPtr _parentHwnd;

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_LAYERED = 0x00080000;

        public IntPtr Hwnd => _hwnd;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent) {
            _parentHwnd = hwndParent.Handle;

            // Create a transparent, layered window
            _hwnd = CreateWindowEx(
                WS_EX_LAYERED, // Extended style for layered windows
                "Static",
                "",
                WS_CHILD | WS_VISIBLE,
                0, 0,
                (int)Width, (int)Height,
                _parentHwnd,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            // Set the background to be fully transparent
            SetLayeredWindowAttributes(_hwnd, 0, 0, LWA_COLORKEY);

            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd) {
            if (_hwnd != IntPtr.Zero) {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int LWA_COLORKEY = 0x00000001;
    }
}
