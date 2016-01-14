using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{

    /// <summary>
    /// Текстурные координаты точки
    /// </summary>
    public class UV
    {
        public double u, v;

        /// <summary>
        /// Создать точку в текстурных координатах
        /// </summary>
        /// <param name="u">Координата U</param>
        /// <param name="v">Координата V</param>
        public UV(double u, double v)
        {
            this.u = u;
            this.v = v;
        }
    }
}
