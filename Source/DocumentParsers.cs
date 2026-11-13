// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZenPeek
{
    public class DocxResult
    {
        public string Title;
        public string Author;
        public int ParagraphCount;
        public int WordCount;
        public string Text;
    }

    public class XlsxResult
    {
        public string SheetName;
        public List<List<string>> Rows;
    }

    public class PdfResult
    {
        public string Version;
        public int PageCount;
        public string Title;
        public string Author;
        public string Subject;
        public string Text;
    }

    public class ZipEntryInfo
    {
        public string Name;
        public bool IsDirectory;
        public long Length;
        public long CompressedLength;
        public DateTime LastWriteTime;
    }

    public class ZipResult
    {
        public int TotalFiles;
        public int TotalDirectories;
        public long TotalUncompressedSize;
        public long TotalCompressedSize;
        public List<ZipEntryInfo> Entries;
    }

    public static class DocumentParsers
    {
        public static DocxResult ParseDocx(string filePath)
        {
            DocxResult result = new DocxResult();
            result.Text = "";
            result.Title = "";
            result.Author = "";

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                // 1. Try reading docProps/core.xml for Title & Author
                try
                {
                    ZipArchiveEntry coreEntry = archive.GetEntry("docProps/core.xml");
                    if (coreEntry != null)
                    {
                        using (Stream s = coreEntry.Open())
                        {
                            XDocument xCore = XDocument.Load(s);
                            XNamespace dc = "http://purl.org/dc/elements/1.1/";
                            XElement titleEl = xCore.Root != null ? xCore.Root.Element(dc + "title") : null;
                            if (titleEl != null && !string.IsNullOrEmpty(titleEl.Value))
                            {
                                result.Title = titleEl.Value;
                            }
                            XElement creatorEl = xCore.Root != null ? xCore.Root.Element(dc + "creator") : null;
                            if (creatorEl != null && !string.IsNullOrEmpty(creatorEl.Value))
                            {
                                result.Author = creatorEl.Value;
                            }
                        }
                    }
                }
                catch { }

                // 2. Read word/document.xml
                ZipArchiveEntry docEntry = archive.GetEntry("word/document.xml");
                if (docEntry != null)
                {
                    using (Stream s = docEntry.Open())
                    {
                        XDocument xDoc = XDocument.Load(s);
                        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                        StringBuilder sb = new StringBuilder();
                        int pCount = 0;
                        int wCount = 0;

                        if (xDoc.Root != null)
                        {
                            foreach (XElement p in xDoc.Descendants(w + "p"))
                            {
                                pCount++;
                                bool isBullet = p.Descendants(w + "numPr").GetEnumerator().MoveNext();
                                bool isHeading = false;

                                foreach (XElement style in p.Descendants(w + "pStyle"))
                                {
                                    XAttribute val = style.Attribute(w + "val");
                                    if (val != null && val.Value.IndexOf("Heading", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        isHeading = true;
                                        break;
                                    }
                                }

                                StringBuilder pText = new StringBuilder();
                                foreach (XElement t in p.Descendants(w + "t"))
                                {
                                    pText.Append(t.Value);
                                }

                                string textLine = pText.ToString().Trim();
                                if (textLine.Length > 0)
                                {
                                    if (isHeading)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("=== " + textLine.ToUpper() + " ===");
                                    }
                                    else if (isBullet)
                                    {
                                        sb.AppendLine("  • " + textLine);
                                    }
                                    else
                                    {
                                        sb.AppendLine(textLine);
                                    }

                                    string[] words = textLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    wCount += words.Length;
                                }
                                else
                                {
                                    sb.AppendLine();
                                }
                            }

                            // Also read tables
                            foreach (XElement tbl in xDoc.Descendants(w + "tbl"))
                            {
                                sb.AppendLine();
                                sb.AppendLine("[Tablo]:");
                                foreach (XElement tr in tbl.Descendants(w + "tr"))
                                {
                                    List<string> rowCells = new List<string>();
                                    foreach (XElement tc in tr.Descendants(w + "tc"))
                                    {
                                        StringBuilder tcText = new StringBuilder();
                                        foreach (XElement t in tc.Descendants(w + "t"))
                                        {
                                            tcText.Append(t.Value);
                                        }
                                        rowCells.Add(tcText.ToString().Trim());
                                    }
                                    if (rowCells.Count > 0)
                                    {
                                        sb.AppendLine("  | " + string.Join(" | ", rowCells.ToArray()) + " |");
                                    }
                                }
                                sb.AppendLine();
                            }
                        }

                        result.ParagraphCount = pCount;
                        result.WordCount = wCount;
                        result.Text = sb.ToString().Trim();
                    }
                }
            }

            return result;
        }

        public static XlsxResult ParseXlsx(string filePath)
        {
            XlsxResult result = new XlsxResult();
            result.Rows = new List<List<string>>();
            result.SheetName = "Sayfa 1";

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                List<string> sharedStrings = new List<string>();
                ZipArchiveEntry ssEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (ssEntry != null)
                {
                    using (Stream s = ssEntry.Open())
                    {
                        XDocument xss = XDocument.Load(s);
                        XNamespace ns = xss.Root != null ? xss.Root.Name.Namespace : XNamespace.None;
                        foreach (XElement si in xss.Descendants(ns + "si"))
                        {
                            StringBuilder siText = new StringBuilder();
                            foreach (XElement t in si.Descendants(ns + "t"))
                            {
                                siText.Append(t.Value);
                            }
                            sharedStrings.Add(siText.ToString());
                        }
                    }
                }

                try
                {
                    ZipArchiveEntry wbEntry = archive.GetEntry("xl/workbook.xml");
                    if (wbEntry != null)
                    {
                        using (Stream s = wbEntry.Open())
                        {
                            XDocument xwb = XDocument.Load(s);
                            XNamespace ns = xwb.Root != null ? xwb.Root.Name.Namespace : XNamespace.None;
                            XElement firstSheet = null;
                            foreach (XElement sh in xwb.Descendants(ns + "sheet"))
                            {
                                firstSheet = sh;
                                break;
                            }
                            if (firstSheet != null)
                            {
                                XAttribute nameAttr = firstSheet.Attribute("name");
                                if (nameAttr != null) result.SheetName = nameAttr.Value;
                            }
                        }
                    }
                }
                catch { }

                ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry != null)
                {
                    using (Stream s = sheetEntry.Open())
                    {
                        XDocument xSheet = XDocument.Load(s);
                        XNamespace ns = xSheet.Root != null ? xSheet.Root.Name.Namespace : XNamespace.None;

                        int rowLimit = 60;
                        int curRow = 0;

                        foreach (XElement row in xSheet.Descendants(ns + "row"))
                        {
                            curRow++;
                            if (curRow > rowLimit) break;

                            List<string> rowValues = new List<string>();
                            foreach (XElement c in row.Descendants(ns + "c"))
                            {
                                XAttribute typeAttr = c.Attribute("t");
                                string cellType = typeAttr != null ? typeAttr.Value : "";

                                XElement v = c.Element(ns + "v");
                                string rawVal = v != null ? v.Value : "";

                                string displayVal = rawVal;
                                if (cellType == "s" && rawVal.Length > 0)
                                {
                                    int ssIndex;
                                    if (int.TryParse(rawVal, out ssIndex) && ssIndex >= 0 && ssIndex < sharedStrings.Count)
                                    {
                                        displayVal = sharedStrings[ssIndex];
                                    }
                                }
                                else if (cellType == "b")
                                {
                                    displayVal = rawVal == "1" ? "DOĞRU" : "YANLIŞ";
                                }

                                rowValues.Add(displayVal);
                            }

                            if (rowValues.Count > 0)
                            {
                                result.Rows.Add(rowValues);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static ZipResult ParseZip(string filePath)
        {
            ZipResult result = new ZipResult();
            result.Entries = new List<ZipEntryInfo>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                long uncompressedTotal = 0;
                long compressedTotal = 0;
                int filesCount = 0;
                int dirsCount = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    bool isDir = entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\");
                    if (isDir)
                    {
                        dirsCount++;
                    }
                    else
                    {
                        filesCount++;
                        uncompressedTotal += entry.Length;
                        compressedTotal += entry.CompressedLength;
                    }

                    ZipEntryInfo info = new ZipEntryInfo();
                    info.Name = entry.FullName.TrimEnd('/', '\\');
                    info.IsDirectory = isDir;
                    info.Length = entry.Length;
                    info.CompressedLength = entry.CompressedLength;
                    info.LastWriteTime = entry.LastWriteTime.DateTime;

                    result.Entries.Add(info);
                }

                result.Entries.Sort(new Comparison<ZipEntryInfo>((a, b) =>
                {
                    if (a.IsDirectory != b.IsDirectory)
                    {
                        return a.IsDirectory ? -1 : 1;
                    }
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                }));

                result.TotalFiles = filesCount;
                result.TotalDirectories = dirsCount;
                result.TotalUncompressedSize = uncompressedTotal;
                result.TotalCompressedSize = compressedTotal;
            }

            return result;
        }

        public static PdfResult ParsePdf(string filePath)
        {
            PdfResult result = new PdfResult();
            result.Version = "1.x";
            result.PageCount = 1;
            result.Title = "";
            result.Author = "";
            result.Subject = "";
            result.Text = "";

            byte[] bytes;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Read up to 512KB for metadata & initial text (fast!)
                int toRead = (int)Math.Min(512 * 1024, fs.Length);
                bytes = new byte[toRead];
                fs.Read(bytes, 0, toRead);
            }

            string text = Encoding.ASCII.GetString(bytes);

            // 1. Version
            Match verMatch = Regex.Match(text, @"%PDF-(\d+\.\d+)");
            if (verMatch.Success) result.Version = verMatch.Groups[1].Value;

            // 2. Page Count
            Match countMatch = Regex.Match(text, @"/Type\s*/Pages.*?/Count\s+(\d+)", RegexOptions.Singleline);
            if (!countMatch.Success) countMatch = Regex.Match(text, @"/Count\s+(\d+)");
            if (countMatch.Success)
            {
                int p;
                if (int.TryParse(countMatch.Groups[1].Value, out p) && p > 0) result.PageCount = p;
            }
            else
            {
                MatchCollection pageMatches = Regex.Matches(text, @"/Type\s*/Page[^s]");
                if (pageMatches.Count > 0) result.PageCount = pageMatches.Count;
            }

            // 3. Metadata
            Match titleMatch = Regex.Match(text, @"/Title\s*\((.*?)\)");
            if (titleMatch.Success) result.Title = CleanPdfString(titleMatch.Groups[1].Value);

            Match authorMatch = Regex.Match(text, @"/Author\s*\((.*?)\)");
            if (authorMatch.Success) result.Author = CleanPdfString(authorMatch.Groups[1].Value);

            Match subjectMatch = Regex.Match(text, @"/Subject\s*\((.*?)\)");
            if (subjectMatch.Success) result.Subject = CleanPdfString(subjectMatch.Groups[1].Value);

            // 4. Extract text from first 5 streams max
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            int extractedChars = 0;
            int streamsParsed = 0;

            while (streamsParsed < 6 && (pos = text.IndexOf("stream", pos, StringComparison.Ordinal)) != -1)
            {
                pos += 6;
                if (pos < bytes.Length && bytes[pos] == '\r') pos++;
                if (pos < bytes.Length && bytes[pos] == '\n') pos++;

                int endStream = text.IndexOf("endstream", pos, StringComparison.Ordinal);
                if (endStream == -1) break;

                int length = endStream - pos;
                if (length > 0 && length < 100000)
                {
                    byte[] streamBytes = new byte[length];
                    Buffer.BlockCopy(bytes, pos, streamBytes, 0, length);

                    byte[] decompressed = TryDecompress(streamBytes);
                    byte[] targetBytes = decompressed != null ? decompressed : streamBytes;

                    string sContent = Encoding.UTF8.GetString(targetBytes);

                    MatchCollection arrayMatches = Regex.Matches(sContent, @"\[(.*?)\]\s*TJ");
                    foreach (Match m in arrayMatches)
                    {
                        string inner = m.Groups[1].Value;
                        MatchCollection innerTexts = Regex.Matches(inner, @"\((.*?)\)");
                        foreach (Match im in innerTexts)
                        {
                            string raw = CleanPdfString(im.Groups[1].Value);
                            if (raw.Length > 0)
                            {
                                sb.Append(raw);
                                extractedChars += raw.Length;
                            }
                        }
                        sb.Append(" ");
                    }

                    MatchCollection matches = Regex.Matches(sContent, @"\((.*?)\)\s*Tj");
                    foreach (Match m in matches)
                    {
                        string t = CleanPdfString(m.Groups[1].Value);
                        if (t.Length > 0)
                        {
                            sb.Append(t).Append(" ");
                            extractedChars += t.Length;
                        }
                    }

                    streamsParsed++;
                    if (extractedChars > 2000) break;
                }

                pos = endStream + 9;
            }

            result.Text = CleanExtractedText(sb.ToString().Trim());
            return result;
        }

        private static string CleanPdfString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\(", "(").Replace("\\)", ")").Replace("\\\\", "\\").Trim();
        }

        private static string CleanExtractedText(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return Regex.Replace(s, @"[ \t]+", " ").Trim();
        }

        private static byte[] TryDecompress(byte[] data)
        {
            try
            {
                int offset = 0;
                if (data.Length > 2 && data[0] == 0x78)
                {
                    offset = 2; // skip zlib header
                }

                using (MemoryStream ms = new MemoryStream(data, offset, data.Length - offset))
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                using (MemoryStream outMs = new MemoryStream())
                {
                    byte[] buf = new byte[4096];
                    int r;
                    while ((r = ds.Read(buf, 0, buf.Length)) > 0)
                    {
                        outMs.Write(buf, 0, r);
                        if (outMs.Length > 200000) break;
                    }
                    return outMs.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
