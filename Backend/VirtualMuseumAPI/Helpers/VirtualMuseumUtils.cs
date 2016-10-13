using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using VirtualMuseumAPI.Models;

namespace VirtualMuseumAPI.Helpers
{
    public static class VirtualMuseumUtils
    {

        public static T RandomIEnumerableElement<T>(this IEnumerable<T> source, Random rng)
        {
            T current = default(T);
            int count = 0;
            foreach (T element in source)
            {
                count++;
                if (rng.Next(count) == 0)
                {
                    current = element;
                }
            }
            return current;
        }

        public static bool IsValidImage(byte[] bytes)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                    Image.FromStream(ms);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

   
        public static byte[] CreateThumbnail(byte[] bytes, int nThumbWidth, int nThumbHeigth)
        {
            byte[] Ret;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                int nJpegQuality = 90;
                Image imgOri = Image.FromStream(ms);

                int nNewWidth = imgOri.Size.Width;
                int nNewHeight = imgOri.Size.Height;
                if (nNewWidth < nThumbWidth & nNewHeight < nThumbHeigth)
                {
                    //noop
                }
                else if (nNewWidth > nNewHeight)
                {
                    nNewHeight = Convert.ToInt32((double)nNewHeight * ((double)nThumbWidth / (double)nNewWidth));
                    nNewWidth = nThumbWidth;
                }
                else if (nNewWidth < nNewHeight)
                {
                    nNewWidth = Convert.ToInt32((double)nNewWidth * ((double)nThumbHeigth / (double)nNewHeight));
                    nNewHeight = nThumbHeigth;
                }
                else
                {
                    nNewWidth = nThumbWidth;
                    nNewHeight = nThumbHeigth;
                }

                Image imgThumb = new Bitmap(nNewWidth, nNewHeight);
                Graphics gr = Graphics.FromImage(imgThumb);
                gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                gr.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                gr.DrawImage(imgOri, 0, 0, nNewWidth, nNewHeight);
                System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                System.Drawing.Imaging.EncoderParameters ePars = new System.Drawing.Imaging.EncoderParameters(1);
                ePars.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, nJpegQuality);
                using (MemoryStream mst = new MemoryStream())
                {
                    imgThumb.Save(mst, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Ret = mst.ToArray();
                }
                imgThumb.Dispose();
                imgOri.Dispose();

            }
            return Ret;
        }        
    }
}