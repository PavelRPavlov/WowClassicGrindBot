﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Libs.Cursor
{
    public static class CursorClassifier
    {
        private static Dictionary<CursorClassification, ulong> imageHashes = new Dictionary<CursorClassification, ulong>()
        {
            {CursorClassification.Kill, 9286546093378506253},
            {CursorClassification.Loot, 16205332705670085656},
            {CursorClassification.None, 4645529528554094592},
            {CursorClassification.Skin, 13901748381153107456},
            {CursorClassification.Mine, 4669700909741929478 },
            {CursorClassification.Herb, 4683320813727784960 }
        };

        public static Bitmap Classify(out CursorClassification classification)
        {
            var result = new Bitmap(32, 32);
            try
            {
                NativeMethods.CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(NativeMethods.CURSORINFO));

                using (var g = Graphics.FromImage(result))
                {
                    if (NativeMethods.GetCursorInfo(out pci))
                    {
                        if (pci.flags == NativeMethods.CURSOR_SHOWING)
                        {
                            var hdc = g.GetHdc();
                            NativeMethods.DrawIconEx(hdc, 0, 0, pci.hCursor, 0, 0, 0, IntPtr.Zero, NativeMethods.DI_NORMAL);
                            g.ReleaseHdc();
                        }
                    }
                }

                var hash = ImageHashing.AverageHash(result);
                //logger.LogInformation("Hash: " + hash);

                var matching = imageHashes.Select(i => (similarity: ImageHashing.Similarity(hash, i.Value), imagehash: i))
                    .OrderByDescending(t => t.similarity)
                    .First();

                classification = matching.imagehash.Key;
                //System.Diagnostics.logger.LogInformation(classification);
                return result;
            }
            catch
            {
                classification = CursorClassification.Unknown;
                return result;
            }
        }
    }
}