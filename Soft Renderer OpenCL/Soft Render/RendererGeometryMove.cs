using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    public partial class Renderer
    {

        //масштаб
        double zoom = 1;

        //углы поворота модели
        double angleY = 0;
        double angleX = 0;
        double angleZ = 0;

        //положение модели
        double posX = 0;
        double posY = 0;
        double posZ = 0;


        double perspectiveZ = 1; //применение перспективы по оси Z


        public double LightX
        {
            get { return light.x; }
            set
            {
                light = new Dot(light.x + value, light.y, light.z);
            }
        }

        public double LightZ
        {
            get { return light.z; }
            set
            {
                light = new Dot(light.x, light.y, light.z + value);
            }
        }

        /// <summary>
        /// Масштаб изображения
        /// </summary>
        public double Zoom
        {
            get
            {
                return zoom;
            }

            set
            {
                double dZoom = value / zoom;

                for (int k = 0; k < dots.Count; k++)
                {
                    //новая координата = старая координата * (новый масштаб / старый масштаб)
                    dots[k] = new Dot(
                        dots[k].x * dZoom,
                        dots[k].y * dZoom,
                        dots[k].z * dZoom,
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        dots[k].ny,
                        dots[k].nz);

                }
                zoom = value;
                if (netMode) RenderingServer.NetSendObject(zoom, NetData.Zoom, client.GetStream());
            }

        }

        /// <summary>
        /// Ширина изображения
        /// </summary>
        public int Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
            }
        }

        /// <summary>
        /// Высота изображения
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }

            set
            {
                height = value;
            }
        }

        /// <summary>
        /// Угол поворота модели по оси Y
        /// </summary>
        public double AngleY
        {
            get
            {
                return angleY;
            }

            set
            {
                double dAngleY = value - angleY;

                for (int k = 0; k < dots.Count; k++)
                {
                    dots[k] = new Dot(
                        Math.Cos(dAngleY) * dots[k].x + Math.Sin(dAngleY) * dots[k].z,
                        dots[k].y,
                        Math.Cos(dAngleY) * dots[k].z - Math.Sin(dAngleY) * dots[k].x,
                        dots[k].u,
                        dots[k].v,
                        Math.Cos(dAngleY) * dots[k].nx + Math.Sin(dAngleY) * dots[k].nz,
                        dots[k].ny,
                        Math.Cos(dAngleY) * dots[k].nz - Math.Sin(dAngleY) * dots[k].nx);
                }
                angleY = value;
                if (netMode) RenderingServer.NetSendObject(angleY, NetData.AngleY, client.GetStream());
            }
        }

        /// <summary>
        /// Угол поворота модели по оси X
        /// </summary>
        public double AngleX
        {
            get
            {
                return angleX;
            }

            set
            {
                double dAngleX = value - angleX;

                for (int k = 0; k < dots.Count; k++)
                {

                    dots[k] = new Dot(
                        dots[k].x,
                        Math.Cos(dAngleX) * dots[k].y - Math.Sin(dAngleX) * dots[k].z,
                        Math.Cos(dAngleX) * dots[k].z + Math.Sin(dAngleX) * dots[k].y,
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        Math.Cos(dAngleX) * dots[k].ny - Math.Sin(dAngleX) * dots[k].nz,
                        Math.Cos(dAngleX) * dots[k].nz + Math.Sin(dAngleX) * dots[k].ny);

                }
                angleX = value;
                if (netMode) RenderingServer.NetSendObject(angleX, NetData.AngleX, client.GetStream());
            }
        }


        /// <summary>
        /// Угол поворота модели по оси Z
        /// </summary>
        public double AngleZ
        {
            get
            {
                return angleZ;
            }

            set
            {
                double dAngleZ = value - angleZ;

                for (int k = 0; k < dots.Count; k++)
                {

                    dots[k] = new Dot(
                        Math.Cos(dAngleZ) * dots[k].x - Math.Sin(dAngleZ) * dots[k].y,
                        Math.Cos(dAngleZ) * dots[k].y + Math.Sin(dAngleZ) * dots[k].x,
                        dots[k].z,
                        dots[k].u,
                        dots[k].v,
                        Math.Cos(dAngleZ) * dots[k].nx - Math.Sin(dAngleZ) * dots[k].ny,
                        Math.Cos(dAngleZ) * dots[k].ny + Math.Sin(dAngleZ) * dots[k].nx,
                        dots[k].nz);

                }
                angleZ = value;
                if (netMode) RenderingServer.NetSendObject(angleZ, NetData.AngleZ, client.GetStream());
            }
        }


        /// <summary>
        /// Координата X
        /// </summary>
        public double PosX
        {
            get
            {
                return posX;
            }

            set
            {
                double dPosX = value - posX;

                for (int k = 0; k < dots.Count; k++)
                {
                    dots[k] = new Dot(
                        dots[k].x + dPosX,
                        dots[k].y,
                        dots[k].z,
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        dots[k].ny,
                        dots[k].nz);
                }

                posX = value;
                if (netMode) RenderingServer.NetSendObject(posX, NetData.PosX, client.GetStream());
            }
        }

        /// <summary>
        /// Координата Y
        /// </summary>
        public double PosY
        {
            get
            {
                return posY;
            }

            set
            {
                double dPosY = value - posY;

                for (int k = 0; k < dots.Count; k++)
                {

                    dots[k] = new Dot(
                        dots[k].x,
                        dots[k].y + dPosY,
                        dots[k].z,
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        dots[k].ny,
                        dots[k].nz);
                }

                posY = value;
                if (netMode) RenderingServer.NetSendObject(posY, NetData.PosY, client.GetStream());
            }
        }


        /// <summary>
        /// Координата Z
        /// </summary>
        public double PosZ
        {
            get
            {
                return posZ;
            }

            set
            {
                double dPosZ = value - posZ;

                for (int k = 0; k < dots.Count; k++)
                {
                    dots[k] = new Dot(
                        dots[k].x,
                        dots[k].y,
                        dots[k].z + dPosZ,
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        dots[k].ny,
                        dots[k].nz);
                }

                posZ = value;
            }
        }


        /// <summary>
        /// Перспективное искажение по оси Z
        /// </summary>
        public double PerspectiveZ
        {
            get
            {
                return perspectiveZ;
            }

            set
            {
                double dPerspectiveZ = value / perspectiveZ;

                for (int k = 0; k < dots.Count; k++)
                {

                    dots[k] = new Dot(
                        dots[k].x / (1 - dots[k].z / dPerspectiveZ),
                        dots[k].y / (1 - dots[k].z / dPerspectiveZ),
                        dots[k].z / (1 - dots[k].z / dPerspectiveZ),
                        dots[k].u,
                        dots[k].v,
                        dots[k].nx,
                        dots[k].ny,
                        dots[k].nz);
                }

                perspectiveZ = value;
            }
        }

    }
}
