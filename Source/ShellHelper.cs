// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ZenPeek
{
    public static class ShellHelper
    {
        public static string GetSelectedFilePath(IntPtr fgHwnd, bool isDesktop)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return null;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic windows = shell.Windows();

                // 1. If Desktop is focused, query Desktop folder view
                if (isDesktop)
                {
                    string desktopPath = GetDesktopSelection(windows);
                    if (!string.IsNullOrEmpty(desktopPath))
                    {
                        return desktopPath;
                    }
                }

                // 2. Query File Explorer selection
                string explorerPath = GetExplorerSelection(windows, fgHwnd);
                if (!string.IsNullOrEmpty(explorerPath))
                {
                    return explorerPath;
                }

                // 3. Fallback: If Explorer wasn't matched, check Desktop
                if (!isDesktop)
                {
                    string fallbackDesktop = GetDesktopSelection(windows);
                    if (!string.IsNullOrEmpty(fallbackDesktop))
                    {
                        return fallbackDesktop;
                    }
                }
            }
            catch { }

            return null;
        }

        private static string GetDesktopSelection(dynamic windows)
        {
            try
            {
                int desktopHwnd = 0;
                // SWC_DESKTOP = 8, SWFO_NEEDDISPATCH = 1
                dynamic desktop = windows.FindWindowSW(0, null, 8, out desktopHwnd, 1);
                if (desktop != null && desktop.Document != null)
                {
                    dynamic doc = desktop.Document;
                    dynamic selectedItems = doc.SelectedItems();
                    if (selectedItems != null && selectedItems.Count > 0)
                    {
                        dynamic item = selectedItems.Item(0);
                        if (item != null && !string.IsNullOrEmpty(item.Path))
                        {
                            return item.Path;
                        }
                    }

                    // Fallback to focused item on Desktop
                    try
                    {
                        dynamic focused = doc.FocusedItem;
                        if (focused != null && !string.IsNullOrEmpty(focused.Path))
                        {
                            return focused.Path;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        private static string GetExplorerSelection(dynamic windows, IntPtr fgHwnd)
        {
            try
            {
                int count = windows.Count;
                if (count == 0) return null;

                long fgVal = fgHwnd.ToInt64() & 0xFFFFFFFFL;
                IntPtr fgRoot = NativeMethods.GetAncestor(fgHwnd, NativeMethods.GA_ROOT);
                long fgRootVal = (fgRoot != IntPtr.Zero) ? (fgRoot.ToInt64() & 0xFFFFFFFFL) : fgVal;

                string bestCandidate = null;

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic window = windows.Item(i);
                        if (window == null) continue;

                        long winHwnd = 0;
                        try { winHwnd = window.HWND; } catch { continue; }
                        long winVal = winHwnd & 0xFFFFFFFFL;

                        bool isMatch = (winVal == fgVal) || (winVal == fgRootVal);

                        if (!isMatch && winVal != 0)
                        {
                            IntPtr winRoot = NativeMethods.GetAncestor((IntPtr)winVal, NativeMethods.GA_ROOT);
                            if (winRoot != IntPtr.Zero)
                            {
                                long winRootVal = winRoot.ToInt64() & 0xFFFFFFFFL;
                                if (winRootVal == fgVal || winRootVal == fgRootVal)
                                {
                                    isMatch = true;
                                }
                            }
                        }

                        // If only one window is open and user is in Explorer, match it
                        if (!isMatch && count == 1)
                        {
                            isMatch = true;
                        }

                        if (isMatch)
                        {
                            dynamic doc = window.Document;
                            if (doc != null)
                            {
                                dynamic selectedItems = doc.SelectedItems();
                                if (selectedItems != null && selectedItems.Count > 0)
                                {
                                    dynamic item = selectedItems.Item(0);
                                    if (item != null && !string.IsNullOrEmpty(item.Path))
                                    {
                                        return item.Path;
                                    }
                                }

                                if (bestCandidate == null)
                                {
                                    try
                                    {
                                        dynamic focused = doc.FocusedItem;
                                        if (focused != null && !string.IsNullOrEmpty(focused.Path))
                                        {
                                            bestCandidate = focused.Path;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(bestCandidate))
                {
                    return bestCandidate;
                }

                // Fallback: Check any open Explorer window that currently has selected items
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic window = windows.Item(i);
                        if (window == null) continue;
                        dynamic doc = window.Document;
                        if (doc != null)
                        {
                            dynamic selectedItems = doc.SelectedItems();
                            if (selectedItems != null && selectedItems.Count > 0)
                            {
                                dynamic item = selectedItems.Item(0);
                                if (item != null && !string.IsNullOrEmpty(item.Path))
                                {
                                    return item.Path;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static BitmapSource GetFileLargeIcon(string filePath)
        {
            try
            {
                NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
                IntPtr hImg = NativeMethods.SHGetFileInfo(
                    filePath,
                    0,
                    ref shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON
                );

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                    bs.Freeze();
                    NativeMethods.DestroyIcon(shinfo.hIcon);
                    return bs;
                }
            }
            catch { }
            return null;
        }
    }
}
