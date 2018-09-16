using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhaseUnwrapping
{
    public static class ImageUtilities
    {
        /// <summary>
        /// Converts 8 bit grayscale image to 2D double array. 
        /// Values are mapped [0, 255] => [<paramref name="minVal"/>, <paramref name="maxVal"/>]
        /// </summary>
        /// <param name="image"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static double[,] ToDouble2D(this BitmapSource image, double minVal = -Math.PI, double maxVal = Math.PI)
        {
            if (image.Format != PixelFormats.Gray8)
            {
                throw new ArgumentException(string.Format("Unsupported pixel format: {0}", image.Format));
            }

            int rows = image.PixelHeight;
            int cols = image.PixelWidth;

            // Init result array
            double[,] result = new double[rows, cols];

            // Copy pixels to temp array
            byte[] pixels = new byte[rows * cols];
            image.CopyPixels(pixels, cols, 0);

            // Calculate each pixel value
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    result[row, col] = Scale(pixels[row * cols + col], 0, 255, minVal, maxVal);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts double[,] array into 8 bit grayscale image. If <paramref name="image"/> is <see cref="WriteableBitmap"/> values are updated directly in the
        /// image else values are set in the new image which is then returned (since original cannot be modified)
        /// </summary>
        /// <param name="image"></param>
        /// <param name="data"></param>
        /// <returns>Either new image (if source is not writable) or source image with updated vales</returns>
        public static BitmapSource FromDouble2D(this BitmapSource image, double[,] data)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            WriteableBitmap result = null;
            if (image is WriteableBitmap)
            {
                result = image as WriteableBitmap;
            }
            else
            {
                result = new WriteableBitmap(cols, rows, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            }

            // Find min max of source data, used later for scaling
            double srcMin = data.Cast<double>().Min();
            double srcMax = data.Cast<double>().Max();

            // Init temp array of pixel values, later to be written into destination image
            byte[] pixels = new byte[rows * cols];

            // Compute pixel values
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    pixels[row * cols + col] = (byte)Scale(data[row, col], srcMin, srcMax, 0, 255);
                }
            }

            // Copy pixels to image
            Int32Rect roi = new Int32Rect(0, 0, cols, rows);
            result.WritePixels(roi, pixels, cols, 0);

            return result;
        }

        /// <summary>
        /// Scales <paramref name="value"/> which value runs from <paramref name="srcMin"/> to <paramref name="srcMax"/>
        /// to run from <paramref name="dstMin"/> to <paramref name="dstMax"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="srcMin"></param>
        /// <param name="srcMax"></param>
        /// <param name="dstMin"></param>
        /// <param name="dstMax"></param>
        /// <returns></returns>
        private static double Scale(double value, double srcMin, double srcMax, double dstMin, double dstMax)
        {
            return (srcMin * dstMax - srcMax * dstMin + dstMin * value - dstMax * value) / (srcMin - srcMax);
        }
    }
}
