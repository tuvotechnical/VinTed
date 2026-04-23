using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace VinTed
{
    /// <summary>
    /// Helper class tạo icon cho Ribbon Button từ Iconify API hoặc fallback vẽ tay.
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// Tải SVG từ Iconify API và render thành Bitmap.
        /// Fallback: vẽ icon đơn giản nếu API không khả dụng.
        /// </summary>
        public static stdole.IPictureDisp CreateIconFromIconify(string iconName, int size, System.Drawing.Color foreColor, System.Drawing.Color backColor)
        {
            try
            {
                string svgData = DownloadSvgFromIconify(iconName);
                if (!String.IsNullOrEmpty(svgData))
                {
                    Bitmap bmp = RenderSvgPathToBitmap(svgData, size, foreColor, backColor);
                    if (bmp != null)
                    {
                        return ConvertBitmapToIPictureDisp(bmp);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback silently
            }

            // Fallback: vẽ icon chữ cái đầu
            return CreateFallbackIcon(iconName, size, foreColor, backColor);
        }

        private static string DownloadSvgFromIconify(string iconName)
        {
            try
            {
                string url = String.Format("https://api.iconify.design/{0}.svg", iconName);
                WebRequest request = WebRequest.Create(url);
                request.Timeout = 5000;
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Parse SVG path data đơn giản và vẽ lên Bitmap.
        /// Hỗ trợ cơ bản: M, L, H, V, Z, C commands.
        /// </summary>
        private static Bitmap RenderSvgPathToBitmap(string svgContent, int size, System.Drawing.Color foreColor, System.Drawing.Color backColor)
        {
            try
            {
                // Trích xuất viewBox
                float vbWidth = 24, vbHeight = 24;
                int vbStart = svgContent.IndexOf("viewBox=\"");
                if (vbStart >= 0)
                {
                    vbStart += 9;
                    int vbEnd = svgContent.IndexOf("\"", vbStart);
                    if (vbEnd > vbStart)
                    {
                        string vb = svgContent.Substring(vbStart, vbEnd - vbStart);
                        string[] parts = vb.Split(' ');
                        if (parts.Length == 4)
                        {
                            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out vbWidth);
                            float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out vbHeight);
                        }
                    }
                }

                // Trích xuất path data
                string pathData = ExtractPathData(svgContent);
                if (String.IsNullOrEmpty(pathData))
                {
                    return null;
                }

                GraphicsPath gPath = ParseSvgPath(pathData);
                if (gPath == null || gPath.PointCount == 0)
                {
                    return null;
                }

                Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(backColor);

                    // Scale path to fit
                    float scale = Math.Min((float)size / vbWidth, (float)size / vbHeight) * 0.85f;
                    float offsetX = (size - vbWidth * scale) / 2f;
                    float offsetY = (size - vbHeight * scale) / 2f;

                    System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();
                    matrix.Translate(offsetX, offsetY);
                    matrix.Scale(scale, scale);
                    gPath.Transform(matrix);

                    using (SolidBrush brush = new SolidBrush(foreColor))
                    {
                        g.FillPath(brush, gPath);
                    }
                }
                return bmp;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ExtractPathData(string svg)
        {
            int idx = svg.IndexOf(" d=\"");
            if (idx < 0)
            {
                idx = svg.IndexOf(" d='");
            }
            if (idx < 0) return null;

            idx += 4;
            char quote = svg[idx - 1];
            int end = svg.IndexOf(quote, idx);
            if (end < 0) return null;

            return svg.Substring(idx, end - idx);
        }

        private static GraphicsPath ParseSvgPath(string pathData)
        {
            GraphicsPath path = new GraphicsPath();
            try
            {
                // Tokenize
                string cleaned = pathData.Replace(",", " ");
                System.Collections.Generic.List<string> tokens = new System.Collections.Generic.List<string>();

                string current = "";
                for (int i = 0; i < cleaned.Length; i++)
                {
                    char c = cleaned[i];
                    if (Char.IsLetter(c))
                    {
                        if (current.Trim().Length > 0) tokens.Add(current.Trim());
                        tokens.Add(c.ToString());
                        current = "";
                    }
                    else if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    {
                        if (current.Trim().Length > 0) tokens.Add(current.Trim());
                        current = "";
                    }
                    else if (c == '-' && current.Trim().Length > 0 && !current.Trim().EndsWith("e", StringComparison.OrdinalIgnoreCase))
                    {
                        if (current.Trim().Length > 0) tokens.Add(current.Trim());
                        current = c.ToString();
                    }
                    else
                    {
                        current += c;
                    }
                }
                if (current.Trim().Length > 0) tokens.Add(current.Trim());

                float cx = 0, cy = 0;
                float startX = 0, startY = 0;
                int ti = 0;

                while (ti < tokens.Count)
                {
                    string cmd = tokens[ti];
                    ti++;

                    if (cmd == "M" && ti + 1 < tokens.Count)
                    {
                        cx = ParseFloat(tokens[ti++]);
                        cy = ParseFloat(tokens[ti++]);
                        startX = cx; startY = cy;
                        path.StartFigure();
                    }
                    else if (cmd == "m" && ti + 1 < tokens.Count)
                    {
                        cx += ParseFloat(tokens[ti++]);
                        cy += ParseFloat(tokens[ti++]);
                        startX = cx; startY = cy;
                        path.StartFigure();
                    }
                    else if (cmd == "L" && ti + 1 < tokens.Count)
                    {
                        while (ti + 1 < tokens.Count && !IsCommand(tokens[ti]))
                        {
                            float nx = ParseFloat(tokens[ti++]);
                            float ny = ParseFloat(tokens[ti++]);
                            path.AddLine(cx, cy, nx, ny);
                            cx = nx; cy = ny;
                        }
                    }
                    else if (cmd == "l" && ti + 1 < tokens.Count)
                    {
                        while (ti + 1 < tokens.Count && !IsCommand(tokens[ti]))
                        {
                            float dx = ParseFloat(tokens[ti++]);
                            float dy = ParseFloat(tokens[ti++]);
                            path.AddLine(cx, cy, cx + dx, cy + dy);
                            cx += dx; cy += dy;
                        }
                    }
                    else if (cmd == "H" && ti < tokens.Count)
                    {
                        float nx = ParseFloat(tokens[ti++]);
                        path.AddLine(cx, cy, nx, cy);
                        cx = nx;
                    }
                    else if (cmd == "h" && ti < tokens.Count)
                    {
                        float dx = ParseFloat(tokens[ti++]);
                        path.AddLine(cx, cy, cx + dx, cy);
                        cx += dx;
                    }
                    else if (cmd == "V" && ti < tokens.Count)
                    {
                        float ny = ParseFloat(tokens[ti++]);
                        path.AddLine(cx, cy, cx, ny);
                        cy = ny;
                    }
                    else if (cmd == "v" && ti < tokens.Count)
                    {
                        float dy = ParseFloat(tokens[ti++]);
                        path.AddLine(cx, cy, cx, cy + dy);
                        cy += dy;
                    }
                    else if ((cmd == "C" || cmd == "c") && ti + 5 < tokens.Count)
                    {
                        bool relative = cmd == "c";
                        while (ti + 5 < tokens.Count && !IsCommand(tokens[ti]))
                        {
                            float x1 = ParseFloat(tokens[ti++]);
                            float y1 = ParseFloat(tokens[ti++]);
                            float x2 = ParseFloat(tokens[ti++]);
                            float y2 = ParseFloat(tokens[ti++]);
                            float x3 = ParseFloat(tokens[ti++]);
                            float y3 = ParseFloat(tokens[ti++]);
                            if (relative)
                            {
                                x1 += cx; y1 += cy;
                                x2 += cx; y2 += cy;
                                x3 += cx; y3 += cy;
                            }
                            path.AddBezier(cx, cy, x1, y1, x2, y2, x3, y3);
                            cx = x3; cy = y3;
                        }
                    }
                    else if (cmd == "Z" || cmd == "z")
                    {
                        path.CloseFigure();
                        cx = startX; cy = startY;
                    }
                }
            }
            catch (Exception)
            {
                // Return whatever we have
            }
            return path;
        }

        private static bool IsCommand(string s)
        {
            if (String.IsNullOrEmpty(s) || s.Length != 1) return false;
            char c = s[0];
            return Char.IsLetter(c);
        }

        private static float ParseFloat(string s)
        {
            float val;
            float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val);
            return val;
        }

        private static stdole.IPictureDisp CreateFallbackIcon(string iconName, int size, System.Drawing.Color foreColor, System.Drawing.Color backColor)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(backColor);

                // Vẽ icon search đơn giản cho Find & Replace
                using (Pen pen = new Pen(foreColor, 2f))
                {
                    float pad = size * 0.2f;
                    float circleSize = size * 0.45f;
                    g.DrawEllipse(pen, pad, pad, circleSize, circleSize);
                    float cx2 = pad + circleSize * 0.85f;
                    float cy2 = pad + circleSize * 0.85f;
                    g.DrawLine(pen, cx2, cy2, size - pad, size - pad);
                }
            }
            return ConvertBitmapToIPictureDisp(bmp);
        }

        public static stdole.IPictureDisp ConvertBitmapToIPictureDisp(Bitmap bitmap)
        {
            return (stdole.IPictureDisp)Support.ImageToIPictureDisp(bitmap);
        }

        // Hỗ trợ convert Image -> IPictureDisp qua AxHost
        private sealed class Support : System.Windows.Forms.AxHost
        {
            private Support() : base(String.Empty) { }

            public static stdole.IPictureDisp ImageToIPictureDisp(Image image)
            {
                return (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
            }
        }
    }
}
