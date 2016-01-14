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
        /// Шейдер, выполняющий отрисовку точки с моделью освещения Phong
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderPhong(Dot d, int frameX, int frameY, double[,] bufferLight, double[,] p1, double[,] p2, double shadow)
        {
            if (zBuffer[frameX, frameY] <= d.z)
            {

                Dot lightPoint = new Dot(light.x - d.x, light.y - d.y, light.z - d.z);

                //длины нормали и вектора света
                double normaleLength = Math.Sqrt(d.nx * d.nx + d.ny * d.ny + d.nz * d.nz);
                double lightLength = Math.Sqrt(lightPoint.x * lightPoint.x + lightPoint.y * lightPoint.y + lightPoint.z * lightPoint.z);

                //интенсивность света как косинус угла между нормалью и вектором света, находим через скалярное произведение
                double lightIntensity = (d.nx * lightPoint.x + d.ny * lightPoint.y + d.nz * lightPoint.z) / (normaleLength * lightLength);

                //вычисление глянцевого освещения
                if (specular_light > 0)
                {
                    //нормализуем вектор света
                    Dot lightNormalized = new Dot(lightPoint.x / lightLength, lightPoint.y / lightLength, lightPoint.z / lightLength);
                    //нормализуем нормаль
                    Dot normale = new Dot(d.nx / normaleLength, d.ny / normaleLength, d.nz / normaleLength);

                    //получаем вектор отраженного света по формуле r = 2*normale*<normale,light> — light
                    double LxN = lightNormalized.x * normale.x + lightNormalized.y * normale.y + lightNormalized.z * normale.z;
                    Dot reflectedLight = new Dot(2 * normale.x * LxN - lightNormalized.x, 2 * normale.y * LxN - lightNormalized.y, 2 * normale.z * LxN - lightNormalized.z);

                    //интенсивность света как косинус между направлением камеры и вектором отраженного света, высчитываем через скалярное произведение
                    double reflectedLightLength = Math.Sqrt(reflectedLight.x * reflectedLight.x + reflectedLight.y * reflectedLight.y + reflectedLight.z * reflectedLight.z);
                    double cameraLength = Math.Sqrt(camera.x * camera.x + camera.y * camera.y + camera.z * camera.z);
                    double cos = (camera.x * reflectedLight.x + camera.y * reflectedLight.y + camera.z * reflectedLight.z) / (reflectedLightLength * cameraLength);
                    //возводим в степень, чтобы уменьшить размер бликов (используем умножение, поскольку оно работает быстрее)
                    cos = Math.Pow(cos, specular_pow);

                    //применяем интенсивность, умноженную на коэффициент только для освещенных пикселей
                    if (lightIntensity > 0 && cos > 0) lightIntensity += cos * specular_light;
                }

                //матовое освещение
                lightIntensity *= diffuse_light;
                //общее освещение
                lightIntensity += ambient_light;

                //вычисление теней
                if (shadow!=0) 
                {
                    lightIntensity *= bufferLight[frameX, frameY]; 
                }

                //отрисовка пикселей в кадр
                DrawPixel(lightIntensity, d, frameX, frameY);
            }

        }


        /// <summary>
        /// Шейдер, выполняющий отрисовку точки с константным освещением
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderNoLight(Dot d, int frameX, int frameY, double[,] p0, double[,] p1, double[,] p2, double p3)
        {
            if (zBuffer[frameX, frameY] <= d.z)
            {
                DrawPixel(1, d, frameX, frameY);
            }

        }


        /// <summary>
        /// Шейдер, заполняющий буфер глубины
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderZBuffer(Dot d, int frameX, int frameY, double[,] p0, double[,] p1, double[,] p2, double p3)
        {
            if (zBuffer[frameX, frameY] < d.z)
            {
                zBuffer[frameX, frameY] = d.z;
            }
        }


        /// <summary>
        /// Шейдер, заполняющий буфер теней
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderPointShadowBuffer(Dot d, int frameX, int frameY, double[,] zBufferShadow, double[,] p1, double[,] p2, double p3)
        {
            if (zBufferShadow[frameX, frameY] < d.z)
            {
                zBufferShadow[frameX, frameY] = d.z;
            }
        }



        /// <summary>
        /// Шейдер, заполняющий буфер освещенности для модели освещения Фонга
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderLightBuffer(Dot d, int frameX, int frameY, double[,] rotateCoefs, double[,] bufferLight, double[,] zBufferShadow, double shadowIntensity)
        {
            if (zBuffer[frameX, frameY] <= d.z)
            {
                Dot rotated = RotateDot(d, rotateCoefs); 

                int xIntShadow = (int)(rotated.x + 0.5);
                int yIntShadow = (int)(rotated.y + 0.5);

                //проверка на граничные условия
                //если перед точкой находятся другие точки, то уменьшаем её освещенность (-20 против артефактов)
                if (xIntShadow < halfWidth && yIntShadow < halfHeight && xIntShadow > -halfWidth && yIntShadow > -halfHeight &&
                rotated.z < (zBufferShadow[xIntShadow + halfWidth, yIntShadow + halfHeight] - 20))
                    bufferLight[frameX, frameY] = shadowIntensity; 

            }
        }



        /// <summary>
        /// Шейдер, заполняющий буфер освещенности
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderAmbientOcclusionLightBuffer(Dot d, int frameX, int frameY, double[,] rotateCoefs, double[,] bufferLight, double[,] zBufferShadow, double shadowIntensity)
        {
            if (zBuffer[frameX, frameY] <= d.z)
            {
                Dot rotated = RotateDot(d, rotateCoefs); 

                int xIntShadow = (int)(rotated.x + 0.5);
                int yIntShadow = (int)(rotated.y + 0.5);

                //проверка на граничные условия
                //если перед точкой нет других точек, то увеличиваем её освещенность
                if (xIntShadow < halfWidth && yIntShadow < halfHeight && xIntShadow > -halfWidth && yIntShadow > -halfHeight &&
                rotated.z >= (zBufferShadow[xIntShadow + halfWidth, yIntShadow + halfHeight]))
                    bufferLight[frameX, frameY] += shadowIntensity;

            }
        }

        /// <summary>
        /// Шейдер отрисовывает кадр в соответсвие с буфером освещенности
        /// </summary>
        /// <param name="d">Точка</param>
        /// <param name="frameX">Экранная координата X</param>
        /// <param name="frameY">Экранная координата Y</param>
        /// <param name="param">Параметры</param>
        private void ShaderAmbientOcclusion(Dot d, int frameX, int frameY, double[,] bufferLight, double[,] p1, double[,] p2, double p3)
        {
            if (zBuffer[frameX, frameY] <= d.z)
            {
                DrawPixel(bufferLight[frameX, frameY], d, frameX, frameY);
            }
        }

    }
}
