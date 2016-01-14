using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    /// <summary>
    /// Нормаль к точке поверхности
    /// </summary>
    public class Normale
    {
        public double nx, ny, nz;

        /// <summary>
        /// Создать нормаль
        /// </summary>
        /// <param name="nx">Координата X</param>
        /// <param name="ny">Координата Y</param>
        /// <param name="nz">Координата Z</param>
        public Normale(double nx, double ny, double nz)
        {
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
        }
    }
}
