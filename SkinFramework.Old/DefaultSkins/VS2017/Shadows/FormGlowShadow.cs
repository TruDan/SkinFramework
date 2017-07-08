﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security;
using System.Windows.Forms;

namespace SkinFramework.DefaultSkins.VS2017.Shadows
{
    public class FormGlowShadow : FormShadowBase
    {
        public Color GlowColor { get; set; }

        public int GlowBlur { get; } = 30;
        public int GlowSpread { get; } = 5;

        public FormGlowShadow(Form targetForm, Color glowColor, int size, int blur, int spread = 0) : base(targetForm, size, (int)(WindowStyles.WS_EX_LAYERED | WindowStyles.WS_EX_TRANSPARENT | WindowStyles.WS_EX_NOACTIVATE))
        {
            GlowColor = glowColor;
            GlowBlur = blur;
            GlowSpread = spread;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PaintShadow();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Visible = true;
            PaintShadow();
        }

        protected override void PaintShadow()
        {
            using (Bitmap getShadow = DrawBlurBorder())
                SetBitmap(getShadow, 255);
        }

        protected override void ClearShadow()
        {
            Bitmap img = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(img);
            g.Clear(Color.Transparent);
            g.Flush();
            g.Dispose();
            SetBitmap(img, 255);
            img.Dispose();
        }

        #region Drawing methods

        [SecuritySafeCritical]
        private void SetBitmap(Bitmap bitmap, byte opacity)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");

            IntPtr screenDc = Win32Api.GetDC(IntPtr.Zero);
            IntPtr memDc = Win32Api.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = Win32Api.SelectObject(memDc, hBitmap);

                SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                POINT pointSource = new POINT(0, 0);
                POINT topPos = new POINT(Left, Top);
                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = Win32Api.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = Win32Api.AC_SRC_ALPHA
                };

                Win32Api.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32Api.ULW_ALPHA);
            }
            finally
            {
                Win32Api.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Win32Api.SelectObject(memDc, oldBitmap);
                    Win32Api.DeleteObject(hBitmap);
                }
                Win32Api.DeleteDC(memDc);
            }
        }

        private Bitmap DrawBlurBorder()
        {
            return (Bitmap)DrawOutsetShadow(0, 0, GlowBlur, GlowSpread, GlowColor, new Rectangle(0, 0, ClientRectangle.Width, ClientRectangle.Height));
        }

        private Image DrawOutsetShadow(int hShadow, int vShadow, int blur, int spread, Color color, Rectangle shadowCanvasArea)
        {
            Rectangle rOuter = shadowCanvasArea;
            Rectangle rInner = shadowCanvasArea;
            rInner.Offset(hShadow, vShadow);
            rInner.Inflate(-blur, -blur);
            rOuter.Inflate(spread, spread);
            rOuter.Offset(hShadow, vShadow);

            Rectangle originalOuter = rOuter;

            Bitmap img = new Bitmap(originalOuter.Width, originalOuter.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(img);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var currentBlur = 0;
            do
            {
                var transparency = (rOuter.Height - rInner.Height) / (double)(blur * 2 + spread * 2);
                var shadowColor = Color.FromArgb(((int)(200 * (transparency * transparency))), color);
                var rOutput = rInner;
                rOutput.Offset(-originalOuter.Left, -originalOuter.Top);

                DrawRoundedRectangle(g, rOutput, currentBlur, Pens.Transparent, shadowColor);
                rInner.Inflate(1, 1);
                currentBlur = (int)(blur * (1 - (transparency * transparency)));

            } while (rOuter.Contains(rInner));

            g.Flush();
            g.Dispose();

            return img;
        }

        private void DrawRoundedRectangle(Graphics g, Rectangle bounds, int cornerRadius, Pen drawPen, Color fillColor)
        {
            int strokeOffset = Convert.ToInt32(Math.Ceiling(drawPen.Width));
            bounds = Rectangle.Inflate(bounds, -strokeOffset, -strokeOffset);
            bounds.Offset(-GlowSpread, -GlowSpread);

            var gfxPath = new GraphicsPath();

            if (cornerRadius > 0)
            {
                gfxPath.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180, 90);
                gfxPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270, 90);
                gfxPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                gfxPath.AddArc(bounds.X, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            }
            else
            {
                gfxPath.AddRectangle(bounds);
            }

            gfxPath.CloseAllFigures();

            if (cornerRadius > 5)
            {
                using (SolidBrush b = new SolidBrush(fillColor))
                {
                    g.FillPath(b, gfxPath);
                }
            }
            if (drawPen != Pens.Transparent)
            {
                using (Pen p = new Pen(drawPen.Color))
                {
                    p.EndCap = p.StartCap = LineCap.Round;
                    g.DrawPath(p, gfxPath);
                }
            }
        }

        #endregion
    }
}