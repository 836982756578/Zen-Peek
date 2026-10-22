// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ZenPeek
{
    public static class ShellHelper
    {
        public static string GetSelectedFilePath()
        {
            try
            {
                IntPtr fgHwnd = NativeMethods.GetForegroundWindow();
                if (fgHwnd == IntPtr.Zero) return null;

                string className = NativeMethods.GetActiveClassName();

                // Check File Explorer or Desktop
                bool isExplorer = className == "CabinetWClass" || className == "ExploreWClass";
                bool isDesktop = className == "Progman" || className == "WorkerW";

                if (!isExplorer && !isDesktop)
                {
                    return null;
                }

                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return null;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic windows = shell.Windows();

                int count = windows.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic window = windows.Item(i);
                        if (window == null) continue;

                        long hwnd = window.HWND;
                        if (isDesktop || (IntPtr)hwnd == fgHwnd)
                        {
                            dynamic doc = window.Document;
                            if (doc != null)
                            {
                                dynamic selectedItems = doc.SelectedItems();
                                if (selectedItems != null && selectedItems.Count > 0)
                                {
                                    dynamic item = selectedItems.Item(0);
                                    if (item != null)
                                    {
                                        string path = item.Path;
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            return path;
                                        }
                                    }
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
    }
}
