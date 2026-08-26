using System;

namespace XOutput.Devices.Input.DirectInput
{
    /// <summary>
    /// Supplies the top-level window handle that DirectInput needs when acquiring
    /// a device with an exclusive cooperative level. The core library is kept
    /// UI-framework-free: each UI host (WPF, WinUI 3) sets <see cref="HwndProvider"/>
    /// to a function returning its own window handle at startup. Falls back to
    /// <see cref="IntPtr.Zero"/> when no host has registered a provider, in which
    /// case exclusive acquisition will fail and the device skips cooperative level
    /// (the same graceful path the app already takes when no window exists yet).
    /// </summary>
    public static class DirectInputPlatform
    {
        /// <summary>
        /// Gets or sets a function that returns the current top-level window handle.
        /// </summary>
        public static Func<IntPtr> HwndProvider { get; set; }

        /// <summary>
        /// Gets the current window handle, or <see cref="IntPtr.Zero"/> if no host
        /// has registered a provider.
        /// </summary>
        public static IntPtr GetWindowHandle()
        {
            return HwndProvider?.Invoke() ?? IntPtr.Zero;
        }
    }
}
