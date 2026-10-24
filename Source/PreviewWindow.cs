// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace ZenPeek
{
    public class PreviewWindow : Window
    {
        private string _currentFilePath;
        private Border _rootBorder;
        private Grid _mainGrid;
        private ContentControl _contentHost;
        private TextBlock _titleBlock;
        private TextBlock _infoBlock;

        public string CurrentFilePath
        {
            get { return _currentFilePath; }
        }

        public PreviewWindow()
        {
            Title = "Zen Peek Önizleme";
            Width = 740;
            Height = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;

            BuildUI();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape || e.Key == Key.Space)
                {
                    e.Handled = true;
                    ClosePreview();
                }
            };

            Deactivated += (s, e) =>
            {
                // Only close if user switched completely away to another application (e.g., Chrome, Word)
                // Keep open while user navigates inside File Explorer or Desktop
                try
                {
                    IntPtr fg = NativeMethods.GetForegroundWindow();
                    if (fg != IntPtr.Zero)
                    {
                        string cls = NativeMethods.GetClassName(fg);
                        bool isExplorerOrDesktop = cls == "CabinetWClass" || cls == "ExploreWClass" ||
                                                  cls == "Progman" || cls == "WorkerW";
                        if (!isExplorerOrDesktop)
                        {
                            ClosePreview();
                        }
                    }
                }
                catch { }
            };
        }

        private void BuildUI()
        {
            _rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(246, 18, 18, 26)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 32,
                    ShadowDepth = 6,
                    Opacity = 0.75
                }
            };

            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Header
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            header.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            StackPanel titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _titleBlock = new TextBlock
            {
                Text = "Dosya Önizleme",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _infoBlock = new TextBlock
            {
                Text = "Zen Peek • Serkan ŞENTÜRK",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 180)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(_titleBlock);
            titleStack.Children.Add(_infoBlock);
            Grid.SetColumn(titleStack, 0);

            StackPanel btnStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button openBtn = new Button
            {
                Content = "↗ Dosyayı Aç",
                FontSize = 11,
                Padding = new Thickness(12, 5, 12, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(110, 190, 255)),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            openBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    try { Process.Start(new ProcessStartInfo(_currentFilePath) { UseShellExecute = true }); } catch { }
                    ClosePreview();
                }
            };

            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(190, 195, 210)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => ClosePreview();

            btnStack.Children.Add(openBtn);
            btnStack.Children.Add(closeBtn);
            Grid.SetColumn(btnStack, 1);

            header.Children.Add(titleStack);
            header.Children.Add(btnStack);
            Grid.SetRow(header, 0);
            _mainGrid.Children.Add(header);

            // Content Host
            _contentHost = new ContentControl();
            Grid.SetRow(_contentHost, 1);
            _mainGrid.Children.Add(_contentHost);

            // Footer
            TextBlock footer = new TextBlock
            {
                Text = "Kapatmak için Boşluk veya Esc | Gezinmek için Yön Tuşları (↑ ↓)",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 135, 150)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(footer, 2);
            _mainGrid.Children.Add(footer);

            _rootBorder.Child = _mainGrid;
            Content = _rootBorder;
        }

        public void ShowFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Logger.Log("PreviewWindow.ShowFile called for: " + filePath);
            _currentFilePath = filePath;
            _titleBlock.Text = Path.GetFileName(filePath);

            try
            {
                if (Directory.Exists(filePath))
                {
                    ShowDirectoryPreview(filePath);
                }
                else if (File.Exists(filePath))
                {
                    FileInfo fi = new FileInfo(filePath);
                    string ext = fi.Extension.ToLower();

                    _infoBlock.Text = string.Format("{0} • {1} • {2}",
                        ext.ToUpper().TrimStart('.'),
                        ShellHelper.FormatFileSize(fi.Length),
                        fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                    );

                    if (IsImage(ext))
                    {
                        ShowImagePreview(filePath, fi);
                    }
                    else if (IsText(ext))
                    {
                        ShowTextPreview(filePath);
                    }
                    else
                    {
                        ShowGenericPreview(filePath, fi);
                    }
                }
                else
                {
                    // Virtual or archive path (e.g. inside a zip archive or special shell folder)
                    ShowVirtualOrArchivePath(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("ShowFile exception: " + ex.Message);
                _contentHost.Content = new TextBlock
                {
                    Text = "Önizleme yüklenirken hata: " + ex.Message,
                    Foreground = Brushes.Tomato,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
            }

            if (!IsVisible)
            {
                Show();
            }
            Activate();
            Focus();
        }

        private bool IsImage(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" ||
                   ext == ".bmp" || ext == ".gif" || ext == ".ico" || ext == ".tiff" || ext == ".tif";
        }

        private bool IsText(string ext)
        {
            return ext == ".txt" || ext == ".cs" || ext == ".py" || ext == ".js" || ext == ".ts" ||
                   ext == ".jsx" || ext == ".tsx" || ext == ".json" || ext == ".html" || ext == ".htm" ||
                   ext == ".css" || ext == ".scss" || ext == ".xml" || ext == ".md" || ext == ".log" ||
                   ext == ".ini" || ext == ".bat" || ext == ".cmd" || ext == ".ps1" || ext == ".sql" ||
                   ext == ".yaml" || ext == ".yml" || ext == ".c" || ext == ".cpp" || ext == ".h" ||
                   ext == ".hpp" || ext == ".java" || ext == ".go" || ext == ".rs" || ext == ".php" ||
                   ext == ".rb" || ext == ".sh" || ext == ".cfg" || ext == ".conf" || ext == ".env" ||
                   ext == ".toml";
        }

        private void ShowImagePreview(string filePath, FileInfo fi)
        {
            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(filePath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                _infoBlock.Text += string.Format(" • {0} x {1} px", bmp.PixelWidth, bmp.PixelHeight);

                Image img = new Image
                {
                    Source = bmp,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

                Border frame = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    CornerRadius = new CornerRadius(8),
                    Child = img
                };

                _contentHost.Content = frame;
            }
            catch
            {
                ShowGenericPreview(filePath, fi);
            }
        }

        private void ShowTextPreview(string filePath)
        {
            string content;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true))
            {
                char[] buffer = new char[16384];
                int read = sr.Read(buffer, 0, buffer.Length);
                content = new string(buffer, 0, read);
                if (!sr.EndOfStream) content += "\n\n... (Devamı için '↗ Dosyayı Aç' butonuna tıklayın)";
            }

            TextBox tb = new TextBox
            {
                Text = content,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 235, 245)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 12,
                Padding = new Thickness(10),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap
            };

            _contentHost.Content = tb;
        }

        private void ShowDirectoryPreview(string dirPath)
        {
            DirectoryInfo di = new DirectoryInfo(dirPath);
            int filesCount = 0;
            int dirsCount = 0;
            try
            {
                filesCount = di.GetFiles().Length;
                dirsCount = di.GetDirectories().Length;
            }
            catch { }

            _infoBlock.Text = string.Format("Klasör • {0} dosya, {1} alt klasör • {2}",
                filesCount, dirsCount, di.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));

            StackPanel sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            BitmapSource folderIcon = ShellHelper.GetFileLargeIcon(dirPath);
            if (folderIcon != null)
            {
                sp.Children.Add(new Image
                {
                    Source = folderIcon,
                    Width = 64,
                    Height = 64,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }
            else
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "📁",
                    FontSize = 54,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            sp.Children.Add(new TextBlock
            {
                Text = di.Name,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            sp.Children.Add(new TextBlock
            {
                Text = di.FullName,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            _contentHost.Content = sp;
        }

        private void ShowGenericPreview(string filePath, FileInfo fi)
        {
            StackPanel sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            BitmapSource icon = ShellHelper.GetFileLargeIcon(filePath);
            if (icon != null)
            {
                Image img = new Image
                {
                    Source = icon,
                    Width = 64,
                    Height = 64,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                sp.Children.Add(img);
            }
            else
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "📄",
                    FontSize = 54,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            sp.Children.Add(new TextBlock
            {
                Text = Path.GetFileName(filePath),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            sp.Children.Add(new TextBlock
            {
                Text = filePath,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 165, 180)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });

            _contentHost.Content = sp;
        }

        private void ShowVirtualOrArchivePath(string virtualPath)
        {
            _infoBlock.Text = "Arşiv / Özel Konum";

            StackPanel sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            sp.Children.Add(new TextBlock
            {
                Text = "📦",
                FontSize = 56,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            sp.Children.Add(new TextBlock
            {
                Text = Path.GetFileName(virtualPath),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            sp.Children.Add(new TextBlock
            {
                Text = "Bu dosya bir ZIP veya arşiv içerisinde görüntüleniyor.\nTam içerik önizlemesi için lütfen arşivi klasöre çıkartın.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(190, 195, 210)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 12)
            });

            _contentHost.Content = sp;
        }

        public void ClosePreview()
        {
            Hide();
            _contentHost.Content = null;
            _currentFilePath = null;
        }
    }
}
