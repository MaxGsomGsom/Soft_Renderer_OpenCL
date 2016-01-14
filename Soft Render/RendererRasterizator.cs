using Soft_Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    public partial class Renderer
    {

        /// <summary>
        /// Растеризация, применение шейдера для одного горизонтального отрезка
        /// </summary>
        /// <param name="d1">Начало отрезка</param>
        /// <param name="d2">Конец отрезка</param>
        /// <param name="Shader">Функция-шейдер</param>
        /// <param name="param">Параметры для шейдера</param>
        private void RenderRasterLine(Dot d1, Dot d2, Action<Dot, int, int, double[,], double[,], double[,], double> Shader, bool needUV, bool needVN, double[,] p0, double[,] p1, double[,] p2, double p3)
        {
            //нахождение длины отрезка по Х
            double lengthX = Math.Abs(d1.x - d2.x);

            //нахождение шага растеризации
            Dot step = CalcRasterStep(d1, d2, lengthX, needUV, needVN);

            int xPrev = int.MinValue, zPrev = int.MinValue, yPrev = int.MinValue;

            Dot d;
            //небольшой отступ 0.01 от начала и конца линии, чтобы не было наложения
            for (double i = indent; i <= lengthX - indent; i += quality)
            {
                d = CalcRasterDot(d1, d2, step, i, needUV, needVN);

                //приводим к целочисленному виду, поскольку изображение состоит из целых пикселей
                int xInt = (int)(d.x + 0.5);
                int yInt = (int)(d.y + 0.5);

                if (optimization)
                {
                    int zInt = (int)(d.z + 0.5);

                    //оптимизация, чтобы не рисовать несколько раз 1 пиксель; проверка граничных условий
                    if ((xInt < halfWidth && yInt < halfHeight && xInt > -halfWidth && yInt > -halfHeight) &&
                           (!optimization || (xPrev != xInt || zPrev != zInt || yPrev != yInt)))
                    {
                        xPrev = xInt;
                        zPrev = zInt;
                        yPrev = yInt;


                        //шейдер, который просчитывает освещение и выводит изображение
                        Shader(d, xInt + halfWidth, yInt + halfHeight, p0, p1, p2, p3);
                    }
                }
                else
                {
                    //проверка граничных условий
                    if (xInt < halfWidth && yInt < halfHeight && xInt > -halfWidth && yInt > -halfHeight)
                    {

                        //шейдер, который просчитывает освещение и выводит изображение
                        Shader(d, xInt + halfWidth, yInt + halfHeight, p0, p1, p2, p3);
                    }
                }
            }
        }




        /// <summary>
        /// Растеризация и отрисовка полигонов
        /// </summary>
        /// <param name="Shader">Функция-шейдер</param>
        /// <param name="param">Параметры для шейдера</param>
        /// <param name="polygons"></param>
        /// <param name="dots"></param>
        private void RenderRasterPolygons(List<Polygon> polygons, List<Dot> dots, Action<Dot, int, int, double[,], double[,], double[,], double> Shader, bool needUV, bool needVN, double[,] p0, double[,] p1, double[,] p2, double p3)
        {
            for (int r = 0; r < polygons.Count; r++)
            {
                Dot upper = dots[polygons[r].d1];
                Dot mid = dots[polygons[r].d2];
                Dot down = dots[polygons[r].d3];

                ////если все точки лежат за границами экрана - пропускаем полигон
                //if (!((upper.x < halfWidth && upper.y < halfHeight && upper.x > -halfWidth && upper.y > -halfHeight) ||
                //    (mid.x < halfWidth && mid.y < halfHeight && mid.x > -halfWidth && mid.y > -halfHeight) ||
                //    (down.x < halfWidth && down.y < halfHeight && down.x > -halfWidth && down.y > -halfHeight))) continue;

                //вычисление нормали для всего полигона
                if (midNormales)
                {
                    CalcPolygonNormale(ref upper, ref mid, ref down);
                }

                //определение верхней, средней, нижней точки полигона
                Dot buf;
                if (mid.y < upper.y)
                {
                    buf = mid;
                    mid = upper;
                    upper = buf;
                }
                if (down.y < upper.y)
                {
                    buf = down;
                    down = upper;
                    upper = buf;
                }
                if (down.y < mid.y)
                {
                    buf = down;
                    down = mid;
                    mid = buf;
                }

                //просчет длин сторон полигона по оси Y 
                double lengthLongY = down.y - upper.y;
                double lengthShortUpY = mid.y - upper.y;
                double lengthShortDownY = down.y - mid.y;

                //простет шагов растеризации для трех сторон полигона
                Dot stepSizeLong = CalcRasterStep(upper, down, lengthLongY, needUV, needVN);
                Dot stepSizeShortUp = CalcRasterStep(upper, mid, lengthShortUpY, needUV, needVN);
                Dot stepSizeShortDown = CalcRasterStep(mid, down, lengthShortDownY, needUV, needVN);


                //координаты предыдущего отрисованного отрезка для оптимизации
                int longPrevX = int.MinValue, shortPrevX = int.MinValue, yPrev = int.MinValue;
                int longPrevZ = int.MinValue, shortPrevZ = int.MinValue;

                Dot longLineEnd;
                Dot shortLineEnd;
                //растеризация полигона горизонтальными отрезками
                //небольшой отступ 0.01 от начала и конца линии, чтобы не было наложения
                for (double i = indent; i <= lengthLongY - indent; i += quality)
                {
                    //вычисление конечных точек горизонтального отрезка
                    //конечная точка на длинной стороне полигона
                    longLineEnd = CalcRasterDot(upper, down, stepSizeLong, i, needUV, needVN);

                    //если растеризуем верхнюю половину полигона
                    if (i < lengthShortUpY)
                    {
                        shortLineEnd = CalcRasterDot(upper, mid, stepSizeShortUp, i, needUV, needVN);
                    }
                    //если растеризуем нижнюю половину полигона
                    else
                    {
                        double k = i - lengthShortUpY;
                        shortLineEnd = CalcRasterDot(mid, down, stepSizeShortDown, k, needUV, needVN);
                    }

                    if (optimization)
                    {
                        //приводим к целочисленному виду, поскольку изображение состоит из целых пикселей
                        int longX = (int)(longLineEnd.x + 0.5);
                        int shortX = (int)(shortLineEnd.x + 0.5);
                        int longZ = (int)(longLineEnd.z + 0.5);
                        int shortZ = (int)(shortLineEnd.z + 0.5);
                        int y = (int)(longLineEnd.y + 0.5);

                        //если все параметры эквивалентны предыдущим, то не будем отрисовывать линию
                        if ((longPrevX != longX || shortPrevX != shortX || yPrev != y ||
                                    longPrevZ != longZ || shortPrevZ != shortZ) && (longX != shortX || longZ != shortZ))
                        {
                            longPrevX = longX;
                            shortPrevX = shortX;
                            longPrevZ = longZ;
                            shortPrevZ = shortZ;
                            yPrev = y;

                            //растеризация и отрисовка горизонтального отрезка
                            RenderRasterLine(longLineEnd, shortLineEnd, Shader, needUV, needVN, p0, p1, p2, p3);

                        }
                    }
                    else
                    {
                        //растеризация и отрисовка горизонтального отрезка
                        RenderRasterLine(longLineEnd, shortLineEnd, Shader, needUV, needVN, p0, p1, p2, p3);
                    }


                }

            }
        }


        /// <summary>
        /// Растеризация и отрисовка только границ полигона
        /// </summary>
        /// <param name="Shader">Функция-шейдер</param>
        /// <param name="param">Параметры для шейдера</param>
        /// <param name="polygons"></param>
        /// <param name="dots"></param>
        private void RenderRasterPolygonWeb(List<Polygon> polygons, List<Dot> dots, Action<Dot, int, int, double[,], double[,], double[,], double> Shader, bool needUV, bool needVN, double[,] p0, double[,] p1, double[,] p2, double p3)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Dot d1 = dots[polygons[i].d1];
                Dot d2 = dots[polygons[i].d2];
                Dot d3 = dots[polygons[i].d3];


                //отрисовка трех границ полигона
                RenderRasterLine(d1, d2, Shader, needUV, needVN, p0, p1, p2, p3);
                RenderRasterLine(d1, d3, Shader, needUV, needVN, p0, p1, p2, p3);
                RenderRasterLine(d2, d3, Shader, needUV, needVN, p0, p1, p2, p3);
            }
        }


        /// <summary>
        /// Вычисляет шаг растеризации отрезков для всех координат относительно длины
        /// </summary>
        /// <param name="d1">Точка 1</param>
        /// <param name="d2">Точка 2</param>
        /// <param name="d3">Длина</param>
        /// <returns></returns>
        public Dot CalcRasterStep(Dot d1, Dot d2, double length, bool needUV, bool needVN)
        {
            Dot result;
            if (needVN)
            {
                result = new Dot(
                    (d1.x - d2.x) / length,
                    (d1.y - d2.y) / length,
                    (d1.z - d2.z) / length,
                    (d1.u - d2.u) / length,
                    (d1.v - d2.v) / length,
                    (d1.nx - d2.nx) / length,
                    (d1.ny - d2.ny) / length,
                    (d1.nz - d2.nz) / length);
            }
            else if (needUV)
            {
                result = new Dot(
                    (d1.x - d2.x) / length,
                    (d1.y - d2.y) / length,
                    (d1.z - d2.z) / length,
                    (d1.u - d2.u) / length,
                    (d1.v - d2.v) / length);
            }
            else
            {
                result = new Dot(
                    (d1.x - d2.x) / length,
                    (d1.y - d2.y) / length,
                    (d1.z - d2.z) / length);
            }

            return result;
        }



        /// <summary>
        /// Вычисление координат точки на отрезке
        /// </summary>
        /// <param name="d1">Начало отрезка</param>
        /// <param name="d2">Конец отрезка</param>
        /// <param name="stepSize">Размер шага растеризации</param>
        /// <param name="i">Номер шага</param>
        /// <returns></returns>
        public Dot CalcRasterDot(Dot d1, Dot d2, Dot stepSize, double i, bool needUV, bool needVN)
        {
            Dot result;
            if (needVN)
            {
                result = new Dot(
                    d1.x - i * stepSize.x,
                    d1.y - i * stepSize.y,
                    d1.z - i * stepSize.z,
                    d1.u - i * stepSize.u,
                    d1.v - i * stepSize.v,
                    d1.nx - i * stepSize.nx,
                    d1.ny - i * stepSize.ny,
                    d1.nz - i * stepSize.nz);
            }
            else if (needUV)
            {
                result = new Dot(
                    d1.x - i * stepSize.x,
                    d1.y - i * stepSize.y,
                    d1.z - i * stepSize.z,
                    d1.u - i * stepSize.u,
                    d1.v - i * stepSize.v);
            }
            else
            {
                result = new Dot(
                    d1.x - i * stepSize.x,
                    d1.y - i * stepSize.y,
                    d1.z - i * stepSize.z);
            }

            return result;

        }

     

    }
}
