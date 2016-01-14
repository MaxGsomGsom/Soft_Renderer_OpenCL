using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    ///// <summary>
    ///// Точка в трехмерном пространстве
    ///// </summary>
    public struct Dot
    {
        public double x, y, z;
        public double u, v;
        public double nx, ny, nz;

        public Dot(double x, double y, double z, double u = 0, double v = 0, double nx = 0, double ny = 0, double nz = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.u = u;
            this.v = v;
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
        }


        public Dot(Dot d, UV uv, Normale n)
        {
            this.x = d.x;
            this.y = d.y;
            this.z = d.z;
            this.u = uv.u;
            this.v = uv.v;
            this.nx = n.nx;
            this.ny = n.ny;
            this.nz = n.nz;
        }


    }

}
