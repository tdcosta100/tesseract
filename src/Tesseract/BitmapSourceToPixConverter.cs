#if NETFULL && !NET20

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tesseract
{
    /// <summary>
    /// Converts a <see cref="BitmapSource"/> to a <see cref="Pix"/>.
    /// </summary>
    public class BitmapSourceToPixConverter
    {
        public BitmapSourceToPixConverter()
        {
        }

        /// <summary>
        /// Converts the specified <paramref name="img"/> to a <see cref="Pix"/>.
        /// </summary>
        /// <param name="img">The source image to be converted.</param>
        /// <returns>The converted pix.</returns>
        public Pix Convert(BitmapSource img)
        {
            var pixDepth = GetPixDepth(img.Format);
            var pix = Pix.Create(img.PixelWidth, img.PixelHeight, pixDepth);            
            pix.XRes = (int) Math.Round(img.DpiX);
            pix.YRes = (int) Math.Round(img.DpiY);

            WriteableBitmap imgData = new WriteableBitmap(img);
            PixData pixData = null;

            try {
                // TODO: Set X and Y resolution
                if (img.Format.ToString().StartsWith("Indexed")) {
                    CopyColormap(img, pix);
                }

                // transfer data
                imgData.Lock();
                pixData = pix.GetData();

                if (imgData.Format.BitsPerPixel == 32) {
                    TransferDataFormat32bppArgb(imgData, pixData);
                } else if (imgData.Format.BitsPerPixel == 24) {
                    TransferDataFormat24bppRgb(imgData, pixData);
                } else if (imgData.Format.ToString().StartsWith("Indexed")) {
                    TransferDataFormatIndexed(imgData, pixData, imgData.Format.BitsPerPixel);
                }

                return pix;
            } catch (Exception) {
                pix.Dispose();
                throw;
            } finally {
                imgData.Unlock();
            }
        }

        private void CopyColormap(BitmapSource img, Pix pix)
        {
            var imgPalette = img.Palette;
            var imgPaletteEntries = imgPalette.Colors;
            var pixColormap = PixColormap.Create(pix.Depth);
            try {
                for (int i = 0; i < imgPaletteEntries.Count; i++) {
                    if (!pixColormap.AddColor((PixColor)imgPaletteEntries[i])) {
                        throw new InvalidOperationException(String.Format("Failed to add colormap entry {0}.", i));
                    }
                }
                pix.Colormap = pixColormap;
            } catch (Exception) {
                pixColormap.Dispose();
                throw;
            }
        }

        private int GetPixDepth(PixelFormat pixelFormat)
        {
            var acceptedBitsPerPixel = new int[] { 1, 8, 24, 32 };

            if (acceptedBitsPerPixel.Contains(pixelFormat.BitsPerPixel)) {
                return pixelFormat.BitsPerPixel;
            }

            throw new InvalidOperationException(String.Format("Source bitmap's pixel format {0} is not supported.", pixelFormat));
        }

        private unsafe void TransferDataFormat32bppArgb(WriteableBitmap imgData, PixData pixData)
        {
            var height = imgData.PixelHeight;
            var width = imgData.PixelWidth;
            var sourceBuffer = imgData.BackBuffer;
            var sourceStride = imgData.BackBufferStride;
            var targetBuffer = pixData.Data;
            var targetStride = pixData.WordsPerLine;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, loopState, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            byte* imgLine = (byte*)sourceBuffer + (y * sourceStride);
                            uint* pixLine = (uint*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                byte* pixelPtr = imgLine + (x << 2);
                                byte blue = pixelPtr[0];
                                byte green = pixelPtr[1];
                                byte red = pixelPtr[2];
                                byte alpha = pixelPtr[3];
                                PixData.SetDataFourByte(pixLine, x, BitmapHelper.EncodeAsRGBA(red, green, blue, alpha));
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }

        private unsafe void TransferDataFormat24bppRgb(WriteableBitmap imgData, PixData pixData)
        {
            var height = imgData.PixelHeight;
            var width = imgData.PixelWidth;
            var sourceBuffer = imgData.BackBuffer;
            var sourceStride = imgData.BackBufferStride;
            var targetBuffer = pixData.Data;
            var targetStride = pixData.WordsPerLine;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, loopState, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            byte* imgLine = (byte*)sourceBuffer + (y * sourceStride);
                            uint* pixLine = (uint*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                byte* pixelPtr = imgLine + x * 3;
                                byte blue = pixelPtr[0];
                                byte green = pixelPtr[1];
                                byte red = pixelPtr[2];
                                byte alpha = 0xFF;
                                PixData.SetDataFourByte(pixLine, x, BitmapHelper.EncodeAsRGBA(red, green, blue, alpha));
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }

        private unsafe void TransferDataFormatIndexed(WriteableBitmap imgData, PixData pixData, int bitsPerPixel)
        {
            var height = imgData.PixelHeight;
            var width = (int)Math.Ceiling(imgData.PixelWidth * bitsPerPixel / 8.0);

            var sourceBuffer = imgData.BackBuffer;
            var sourceStride = imgData.BackBufferStride;
            var targetBuffer = pixData.Data;
            var targetStride = pixData.WordsPerLine;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, loopState, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            byte* imgLine = (byte*)sourceBuffer + (y * sourceStride);
                            uint* pixLine = (uint*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                byte pixelVal = *(imgLine + x);
                                PixData.SetDataByte(pixLine, x, pixelVal);
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }
    }
}
#endif