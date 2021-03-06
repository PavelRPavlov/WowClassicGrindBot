﻿using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;

namespace Libs
{
    public class WowScreen : IColorReader
    {
        private readonly ILogger logger;

        public delegate void ScreenChangeEventHandler(object sender, ScreenChangeEventArgs args);

        public event ScreenChangeEventHandler? OnScreenChanged;

        public int Size { get; set; } = 1024;

        public WowScreen(ILogger logger)
        {
            this.logger = logger;
        }

        public static Bitmap GetAddonBitmap(int width = 500, int height = 200)
        {
            var bmpScreen = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }

        public Color GetColorAt(Point pos, Bitmap bmp)
        {
            var color = bmp.GetPixel(pos.X, pos.Y);

            return color;
        }

        public void DoScreenshot(NpcNameFinder npcNameFinder)
        {
            try
            {
                var npcs = npcNameFinder.RefreshNpcPositions();
                var bitmap = npcNameFinder.Screenshot.Bitmap;

                using (var gr = Graphics.FromImage(bitmap))
                {
                    if (npcs.Count > 0)
                    {
                        var margin = 10;

                        using (var whitePen = new Pen(Color.White, 3))
                        {
                            npcs.ForEach(n => gr.DrawEllipse(whitePen, new Rectangle(n.ClickPoint.X - (margin / 2), n.ClickPoint.Y - (margin / 2), margin, margin)));
                        }
                    }
                    using (var blackPen = new SolidBrush(Color.Black))
                    {
                        gr.FillRectangle(blackPen, new Rectangle(new Point(bitmap.Width / 15, bitmap.Height / 40), new Size(bitmap.Width / 15, bitmap.Height / 40)));
                    }
                }

                this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(npcNameFinder.Screenshot.ToBase64(Size)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            Thread.Sleep(1);
        }
    }
}