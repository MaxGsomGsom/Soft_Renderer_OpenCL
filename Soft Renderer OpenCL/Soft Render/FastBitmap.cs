using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{

    /// <summary>
    /// Класс представляет изображение в виде массива байт для быстрого доступа
    /// </summary>
    public class FastBitmap
    {
        Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero; //указатель на начало изображения в памяти
        BitmapData bitmapData = null;

        int cCount = 0; //количество байт на пиксель

        public byte[] Pixels { get; private set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }



        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="source">Оригинальное изображение</param>
        public FastBitmap(Bitmap source)
        {
            this.source = source;
        }


        /// <summary>
        /// Очистка изображения черным цветом
        /// </summary>
        public void ClearBlack()
        {
            if (Pixels != null) Array.Clear(Pixels, 0, Pixels.Length);
        }

        /// <summary>
        /// Скопировать пиксели изображения в массив
        /// </summary>
        public void CopyBytesFromSource()
        {
                Width = source.Width;
                Height = source.Height;

                Rectangle rect = new Rectangle(0, 0, Width, Height);

                Depth = Bitmap.GetPixelFormatSize(source.PixelFormat);
                cCount = Depth / 8; 

                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite, source.PixelFormat);

                Pixels = new byte[Width * Height * Depth / 8];
                Iptr = bitmapData.Scan0;

                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
        }

        /// <summary>
        /// Скопировать пиксели обратно в изображение
        /// </summary>
        public void ReturnBytesToSource()
        {
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);
                source.UnlockBits(bitmapData);
        }

        /// <summary>
        /// Получить пиксель
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;

            // индекс первого байта пикселя
            int i = ((y * Width) + x) * cCount;

            //если координаты выходят за рамки изображения
            if (i > Pixels.Length - cCount || i<0)
                return Color.White;

            //иначе получаем пиксель в зависимости от глубины цвета
            if (Depth == 32) 
            {
                clr = Color.FromArgb(Pixels[i + 3], Pixels[i + 2], Pixels[i + 1], Pixels[i]);
            }
            if (Depth == 24) 
            {
                clr = Color.FromArgb(Pixels[i + 2], Pixels[i + 1], Pixels[i]);
            }
            return clr;
        }

        /// <summary>
        /// Установить цвет пикселя
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            // индекс первого байта пикселя
            int i = ((y * Width) + x) * cCount;

            //если координаты выходят за рамки изображения
            if (i > Pixels.Length - cCount) return;

            //иначе устанавливаем пиксель в зависимости от глубины цвета
            if (Depth == 32)
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) 
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
        }
    }

}
