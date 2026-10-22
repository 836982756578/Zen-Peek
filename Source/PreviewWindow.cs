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

        public PreviewWindow()
        {
            Title = "Zen Peek Önizleme";
            Width = 720;
            Height = 520;
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
                ClosePreview();
            };
        }

        private void BuildUI()
        {
            _rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(246, 20, 22, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 28,
                    ShadowDepth = 6,
                    Opacity = 0.7
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
                Padding = new Thickness(10, 4, 10, 4),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            openBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                {
                    try { Process.Start(new ProcessStartInfo(_currentFilePath) { UseShellExecute = true }); } catch { }
                    ClosePreview();
                }
            };

            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)),
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
                Text = "Kapatmak için Boşluk (Space) veya Esc tuşuna basın",
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
            if (string.IsNullOrEmpty(filePath) || (!File.Exists(filePath) && !Directory.Exists(filePath)))
            {
                return;
            }

            _currentFilePath = filePath;
            _titleBlock.Text = Path.GetFileName(filePath);

            try
            {
                if (Directory.Exists(filePath))
                {
                    ShowDirectoryPreview(filePath);
                }
                else
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
                        ShowImagePreview(filePath);
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
            }
            catch (Exception ex)
            {
                _contentHost.Content = new TextBlock
                {
                    Text = "Önizleme yüklenirken hata oluştu: " + ex.Message,
                    Foreground = Brushes.Red,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
            }

            Show();
            Activate();
            Focus();
        }

        private bool IsImage(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".bmp" || ext == ".gif" || ext == ".ico";
        }

        private bool IsText(string ext)
        {
            return ext == ".txt" || ext == ".cs" || ext == ".py" || ext == ".js" || ext == ".json" ||
                   ext == ".html" || ext == ".css" || ext == ".xml" || ext == ".md" || ext == ".log" ||
                   ext == ".ini" || ext == ".bat" || ext == ".ps1" || ext == ".sql" || ext == ".yaml" || ext == ".yml";
        }

        private void ShowImagePreview(string filePath)
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

        private void ShowTextPreview(string filePath)
        {
            string content;
            using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8, true))
            {
                char[] buffer = new char[8192];
                int read = sr.Read(buffer, 0, buffer.Length);
                content = new string(buffer, 0, read);
                if (!sr.EndOfStream) content += "\n\n... (Devamı için dosyayı açın)";
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

            sp.Children.Add(new TextBlock
            {
                Text = "📁",
                FontSize = 64,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            sp.Children.Add(new TextBlock
            {
                Text = di.FullName,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
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

            sp.Children.Add(new TextBlock
            {
                Text = "📄",
                FontSize = 64,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            sp.Children.Add(new TextBlock
            {
                Text = Path.GetFileName(filePath),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 12, 0, 4)
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

        public void ClosePreview()
        {
            Hide();
            _contentHost.Content = null;
        }
    }
}
