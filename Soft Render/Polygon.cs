using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    /// <summary>
    /// Треугольник в трехмерном пространстве
    /// </summary>
    public class Polygon
    {
        public int d1, d2, d3;
        public int uv1, uv2, uv3;
        public int vn1, vn2, vn3;

        /// <summary>
        /// Создать треугольник
        /// </summary>
        /// <param name="d1">Указатель на точку 1 в массиве точек</param>
        /// <param name="d2">Указатель на точку 2 в массиве точек</param>
        /// <param name="d3">Указатель на точку 3 в массиве точек</param>
        public Polygon(int d1, int d2, int d3)
        {
            this.d1 = d1;
            this.d2 = d2;
            this.d3 = d3;
        }

        /// <summary>
        /// Задать указатели на текстурные координаты точек в массиве UV
        /// </summary>
        /// <param name="uv1">Указатель на текстурные координаты 1 в массиве UV</param>
        /// <param name="uv2">Указатель на текстурные координаты 2 в массиве UV</param>
        /// <param name="uv3">Указатель на текстурные координаты 3 в массиве UV</param>
        public void SetUVsPointers(int uv1, int uv2, int uv3)
        {
            this.uv1 = uv1;
            this.uv2 = uv2;
            this.uv3 = uv3;
        }

        /// <summary>
        /// Задать указатели на нормали точек в массиве
        /// </summary>
        /// <param name="vn1">Задать указатели на нормаль точки 1</param>
        /// <param name="vn2">Задать указатели на нормаль точки 2</param>
        /// <param name="vn3">Задать указатели на нормаль точки 3</param>
        public void SetNormalesPointers(int vn1, int vn2, int vn3)
        {
            this.vn1 = vn1;
            this.vn2 = vn2;
            this.vn3 = vn3;
        }
    }
}
