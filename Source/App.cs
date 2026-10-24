// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace ZenPeek
{
    public class App : System.Windows.Application
    {
        private static Mutex _mutex;
        private NotifyIcon _trayIcon;
        private PreviewWindow _previewWindow;
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _hookProc;

        [STAThread]
        public static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ZenPeek_SingleInstance_Mutex_SerkanSenturk_2026", out createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Zen Peek zaten arka planda çalışıyor!\n\nDosya Gezgini'nde veya Masaüstünde herhangi bir dosyaya tıklayıp Boşluk (Space) tuşuna basarak anında önizleme yapabilirsiniz.\n\nGeliştirici: Serkan ŞENTÜRK",
                    "Zen Peek",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            App app = new App();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _previewWindow = new PreviewWindow();

            _hookProc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookProc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }

            SetupTrayIcon();

            // Periodic memory trimming (~2-3 MB RAM)
            System.Windows.Threading.DispatcherTimer trimTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            trimTimer.Tick += (s, ev) =>
            {
                try
                {
                    NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
                catch { }
            };
            trimTimer.Start();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == NativeMethods.VK_SPACE)
                {
                    // 1. If preview window is open, close it (toggle behavior)
                    if (_previewWindow != null && _previewWindow.IsVisible)
                    {
                        Dispatcher.BeginInvoke(new Action(() => _previewWindow.ClosePreview()));
                        return (IntPtr)1; // Suppress space
                    }

                    // 2. Check if Explorer or Desktop has focus
                    IntPtr fgHwnd = NativeMethods.GetForegroundWindow();
                    if (fgHwnd != IntPtr.Zero)
                    {
                        string className = NativeMethods.GetClassName(fgHwnd);
                        bool isDesktop = className == "Progman" || className == "WorkerW";
                        bool isExplorer = className == "CabinetWClass" || className == "ExploreWClass";

                        if (!isDesktop && !isExplorer)
                        {
                            IntPtr root = NativeMethods.GetAncestor(fgHwnd, NativeMethods.GA_ROOT);
                            if (root != IntPtr.Zero && root != fgHwnd)
                            {
                                string rootClass = NativeMethods.GetClassName(root);
                                if (rootClass == "CabinetWClass" || rootClass == "ExploreWClass")
                                {
                                    isExplorer = true;
                                    fgHwnd = root;
                                }
                                else if (rootClass == "Progman" || rootClass == "WorkerW")
                                {
                                    isDesktop = true;
                                    fgHwnd = root;
                                }
                            }
                        }

                        if (isDesktop || isExplorer)
                        {
                            // If user is editing text (typing in search bar, renaming a file with F2), do not intercept
                            if (!NativeMethods.IsUserEditingText(fgHwnd))
                            {
                                string selectedPath = ShellHelper.GetSelectedFilePath(fgHwnd, isDesktop);
                                if (!string.IsNullOrEmpty(selectedPath))
                                {
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        _previewWindow.ShowFile(selectedPath);
                                    }));
                                    return (IntPtr)1; // Suppress space
                                }
                            }
                        }
                    }
                }
                else if (vkCode == NativeMethods.VK_ESCAPE)
                {
                    if (_previewWindow != null && _previewWindow.IsVisible)
                    {
                        Dispatcher.BeginInvoke(new Action(() => _previewWindow.ClosePreview()));
                        return (IntPtr)1; // Suppress escape
                    }
                }
                else if (vkCode == NativeMethods.VK_UP || vkCode == NativeMethods.VK_DOWN ||
                         vkCode == NativeMethods.VK_LEFT || vkCode == NativeMethods.VK_RIGHT)
                {
                    if (_previewWindow != null && _previewWindow.IsVisible)
                    {
                        // File navigation with arrow keys while preview window is active
                        IntPtr fgHwnd = NativeMethods.GetForegroundWindow();
                        if (fgHwnd != IntPtr.Zero)
                        {
                            string className = NativeMethods.GetClassName(fgHwnd);
                            bool isDesktop = className == "Progman" || className == "WorkerW";
                            bool isExplorer = className == "CabinetWClass" || className == "ExploreWClass";

                            if (!isDesktop && !isExplorer)
                            {
                                IntPtr root = NativeMethods.GetAncestor(fgHwnd, NativeMethods.GA_ROOT);
                                if (root != IntPtr.Zero)
                                {
                                    string rootClass = NativeMethods.GetClassName(root);
                                    if (rootClass == "CabinetWClass" || rootClass == "ExploreWClass")
                                    {
                                        isExplorer = true;
                                        fgHwnd = root;
                                    }
                                    else if (rootClass == "Progman" || rootClass == "WorkerW")
                                    {
                                        isDesktop = true;
                                        fgHwnd = root;
                                    }
                                }
                            }

                            if (isDesktop || isExplorer)
                            {
                                System.Windows.Threading.DispatcherTimer navTimer = new System.Windows.Threading.DispatcherTimer();
                                navTimer.Interval = TimeSpan.FromMilliseconds(75);
                                navTimer.Tick += (s, e) =>
                                {
                                    navTimer.Stop();
                                    if (_previewWindow != null && _previewWindow.IsVisible)
                                    {
                                        string newPath = ShellHelper.GetSelectedFilePath(fgHwnd, isDesktop);
                                        if (!string.IsNullOrEmpty(newPath) && newPath != _previewWindow.CurrentFilePath)
                                        {
                                            _previewWindow.ShowFile(newPath);
                                        }
                                    }
                                };
                                navTimer.Start();
                            }
                        }
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Zen Peek - Hızlı Önizleme (Space) | Serkan ŞENTÜRK";
            _trayIcon.Icon = LoadAppIcon();
            _trayIcon.Visible = true;

            _trayIcon.DoubleClick += (s, e) =>
            {
                _trayIcon.ShowBalloonTip(3000, "Zen Peek ☯️", "Dosya Gezgini'nde veya Masaüstünde herhangi bir dosyaya tıklayıp Boşluk (Space) tuşuna basarak önizleme yapabilirsiniz.", ToolTipIcon.Info);
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem titleItem = new ToolStripMenuItem("☯️ Zen Peek - Hızlı Önizleme");
            titleItem.Font = new System.Drawing.Font(titleItem.Font, System.Drawing.FontStyle.Bold);
            titleItem.Enabled = false;
            menu.Items.Add(titleItem);

            ToolStripMenuItem authorItem = new ToolStripMenuItem("👤 Geliştirici: Serkan ŞENTÜRK");
            authorItem.Font = new System.Drawing.Font(authorItem.Font.FontFamily, 8.5f, System.Drawing.FontStyle.Italic);
            authorItem.Enabled = false;
            menu.Items.Add(authorItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem infoItem = new ToolStripMenuItem("👁️ Kullanım: Dosyayı seçip Boşluk (Space) tuşuna basın");
            infoItem.Enabled = false;
            menu.Items.Add(infoItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem devProfileItem = new ToolStripMenuItem("🌐 Geliştirici Sayfası (GitHub)");
            devProfileItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(devProfileItem);

            ToolStripMenuItem projectRepoItem = new ToolStripMenuItem("⭐ Zen Peek GitHub");
            projectRepoItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578/Zen-Peek") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(projectRepoItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("ℹ️ Zen Peek Hakkında");
            aboutItem.Click += (s, e) =>
            {
                System.Windows.MessageBox.Show(
                    "Zen Peek - Hızlı Dosya Önizleme v1.0.1\n\n" +
                    "🌟 Geliştirici: Serkan ŞENTÜRK\n" +
                    "Telif Hakkı: © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.\n\n" +
                    "macOS QuickLook özelliğini Windows 10/11'e getiren araç.\n" +
                    "Masaüstünde veya Dosya Gezgini'nde herhangi bir dosyaya tıklayıp\n" +
                    "Boşluk (Space) tuşuna bastığınızda dosyayı anında önizler.\n\n" +
                    "• Görseller, Kodlar, Metinler, Klasörler ve Medyalar\n" +
                    "• Windows 11 Sekmeli Gezgin (Tabs) Tam Uyumluluğu\n" +
                    "• Masaüstü Önizleme Desteği\n" +
                    "• Yön Tuşlarıyla (↑ ↓) Dosyalar Arasında Gezinme\n" +
                    "• 100% Bağımsız ve Taşınabilir (Zero-Install Portable)\n" +
                    "• Ultra Düşük Bellek (~2-3 MB RAM)\n\n" +
                    "GitHub: https://github.com/836982756578/Zen-Peek",
                    "Zen Peek Hakkında",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("🚪 Çıkış");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;

            // Show initial balloon tip to inform user
            try
            {
                _trayIcon.ShowBalloonTip(2500, "Zen Peek Aktif ☯️", "Masaüstünde veya Dosya Gezgini'nde bir dosyayı seçip Boşluk (Space) tuşuna basın!", ToolTipIcon.Info);
            }
            catch { }
        }

        private System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    return new System.Drawing.Icon(icoPath, SystemInformation.SmallIconSize);
                }
                return System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void ExitApplication()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_previewWindow != null)
            {
                _previewWindow.Close();
                _previewWindow = null;
            }

            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
                _mutex = null;
            }

            Shutdown();
        }
    }
}
