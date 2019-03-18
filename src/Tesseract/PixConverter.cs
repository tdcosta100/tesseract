#if NETFULL

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
#if !NET20
using System.Windows.Media.Imaging;
#endif

namespace Tesseract
{
    /// <summary>
    /// Handles converting between different image formats supported by DotNet.
    /// </summary>
    public static class PixConverter {
        private static readonly BitmapToPixConverter bitmaptoPixConverter = new BitmapToPixConverter();
        private static readonly PixToBitmapConverter pixtoBitmapConverter = new PixToBitmapConverter();
#if !NET20
        private static readonly BitmapSourceToPixConverter bitmapSourceToPixConverter = new BitmapSourceToPixConverter();
        private static readonly PixToBitmapSourceConverter pixtoBitmapSourceConverter = new PixToBitmapSourceConverter();
#endif

        /// <summary>
        /// Converts the specified <paramref name="pix"/> to a Bitmap.
        /// </summary>
        /// <param name="pix">The source image to be converted.</param>
        /// <returns>The converted pix as a <see cref="Bitmap"/>.</returns>
        public static Bitmap ToBitmap(Pix pix)
        {
            return pixtoBitmapConverter.Convert(pix);
        }

        /// <summary>
        /// Converts the specified <paramref name="img"/> to a Pix.
        /// </summary>
        /// <param name="img">The source image to be converted.</param>
        /// <returns>The converted bitmap image as a <see cref="Pix"/>.</returns>
        public static Pix ToPix(Bitmap img)
        {
            return bitmaptoPixConverter.Convert(img);
        }

#if !NET20
        /// <summary>
        /// Converts the specified <paramref name="pix"/> to a Bitmap.
        /// </summary>
        /// <param name="pix">The source image to be converted.</param>
        /// <returns>The converted pix as a <see cref="BitmapSource"/>.</returns>
        public static BitmapSource ToBitmapSource(Pix pix)
        {
            return pixtoBitmapSourceConverter.Convert(pix);
        }

        /// <summary>
        /// Converts the specified <paramref name="img"/> to a Pix.
        /// </summary>
        /// <param name="img">The source image to be converted.</param>
        /// <returns>The converted bitmap image as a <see cref="Pix"/>.</returns>
        public static Pix ToPix(BitmapSource img)
        {
            return bitmapSourceToPixConverter.Convert(img);
        }
#endif
    }
}

#endif