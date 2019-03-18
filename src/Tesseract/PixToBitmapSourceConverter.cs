#if NETFULL && !NET20

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tesseract
{
    public class PixToBitmapSourceConverter
    {
        public BitmapSource Convert(Pix pix, bool includeAlpha = false)
        {
            var pixelFormat = GetPixelFormat(pix);
            var depth = pix.Depth;
            var img = new WriteableBitmap(pix.Width, pix.Height, 96, 96, pixelFormat, (pix.Colormap?.Count > 0) ? new BitmapPalette(new List<Color>()) : null);

            PixData pixData = null;
            try {
                // TODO: Set X and Y resolution
                // transfer pixel data
                if (img.Format.ToString().StartsWith("Indexed") && pix.Colormap?.Count > 0) {
                    TransferPalette(pix, img);
                }

                // transfer data
                img.Lock();

                pixData = pix.GetData();
                
                if (depth == 32) {
                    TransferData32(pixData, img, includeAlpha ? 0 : 255);
                } else if (depth == 16) {
                    TransferData16(pixData, img);
                } else if (depth <= 8) {
                    TransferDataIndexed(pixData, img);
                }
                return img;
            } catch (Exception) {
                throw;
            } finally {
                img.Unlock();
            }
        }

        private unsafe void TransferData32(PixData pixData, WriteableBitmap imgData, int alphaMask)
        {
            var imgFormat = imgData.Format;
            var height = imgData.PixelHeight;
            var width = imgData.PixelWidth;

            var sourceBuffer = pixData.Data;
            var sourceStride = pixData.WordsPerLine;
            var targetBuffer = imgData.BackBuffer;
            var targetStride = imgData.BackBufferStride;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, state, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            uint* pixLine = (uint*)sourceBuffer + (y * sourceStride);
                            byte* imgLine = (byte*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                var pixVal = PixColor.FromRgba(pixLine[x]);

                                byte* pixelPtr = imgLine + (x << 2);
                                pixelPtr[0] = pixVal.Blue;
                                pixelPtr[1] = pixVal.Green;
                                pixelPtr[2] = pixVal.Red;
                                pixelPtr[3] = (byte)(alphaMask | pixVal.Alpha); // Allow user to include alpha or not
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }

        private unsafe void TransferData16(PixData pixData, WriteableBitmap imgData)
        {
            var imgFormat = imgData.Format;
            var height = imgData.PixelHeight;
            var width = imgData.PixelWidth;

            var sourceBuffer = pixData.Data;
            var sourceStride = pixData.WordsPerLine;
            var targetBuffer = imgData.BackBuffer;
            var targetStride = imgData.BackBufferStride;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, state, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            uint* pixLine = (uint*)sourceBuffer + (y * sourceStride);
                            ushort* imgLine = (ushort*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                ushort pixVal = (ushort)PixData.GetDataTwoByte(pixLine, x);

                                imgLine[x] = pixVal;
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }

        private unsafe void TransferDataIndexed(PixData pixData, WriteableBitmap imgData)
        {
            var imgFormat = imgData.Format;
            var bitsPerPixel = imgData.Format.BitsPerPixel;
            var height = imgData.PixelHeight;
            var width = (int)Math.Ceiling(imgData.PixelWidth * bitsPerPixel / 8.0);

            var sourceBuffer = pixData.Data;
            var sourceStride = pixData.WordsPerLine;
            var targetBuffer = imgData.BackBuffer;
            var targetStride = imgData.BackBufferStride;

            Parallel.ForEach(
                source: Partitioner.Create(0, height),
                localInit: () => 0,
                body:
                    (partition, state, index) => {
                        var yMin = partition.Item1;
                        var yMax = partition.Item2;

                        for (int y = yMin; y < yMax; y++) {
                            uint* pixLine = (uint*)sourceBuffer + (y * sourceStride);
                            byte* imgLine = (byte*)targetBuffer + (y * targetStride);

                            for (int x = 0; x < width; x++) {
                                byte pixVal = (byte)PixData.GetDataByte(pixLine, x);

                                imgLine[x] = pixVal;
                            }
                        }

                        return index;
                    },
                localFinally: _ => { }
            );
        }

        private void TransferPalette(Pix pix, BitmapSource img)
        {
            var palete = img.Palette;
            var maxColors = palete.Colors.Count;
            var lastColor = maxColors - 1;
            var colormap = pix.Colormap;
            if (colormap != null && colormap.Count <= maxColors) {
                var colormapCount = colormap.Count;
                for (int i = 0; i < colormapCount; i++) {
                    palete.Colors.Add((Color)colormap[i]);
                }
            } else {
                for (int i = 0; i < maxColors; i++) {
                    var value = (byte)(i * 255 / lastColor);
                    palete.Colors.Add(Color.FromArgb(value, value, value, 0xFF));
                }
            }
        }


        private PixelFormat GetPixelFormat(Pix pix)
        {
            switch (pix.Depth) {
                case 1: return PixelFormats.Indexed1;
                case 8: return PixelFormats.Indexed8;
                case 16: return PixelFormats.Gray16;
                case 32: return PixelFormats.Bgra32;
                default: throw new InvalidOperationException(String.Format("Pix depth {0} is not supported.", pix.Depth));
            }
        }
    }
}

#endif