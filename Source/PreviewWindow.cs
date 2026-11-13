// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
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
            Width = 780;
            Height = 560;
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
                FontSize = 14.5,
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

                    if (ext == ".zip")
                    {
                        ShowZipPreview(filePath, fi);
                    }
                    else if (ext == ".docx")
                    {
                        ShowDocxPreview(filePath, fi);
                    }
                    else if (ext == ".xlsx" || ext == ".csv")
                    {
                        ShowXlsxPreview(filePath, fi);
                    }
                    else if (ext == ".pdf")
                    {
                        ShowPdfPreview(filePath, fi);
                    }
                    else if (IsImage(ext))
                    {
                        _infoBlock.Text = string.Format("{0} • {1} • {2}",
                            ext.ToUpper().TrimStart('.'),
                            ShellHelper.FormatFileSize(fi.Length),
                            fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                        );
                        ShowImagePreview(filePath, fi);
                    }
                    else if (IsText(ext))
                    {
                        _infoBlock.Text = string.Format("{0} • {1} • {2}",
                            ext.ToUpper().TrimStart('.'),
                            ShellHelper.FormatFileSize(fi.Length),
                            fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                        );
                        ShowTextPreview(filePath);
                    }
                    else
                    {
                        _infoBlock.Text = string.Format("{0} • {1} • {2}",
                            ext.ToUpper().TrimStart('.'),
                            ShellHelper.FormatFileSize(fi.Length),
                            fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                        );
                        ShowGenericPreview(filePath, fi);
                    }
                }
                else
                {
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

        // ==========================================
        // 1. FOLDER CONTENTS PREVIEW (Klasör İçeriği)
        // ==========================================
        private void ShowDirectoryPreview(string dirPath)
        {
            DirectoryInfo di = new DirectoryInfo(dirPath);
            DirectoryInfo[] subDirs = new DirectoryInfo[0];
            FileInfo[] files = new FileInfo[0];

            try
            {
                subDirs = di.GetDirectories();
                files = di.GetFiles();
            }
            catch { }

            int totalItems = subDirs.Length + files.Length;
            _infoBlock.Text = string.Format("Klasör İçeriği • {0} dosya, {1} alt klasör • {2}",
                files.Length, subDirs.Length, di.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));

            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 8, 0)
            };

            StackPanel container = new StackPanel();

            if (totalItems == 0)
            {
                TextBlock emptyBlock = new TextBlock
                {
                    Text = "Bu klasör boş.",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 180)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                container.Children.Add(emptyBlock);
            }
            else
            {
                int count = 0;
                int maxItems = 120;

                // Subdirectories first
                foreach (DirectoryInfo sub in subDirs)
                {
                    count++;
                    if (count > maxItems) break;

                    Border row = CreateListRow("📁", sub.Name, "<Klasör>", sub.LastWriteTime.ToString("dd.MM.yyyy HH:mm"), true);
                    container.Children.Add(row);
                }

                // Files next
                foreach (FileInfo f in files)
                {
                    count++;
                    if (count > maxItems) break;

                    string icon = GetIconForExtension(f.Extension);
                    Border row = CreateListRow(icon, f.Name, ShellHelper.FormatFileSize(f.Length), f.LastWriteTime.ToString("dd.MM.yyyy HH:mm"), false);
                    container.Children.Add(row);
                }

                if (totalItems > maxItems)
                {
                    TextBlock moreBlock = new TextBlock
                    {
                        Text = string.Format("... ve {0} öğe daha (Tümünü görmek için '↗ Dosyayı Aç' butonuna tıklayın)", totalItems - maxItems),
                        Foreground = new SolidColorBrush(Color.FromRgb(130, 170, 220)),
                        FontSize = 11,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    container.Children.Add(moreBlock);
                }
            }

            sv.Content = container;
            _contentHost.Content = sv;
        }

        // ==========================================
        // 2. ZIP ARCHIVE PREVIEW (ZIP Önizlemesi)
        // ==========================================
        private void ShowZipPreview(string zipPath, FileInfo fi)
        {
            ZipResult res = DocumentParsers.ParseZip(zipPath);

            _infoBlock.Text = string.Format("ZIP Arşivi • {0} dosya, {1} klasör • Açık Boyut: {2} • Sıkıştırılmış: {3}",
                res.TotalFiles, res.TotalDirectories,
                ShellHelper.FormatFileSize(res.TotalUncompressedSize),
                ShellHelper.FormatFileSize(res.TotalCompressedSize));

            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 8, 0)
            };

            StackPanel container = new StackPanel();

            if (res.Entries.Count == 0)
            {
                TextBlock emptyBlock = new TextBlock
                {
                    Text = "Bu arşiv boş.",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 180)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                container.Children.Add(emptyBlock);
            }
            else
            {
                int count = 0;
                int maxItems = 120;

                foreach (ZipEntryInfo entry in res.Entries)
                {
                    count++;
                    if (count > maxItems) break;

                    string icon = entry.IsDirectory ? "📁" : GetIconForExtension(Path.GetExtension(entry.Name));
                    string sizeStr = entry.IsDirectory ? "<Klasör>" : ShellHelper.FormatFileSize(entry.Length);
                    string dateStr = entry.LastWriteTime.ToString("dd.MM.yyyy HH:mm");

                    Border row = CreateListRow(icon, entry.Name, sizeStr, dateStr, entry.IsDirectory);
                    container.Children.Add(row);
                }

                if (res.Entries.Count > maxItems)
                {
                    TextBlock moreBlock = new TextBlock
                    {
                        Text = string.Format("... ve {0} arşiv öğesi daha", res.Entries.Count - maxItems),
                        Foreground = new SolidColorBrush(Color.FromRgb(130, 170, 220)),
                        FontSize = 11,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    container.Children.Add(moreBlock);
                }
            }

            sv.Content = container;
            _contentHost.Content = sv;
        }

        // ==========================================
        // 3. WORD DOCUMENT PREVIEW (DOCX Önizlemesi)
        // ==========================================
        private void ShowDocxPreview(string docxPath, FileInfo fi)
        {
            DocxResult res = DocumentParsers.ParseDocx(docxPath);

            string info = string.Format("Word Belgesi (.docx) • {0} paragraf • ~{1} kelime • {2}",
                res.ParagraphCount, res.WordCount, ShellHelper.FormatFileSize(fi.Length));

            if (!string.IsNullOrEmpty(res.Author))
            {
                info += " • Yazar: " + res.Author;
            }
            _infoBlock.Text = info;

            Grid docGrid = new Grid();
            docGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            docGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            if (!string.IsNullOrEmpty(res.Title))
            {
                Border titleBanner = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 43, 114, 186)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(90, 43, 114, 186)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                TextBlock tb = new TextBlock
                {
                    Text = "📘 " + res.Title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 215, 255))
                };
                titleBanner.Child = tb;
                Grid.SetRow(titleBanner, 0);
                docGrid.Children.Add(titleBanner);
            }

            TextBox textBox = new TextBox
            {
                Text = string.IsNullOrEmpty(res.Text) ? "(Belge metin içeriği bulunamadı veya boş)" : res.Text,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 240, 250)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Segoe UI, Calibri, Arial"),
                FontSize = 12.5,
                Padding = new Thickness(14),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(textBox, 1);
            docGrid.Children.Add(textBox);

            _contentHost.Content = docGrid;
        }

        // ==========================================
        // 4. EXCEL SPREADSHEET PREVIEW (XLSX / CSV)
        // ==========================================
        private void ShowXlsxPreview(string filePath, FileInfo fi)
        {
            XlsxResult res = DocumentParsers.ParseXlsx(filePath);

            _infoBlock.Text = string.Format("Excel Tablosu (.xlsx) • {0} • {1} satır • {2}",
                res.SheetName, res.Rows.Count, ShellHelper.FormatFileSize(fi.Length));

            if (res.Rows.Count == 0)
            {
                _contentHost.Content = new TextBlock
                {
                    Text = "Tabloda veri bulunamadı veya sayfa boş.",
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 170, 190)),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                return;
            }

            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel tablePanel = new StackPanel();

            for (int r = 0; r < res.Rows.Count; r++)
            {
                List<string> rowData = res.Rows[r];
                bool isHeader = (r == 0);

                StackPanel rowPanel = new StackPanel { Orientation = Orientation.Horizontal };
                Border rowBorder = new Border
                {
                    Background = isHeader
                        ? new SolidColorBrush(Color.FromArgb(80, 20, 100, 50))
                        : (r % 2 == 0 ? new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)) : Brushes.Transparent),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(4, 3, 4, 3)
                };

                for (int c = 0; c < rowData.Count; c++)
                {
                    Border cell = new Border
                    {
                        Width = 110,
                        Padding = new Thickness(6, 2, 6, 2)
                    };
                    TextBlock tb = new TextBlock
                    {
                        Text = rowData[c],
                        FontSize = 11.5,
                        FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = isHeader ? Brushes.White : new SolidColorBrush(Color.FromRgb(220, 225, 235)),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    cell.Child = tb;
                    rowPanel.Children.Add(cell);
                }

                rowBorder.Child = rowPanel;
                tablePanel.Children.Add(rowBorder);
            }

            sv.Content = tablePanel;
            _contentHost.Content = sv;
        }

        // ==========================================
        // 5. PDF DOCUMENT PREVIEW (PDF Önizlemesi)
        // ==========================================
        private void ShowPdfPreview(string pdfPath, FileInfo fi)
        {
            PdfResult res = DocumentParsers.ParsePdf(pdfPath);

            _infoBlock.Text = string.Format("PDF Belgesi • {0} Sayfa • v{1} • {2}",
                res.PageCount, res.Version, ShellHelper.FormatFileSize(fi.Length));

            Grid pdfGrid = new Grid();
            pdfGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            pdfGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Summary Card
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 200, 50, 50)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock iconBlock = new TextBlock
            {
                Text = "📑",
                FontSize = 32,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(iconBlock, 0);
            cardGrid.Children.Add(iconBlock);

            StackPanel details = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            string titleText = !string.IsNullOrEmpty(res.Title) ? res.Title : Path.GetFileName(pdfPath);
            details.Children.Add(new TextBlock
            {
                Text = titleText,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            string metaText = string.Format("Toplam {0} Sayfa  |  PDF v{1}  |  {2}",
                res.PageCount, res.Version, ShellHelper.FormatFileSize(fi.Length));
            if (!string.IsNullOrEmpty(res.Author)) metaText += "  |  Yazar: " + res.Author;

            details.Children.Add(new TextBlock
            {
                Text = metaText,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 200)),
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(details, 1);
            cardGrid.Children.Add(details);
            card.Child = cardGrid;

            Grid.SetRow(card, 0);
            pdfGrid.Children.Add(card);

            // Document text preview if extracted
            if (!string.IsNullOrEmpty(res.Text) && res.Text.Length > 20)
            {
                TextBox textBox = new TextBox
                {
                    Text = res.Text,
                    IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(235, 240, 250)),
                    BorderThickness = new Thickness(0),
                    FontFamily = new FontFamily("Segoe UI, Arial"),
                    FontSize = 12,
                    Padding = new Thickness(14),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(textBox, 1);
                pdfGrid.Children.Add(textBox);
            }
            else
            {
                // Scanned PDF / Vector graphics notice
                Border noticeBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20)
                };

                StackPanel noticePanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                noticePanel.Children.Add(new TextBlock
                {
                    Text = "📄",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                noticePanel.Children.Add(new TextBlock
                {
                    Text = "Bu PDF dosyası taranmış görsel veya vektörel sayfalardan oluşmaktadır.",
                    FontSize = 13,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });

                noticePanel.Children.Add(new TextBlock
                {
                    Text = "Tüm sayfaları tam çözünürlükte görüntülemek için sağ üstteki '↗ Dosyayı Aç' butonuna tıklayabilirsiniz.",
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 170, 190)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 0)
                });

                noticeBorder.Child = noticePanel;
                Grid.SetRow(noticeBorder, 1);
                pdfGrid.Children.Add(noticeBorder);
            }

            _contentHost.Content = pdfGrid;
        }

        // ==========================================
        // HELPER: LIST ROW CREATION FOR FOLDER & ZIP
        // ==========================================
        private Border CreateListRow(string icon, string name, string sizeStr, string dateStr, bool isDir)
        {
            Border row = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 4)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock iconBlock = new TextBlock
            {
                Text = icon,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(iconBlock, 0);

            TextBlock nameBlock = new TextBlock
            {
                Text = name,
                FontSize = 12,
                FontWeight = isDir ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isDir ? new SolidColorBrush(Color.FromRgb(255, 215, 100)) : Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 1);

            TextBlock sizeBlock = new TextBlock
            {
                Text = sizeStr,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 180, 220)),
                Width = 90,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(sizeBlock, 2);

            TextBlock dateBlock = new TextBlock
            {
                Text = dateStr,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 135, 150)),
                Width = 110,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateBlock, 3);

            g.Children.Add(iconBlock);
            g.Children.Add(nameBlock);
            g.Children.Add(sizeBlock);
            g.Children.Add(dateBlock);

            row.Child = g;
            return row;
        }

        private string GetIconForExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "📄";
            ext = ext.ToLower();

            if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".webp" || ext == ".gif" || ext == ".bmp") return "🖼️";
            if (ext == ".pdf") return "📑";
            if (ext == ".docx" || ext == ".doc" || ext == ".rtf" || ext == ".odt") return "📘";
            if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") return "📊";
            if (ext == ".pptx" || ext == ".ppt") return "📙";
            if (ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".tar" || ext == ".gz") return "📦";
            if (ext == ".mp3" || ext == ".wav" || ext == ".m4a" || ext == ".flac") return "🎵";
            if (ext == ".mp4" || ext == ".mkv" || ext == ".avi" || ext == ".mov") return "🎬";
            if (ext == ".cs" || ext == ".py" || ext == ".js" || ext == ".ts" || ext == ".html" || ext == ".css" || ext == ".json" || ext == ".xml") return "💻";
            if (ext == ".exe" || ext == ".bat" || ext == ".cmd" || ext == ".ps1") return "⚡";

            return "📄";
        }

        // ==========================================
        // 6. OTHER MEDIA / TEXT / IMAGE HANDLERS
        // ==========================================
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
                   ext == ".toml" || ext == ".rtf";
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
                char[] buffer = new char[24576]; // up to 24KB preview
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
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 12,
                Padding = new Thickness(12),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap
            };

            _contentHost.Content = tb;
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
                    Text = GetIconForExtension(fi.Extension),
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
