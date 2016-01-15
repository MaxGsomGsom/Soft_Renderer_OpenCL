using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Cloo;
using System.Runtime.InteropServices;

namespace Soft_Renderer
{
    //оси: ОУ - вверх, ОZ - на камеру, OX - влево
    public partial class Renderer
    {
        FastBitmap texture; //текстура
        FastBitmap frame; //изображение для вывода кадра

        List<Dot> dots = new List<Dot>();
        List<Polygon> polygons = new List<Polygon>();


        public int opencl = 0; //сколько потококов запускать на OpenCL: 0, 1, 2, 2+1
        public int cpu = 8; //сколько потококов запускать на процессоре
        int devNumGPU = 0, devNumCPU = 1; //номмера устройств, необходимо для OpenCL


        public int progress = 0;

        public bool useTexture = true; //нужно ли использовать текстуры
        public bool polygonWeb = false; //рисовать только полигональную сетку
        public bool noLight = false; //не использовать освещение вообще
        public bool midNormales = false; //использование средних нормалей  к поверхности (угловатая модель)
        public bool shadow = true; //тени
        //public bool background = false; //фоновый объект
        public bool ambient_occlusion = false; //глобальное освещение или Фонга

        public int lightsNum = 100;
        public int lightsForServer = 50;

        public bool netMode = false;
        TcpClient client;
        int offset = sizeof(int);
        BinaryFormatter formatter = new BinaryFormatter();


        public double indent = 0.1; //отступ при растеризации чтобы не было наложения
        public double quality = 1; //качество рендера (размер шага в пикселях)
        public bool optimization = false; //использовать оптимизацию при рендеринге (при большом качестве отсекает повторяющиеся точки)

        public double brightness = 1;

        //=====Phong=====
        double ambient_light = 0.5; //общее освещение
        double diffuse_light = 0.7; //матовое освещение
        double specular_light = 1; //глянцевое освещение
        double specular_pow = 9; //степень глянцевого освещения
        double shadow_intensity = 0.7; //интенсивность теней
        //=====Phong=====


        Dot light = new Dot(10000, 10000, 10000); //положение точечного источника света
        Dot camera = new Dot(1, 1, 10000); //положение камеры



        double[,] zBuffer, cleanBuffer; //буффер глубины
        float[] cleanBufferF;

        //размеры выходного изображения
        int width, height;
        int halfWidth;
        int halfHeight;

        string objFileName = "";
        string textureFileName = "";




        ComputePlatform platform ;
        ComputeContextPropertyList properties;
        ComputeContext context;
        ComputeProgram program;


        /// <summary>
        /// Рендерить кадр
        /// </summary>
        /// <returns></returns>
        public Bitmap GetFrame()
        {
            ControlsForm.calctime = DateTime.Now.Ticks;

            Bitmap result = new Bitmap(width, height);
            frame = new FastBitmap(result);
            frame.CopyBytesFromSource();

            MainLoop();

            frame.ReturnBytesToSource();


            ControlsForm.calctime = DateTime.Now.Ticks - ControlsForm.calctime;


            return result;
        }

        public void StopClient()
        {
            if (client != null) client.Close();
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <param name="objFileName">Имя файла модели</param>
        /// <param name="textureFileName">Имя файла текстуры</param>
        public Renderer(int width, int height, string objFileName, string textureFileName)
        {
            this.width = width;
            this.height = height;

            this.objFileName = objFileName;
            this.textureFileName = textureFileName;

            halfWidth = width / 2;
            halfHeight = height / 2;

            //загрузка текстуры
            if (textureFileName != null)
            {
                texture = new FastBitmap(new Bitmap(textureFileName));
                texture.CopyBytesFromSource();
            }
            else useTexture = false;

            zBuffer = new double[width, height];
            cleanBuffer = new double[width, height];
            cleanBufferF = new float[width * height];

            //считывание модели из файла
            ObjParser(objFileName);

            //if (background) ObjBackground();


            ClearBuffer(cleanBuffer, double.MinValue);

            for (int i = 0; i < cleanBufferF.Length; i++)
            {
                cleanBufferF[i] = float.MinValue;
            }


            if (ComputePlatform.Platforms[0].Devices[0].Name.Contains("CPU"))
            {
                devNumGPU = 1;
                devNumCPU = 0;
            }



            platform = ComputePlatform.Platforms[0];
            properties = new ComputeContextPropertyList(platform);
            context = new ComputeContext(platform.Devices, properties, null, IntPtr.Zero);
            program = new ComputeProgram(context, Kernel.Source);
            program.Build(null, null, null, IntPtr.Zero);

        }




        /// <summary>
        /// Считывает файл с 3D моделью
        /// </summary>
        private void ObjParser(string objFileName)
        {
            StreamReader reader = File.OpenText(objFileName);

            List<UV> UVs = new List<UV>();
            List<Normale> normales = new List<Normale>();

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                //полигоны
                if (line.StartsWith("f"))
                {

                    MatchCollection matches = Regex.Matches(line, "[0-9]+");

                    if (matches.Count == 9)
                    {
                        Polygon p = new Polygon(
                            Convert.ToInt32(matches[0].Value) - 1,
                            Convert.ToInt32(matches[3].Value) - 1,
                            Convert.ToInt32(matches[6].Value) - 1);

                        p.SetUVsPointers(Convert.ToInt32(matches[1].Value) - 1,
                            Convert.ToInt32(matches[4].Value) - 1,
                            Convert.ToInt32(matches[7].Value) - 1);

                        p.SetNormalesPointers(Convert.ToInt32(matches[2].Value) - 1,
                            Convert.ToInt32(matches[5].Value) - 1,
                            Convert.ToInt32(matches[8].Value) - 1);

                        polygons.Add(p);
                    }
                    else if (matches.Count == 6)
                    {
                        Polygon p = new Polygon(
                            Convert.ToInt32(matches[0].Value) - 1,
                            Convert.ToInt32(matches[2].Value) - 1,
                            Convert.ToInt32(matches[4].Value) - 1);

                        p.SetUVsPointers(Convert.ToInt32(matches[1].Value) - 1,
                            Convert.ToInt32(matches[3].Value) - 1,
                            Convert.ToInt32(matches[5].Value) - 1);


                        polygons.Add(p);
                    }

                }
                //текстурные координаты
                else if (line.StartsWith("vt"))
                {

                    MatchCollection matches = Regex.Matches(line, "[\\-\\+]?[0-9]+(\\.[0-9]+)?(e\\-[0-9]+)?");

                    UV uv;
                    if (useTexture)
                    {
                        uv = new UV(
                            Math.Abs(Convert.ToDouble(matches[0].Value, NumberFormatInfo.InvariantInfo) * texture.Width),
                            Math.Abs(Convert.ToDouble(matches[1].Value, NumberFormatInfo.InvariantInfo) * texture.Height));
                    }
                    else uv = new UV(1, 1);

                    UVs.Add(uv);
                }
                //нормали
                else if (line.StartsWith("vn"))
                {
                    MatchCollection matches = Regex.Matches(line, "[\\-\\+]?[0-9]+(\\.[0-9]+)?(e\\-[0-9]+)?");

                    Normale n = new Normale(
                        Convert.ToDouble(matches[0].Value, NumberFormatInfo.InvariantInfo),
                        Convert.ToDouble(matches[1].Value, NumberFormatInfo.InvariantInfo),
                        Convert.ToDouble(matches[2].Value, NumberFormatInfo.InvariantInfo));

                    normales.Add(n);
                }
                //точки
                else if (line.StartsWith("v"))
                {
                    MatchCollection matches = Regex.Matches(line, "[\\-\\+]?[0-9]+(\\.[0-9]+)?(e\\-[0-9]+)?");

                    Dot d = new Dot(
                        Convert.ToDouble(matches[0].Value, NumberFormatInfo.InvariantInfo),
                        Convert.ToDouble(matches[1].Value, NumberFormatInfo.InvariantInfo),
                        Convert.ToDouble(matches[2].Value, NumberFormatInfo.InvariantInfo));

                    dots.Add(d);
                }

            }

            reader.Close();

            //соотнесение точек с текстурными координатами и нормалями
            for (int i = 0; i < polygons.Count; i++)
            {
                dots[polygons[i].d1] = new Dot(dots[polygons[i].d1], UVs[polygons[i].uv1], normales[polygons[i].vn1]);
                dots[polygons[i].d2] = new Dot(dots[polygons[i].d2], UVs[polygons[i].uv2], normales[polygons[i].vn2]);
                dots[polygons[i].d3] = new Dot(dots[polygons[i].d3], UVs[polygons[i].uv3], normales[polygons[i].vn3]);
            }

        }


        /// <summary>
        /// Добавляет фоновую плоскость к объекту
        /// </summary>
        public void ObjBackground()
        {
            double size = 10;

            Dot d1 = new Dot(10 * size, -10 * size, 10 * size, 100, 100, 0, 0, 1);

            Dot d2 = new Dot(-10 * size, -10 * size, 10 * size, 100, 100, 0, 0, 1);

            Dot d3 = new Dot(10 * size, -10 * size, -10 * size, 100, 100, 0, 0, 1);

            Dot d4 = new Dot(-10 * size, -10 * size, -10 * size, 100, 100, 0, 0, 1);

            dots.Add(d1);
            dots.Add(d2);
            dots.Add(d3);
            dots.Add(d4);

            Polygon p1 = new Polygon(dots.Count - 4, dots.Count - 3, dots.Count - 2);
            Polygon p2 = new Polygon(dots.Count - 3, dots.Count - 2, dots.Count - 1);

            polygons.Add(p1);
            polygons.Add(p2);
        }


        /// <summary>
        /// Рендер одного кадра
        /// </summary>
        public void MainLoop()
        {
            progress = 0;

            ambient_light = 0.5 * brightness;


            //обнуление буфера глубины
            Array.Copy(cleanBuffer, zBuffer, cleanBuffer.Length);

            //рендеринг полигональной сетки
            if (polygonWeb)
            {
                RenderRasterPolygonWeb(polygons, dots, ShaderNoLight, true, false, null, null, null, 0);
                return;
            }

            //заполнение буфера глубины
            RenderRasterPolygons(polygons, dots, ShaderZBuffer, false, false, null, null, null, 0);

            //рендеринг без освещения
            if (noLight)
            {
                RenderRasterPolygons(polygons, dots, ShaderNoLight, true, false, null, null, null, 0);
                return;
            }

            //глобальное свещение
            if (ambient_occlusion)
            {

                double lightIntensity = Math.Sqrt(1d / (lightsNum * Math.Sqrt(lightsNum) * quality * quality)) * brightness;

                double[,] bufferLight = null; //буффер освещенности

                if (netMode)
                {
                    NetworkStream stream = client.GetStream();

                    RenderingServer.NetSendObject(lightsForServer, NetData.LightsNum, stream);
                    RenderingServer.NetSendObject(lightIntensity, NetData.LightIntensity, stream);
                    RenderingServer.NetSendObject(optimization, NetData.Optimization, stream);
                    RenderingServer.NetSendObject(indent, NetData.Indent, stream);
                    RenderingServer.NetSendObject(zBuffer, NetData.ZBuffer, stream);
                    RenderingServer.NetSendObject(null, NetData.StartRender, stream);

                    bufferLight = RunCalculationsCPUorOpenCL(lightIntensity, bufferLight, lightsNum - lightsForServer);

                    while (!stream.DataAvailable) { }

                    byte[] dataSizeBytes = new byte[offset];
                    RenderingServer.ReadNetStream(stream, dataSizeBytes, 0, offset);
                    int dataSize = BitConverter.ToInt32(dataSizeBytes, 0);

                    byte[] keyBytes = new byte[offset];
                    RenderingServer.ReadNetStream(stream, keyBytes, 0, offset);
                    NetData key = (NetData)BitConverter.ToInt32(keyBytes, 0);

                    byte[] data = new byte[dataSize];
                    RenderingServer.ReadNetStream(stream, data, 0, dataSize);


                    if (key == NetData.LightBuffer)
                    {
                        double[,] bufferLight2 = new double[width, height];
                        Buffer.BlockCopy(data, 0, bufferLight2, 0, data.Length);

                        for (int w = 0; w < width; w++)
                        {
                            for (int h = 0; h < height; h++)
                            {
                                bufferLight[w, h] += bufferLight2[w, h];
                            }
                        }
                    }
                    else throw new Exception();

                }
                else
                {
                    bufferLight = RunCalculationsCPUorOpenCL(lightIntensity, bufferLight, lightsNum);

                }

                //отрисовка с иcпользованием буфера освещенности
                RenderRasterPolygons(polygons, dots, ShaderAmbientOcclusion, true, false, bufferLight, null, null, 0);
                
                return;
            }

            //модель освещения Фонга
            else
            {
                double[,] bufferLight = null; //буффер освещенности

                //тени
                if (shadow)
                {
                    //создание буфера глубины для теней и буфера освещенности
                    double[,] zBufferShadow = new double[width, height]; //буффер глубины для теней
                    bufferLight = new double[width, height];

                    ClearBuffer(zBufferShadow, double.MinValue);
                    ClearBuffer(bufferLight, 1);

                    //вычисление матрицы поворота
                    double[,] rotateCoefs = CalcRotateCoefficents(light);

                    //массив повернутых точек
                    List<Dot> rotatedDots = new List<Dot>();
                    //поворачиваем все точки
                    for (int k = 0; k < dots.Count; k++)
                    {
                        rotatedDots.Add(RotateDot(dots[k], rotateCoefs));
                    }
                    //рендерим повернутую модель, чтобы заполнить буфер теней
                    RenderRasterPolygons(polygons, rotatedDots, ShaderPointShadowBuffer, false, false, zBufferShadow, null, null, 0);

                    //вычисление буфера освещенности по буферу теней
                    RenderRasterPolygons(polygons, dots, ShaderLightBuffer, false, false, rotateCoefs, bufferLight, zBufferShadow, shadow_intensity);
                }

                //отрисовка с иcпользованием буфера освещенности
                RenderRasterPolygons(polygons, dots, ShaderPhong, true, true, bufferLight, null, null, shadow ? 1 : 0);

                return;
            }

        }

        public double[,] RunCalculationsCPUorOpenCL(double lightIntensity, double[,] bufferLight, int lightsNum)
        {


            Task tsk1 = null, tsk2 = null, tsk3 = null;
            //вычисление в 1 поток OpenCL
            if (opencl > 0)
            {
                tsk1 = new Task(() =>
                {
                    bufferLight = AmbientOcclusionCycleOpenCL(lightsNum / (opencl + (cpu > 0 ? 1 : 0)), lightIntensity, devNumGPU);
                });


                //вычисление в 2 потока OpenCL
                if (opencl > 1)
                {

                    tsk2 = new Task(() =>
                    {
                        double[,] bufferLight2 = AmbientOcclusionCycleOpenCL(lightsNum / (opencl + (cpu > 0 ? 1 : 0)), lightIntensity, devNumGPU);

                        tsk1.Wait();
                        for (int o = 0; o < height; o++)
                        {
                            for (int p = 0; p < width; p++)
                            {
                                bufferLight[o, p] += bufferLight2[o, p];
                            }
                        }
                    });

                }

                if (opencl > 2)
                {
                    //вычисление на процессоре на OpenCL
                    tsk3 = new Task(() =>
                    {
                        double[,] bufferLight2 = AmbientOcclusionCycleOpenCL((int)(0.75*lightsNum / (opencl + (cpu > 0 ? 1 : 0))), lightIntensity, devNumCPU);

                        tsk1.Wait();
                        for (int o = 0; o < height; o++)
                        {
                            for (int p = 0; p < width; p++)
                            {
                                bufferLight[o, p] += bufferLight2[o, p];
                            }
                        }
                    });
                }
            }

            


            //запуск задач
            if (opencl > 0)
            {
                tsk1.Start();
                if (opencl > 1) tsk2.Start();
                if (opencl > 2) tsk3.Start();
            }


            //запуск выполнения на cpu если выбрано в настройках
            if (cpu > 0)
            {
                double[,] bufferLight2 = AmbientOcclusionCycle(lightsNum / (opencl + (cpu > 0 ? 1 : 0)), lightIntensity);

                if (opencl > 0)
                {
                    tsk1.Wait();

                    for (int o = 0; o < height; o++)
                    {
                        for (int p = 0; p < width; p++)
                        {
                            bufferLight[o, p] += bufferLight2[o, p];
                        }
                    }
                }
                else bufferLight = bufferLight2;

            }



            //ожидание задач
            if (opencl > 0)
            {
                tsk1.Wait();
                if (opencl > 1) tsk2.Wait();
                if (opencl > 2) tsk3.Wait();
            }


            return bufferLight;
        }



        /// <summary>
        /// Подключиться к серверу рендеринга
        /// </summary>
        /// <param name="ip"></param>
        public void ConnectRenderServer(string ip)
        {
            client = new TcpClient();
            client.Connect(ip, 7779);
            client.ReceiveBufferSize = int.MaxValue;
            client.SendBufferSize = int.MaxValue;
            client.NoDelay = true;

            NetworkStream stream = client.GetStream();

            RenderingServer.NetSendObject(File.ReadAllBytes(objFileName), NetData.Model, stream);
            RenderingServer.NetSendObject(File.ReadAllBytes(textureFileName), NetData.Texture, stream);
            RenderingServer.NetSendObject(height, NetData.Height, stream);
            RenderingServer.NetSendObject(width, NetData.Width, stream);
            RenderingServer.NetSendObject(null, NetData.CreateRenderer, stream);

            netMode = true;
        }




        /// <summary>
        /// Вычисление освещенности для глобального освещения с помощью OpenCL
        /// </summary>
        /// <param name="lights"></param>
        /// <param name="lightIntensity"></param>
        /// <returns></returns>
        public double[,] AmbientOcclusionCycleOpenCL(int lightsNum, double lightIntensity, int deviceNum)
        {

            //заполнение массива источников света
            List<Dot> lights = new List<Dot>();
            Random rnd = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < lightsNum; i++)
            {
                lights.Add(new Dot((rnd.NextDouble() - 0.5) * 100000, 5000, (rnd.NextDouble() - 0.5) * 100000));
            }


            float[] bufferLight = new float[width * height];


            float indentF = (float)indent;
            float qualityF = (float)quality;
            float lightIntensityF = (float)lightIntensity;

            //преобразование буфера глубины для использования в функции выполняемой на OpenCL
            float[] zBufferArr = new float[width * height];
            for (int i = 0; i < height; i++)
            {
                for (int k = 0; k < width; k++)
                {
                    if (zBuffer[i, k] > double.MinValue) zBufferArr[i * height + k] = (float)zBuffer[i, k];
                    else zBufferArr[i * height + k] = float.MinValue;
                }
            }



            //подготовка платформы
            
            ComputeKernel kernel1 = program.CreateKernel("CalcShadow");
            ComputeKernel kernel2 = program.CreateKernel("CalcLight");
            ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[deviceNum], ComputeCommandQueueFlags.None);

            //создание буферов
            ComputeBuffer<float> zBufferCL = new ComputeBuffer<float>(context, ComputeMemoryFlags.CopyHostPointer, zBufferArr);
            ComputeBuffer<float> bufferLightCL = new ComputeBuffer<float>(context, ComputeMemoryFlags.CopyHostPointer, bufferLight);


            //присвваивание параметров
            kernel1.SetIntArgument(0, width);
            kernel1.SetIntArgument(1, height);
            kernel1.SetIntArgument(19, halfWidth);
            kernel1.SetIntArgument(20, halfHeight);

            kernel2.SetIntArgument(0, width);
            kernel2.SetIntArgument(1, height);
            kernel2.SetIntArgument(19, halfWidth);
            kernel2.SetIntArgument(20, halfHeight);
            kernel2.SetFloatArgument(21, lightIntensityF);
            kernel2.SetMemoryArgument(23, bufferLightCL);
            kernel2.SetMemoryArgument(24, zBufferCL);

            ControlsForm.lighttime = DateTime.Now.Ticks;

            for (int i = 0; i < lights.Count; i++)
            {
                progress++;
                float[] zBufferShadow = new float[width * height];

                Array.Copy(cleanBufferF, zBufferShadow, cleanBufferF.Length);

                //создание и присваивание буфера
                ComputeBuffer<float> zBufferShadowCL = new ComputeBuffer<float>(context, ComputeMemoryFlags.CopyHostPointer, zBufferShadow);
                kernel1.SetMemoryArgument(2, zBufferShadowCL);
                kernel2.SetMemoryArgument(2, zBufferShadowCL);


                //вычисление матрицы поворота

                float[] rotateCoefs = new float[9];

                //ось вращения
                Dot axis = new Dot(camera.z * lights[i].y - camera.y * lights[i].z, camera.x * lights[i].z - camera.z * lights[i].x, camera.y * lights[i].x - camera.x * lights[i].y);
                //нормируем ось вращения
                double axisLength = Math.Sqrt(axis.x * axis.x + axis.y * axis.y + axis.z * axis.z);
                axis = new Dot(axis.x / axisLength, axis.y / axisLength, axis.z / axisLength);

                //угол вращения (через скалярное произведение)
                double angle = Math.Acos((camera.x * lights[i].x + camera.y * lights[i].y + camera.z * lights[i].z) / (Math.Sqrt(camera.x * camera.x + camera.y * camera.y + camera.z * camera.z) * Math.Sqrt(lights[i].x * lights[i].x + lights[i].y * lights[i].y + lights[i].z * lights[i].z)));

                rotateCoefs[0] = (float)(Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.x * axis.x);
                rotateCoefs[1] = (float)((1 - Math.Cos(angle)) * axis.x * axis.y - Math.Sin(angle) * axis.z);
                rotateCoefs[2] = (float)((1 - Math.Cos(angle)) * axis.x * axis.z + Math.Sin(angle) * axis.y);

                rotateCoefs[3] = (float)((1 - Math.Cos(angle)) * axis.x * axis.y + Math.Sin(angle) * axis.z);
                rotateCoefs[4] = (float)(Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.y * axis.y);
                rotateCoefs[5] = (float)((1 - Math.Cos(angle)) * axis.y * axis.z - Math.Sin(angle) * axis.x);

                rotateCoefs[6] = (float)((1 - Math.Cos(angle)) * axis.z * axis.x - Math.Sin(angle) * axis.y);
                rotateCoefs[7] = (float)((1 - Math.Cos(angle)) * axis.y * axis.z + Math.Sin(angle) * axis.x);
                rotateCoefs[8] = (float)(Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.z * axis.z);


                ComputeBuffer<float> rotateCoefsCL = new ComputeBuffer<float>(context, ComputeMemoryFlags.CopyHostPointer, rotateCoefs);
                kernel2.SetMemoryArgument(22, rotateCoefsCL);

                //массив повернутых точек
                List<Dot> rotatedDots = new List<Dot>();
                //поворачиваем все точки
                for (int k = 0; k < dots.Count; k++)
                {
                    Dot d = dots[k];
                    Dot result = new Dot(
                   rotateCoefs[0] * d.x + rotateCoefs[1] * d.y + rotateCoefs[2] * d.z,
                   rotateCoefs[3] * d.x + rotateCoefs[4] * d.y + rotateCoefs[5] * d.z,
                   rotateCoefs[6] * d.x + rotateCoefs[7] * d.y + rotateCoefs[8] * d.z);

                    rotatedDots.Add(result);
                }




                //растеризация полигона - ТЕНИ
                //=============================================

                for (int r = 0; r < polygons.Count; r++)
                {

                    float[] upper = { (float)rotatedDots[polygons[r].d1].x, (float)rotatedDots[polygons[r].d1].y, (float)rotatedDots[polygons[r].d1].z };
                    float[] mid = { (float)rotatedDots[polygons[r].d2].x, (float)rotatedDots[polygons[r].d2].y, (float)rotatedDots[polygons[r].d2].z };
                    float[] down = { (float)rotatedDots[polygons[r].d3].x, (float)rotatedDots[polygons[r].d3].y, (float)rotatedDots[polygons[r].d3].z };


                    float[] buf;
                    if (mid[1] < upper[1])
                    {
                        buf = mid;
                        mid = upper;
                        upper = buf;
                    }
                    if (down[1] < upper[1])
                    {
                        buf = down;
                        down = upper;
                        upper = buf;
                    }
                    if (down[1] < mid[1])
                    {
                        buf = down;
                        down = mid;
                        mid = buf;
                    }

                    float lengthLongY = down[1] - upper[1];
                    float lengthShortUpY = mid[1] - upper[1];
                    float lengthShortDownY = down[1] - mid[1];


                    float[] stepSizeLong = {
                        (upper[0] - down[0]) / lengthLongY,
                        (upper[1] - down[1]) / lengthLongY,
                        (upper[2] - down[2]) / lengthLongY
                    };

                    float[] stepSizeShortUp = {
                        (upper[0] - mid[0]) / lengthShortUpY,
                        (upper[1] - mid[1]) / lengthShortUpY,
                        (upper[2] - mid[2]) / lengthShortUpY
                    };

                    float[] stepSizeShortDown = {
                        (mid[0] - down[0]) / lengthShortDownY,
                        (mid[1] - down[1]) / lengthShortDownY,
                        (mid[2] - down[2]) / lengthShortDownY
                    };



                    if ((long)lengthLongY == 0) continue;


                    //присваивание параметров и запуск задачи на OpenCL
                    kernel1.SetFloatArgument(3, upper[0]);
                    kernel1.SetFloatArgument(4, upper[1]);
                    kernel1.SetFloatArgument(5, upper[2]);
                    kernel1.SetFloatArgument(6, mid[0]);
                    kernel1.SetFloatArgument(7, mid[1]);
                    kernel1.SetFloatArgument(8, mid[2]);
                    kernel1.SetFloatArgument(9, stepSizeLong[0]);
                    kernel1.SetFloatArgument(10, stepSizeLong[1]);
                    kernel1.SetFloatArgument(11, stepSizeLong[2]);

                    kernel1.SetFloatArgument(12, stepSizeShortUp[0]);
                    kernel1.SetFloatArgument(13, stepSizeShortUp[1]);
                    kernel1.SetFloatArgument(14, stepSizeShortUp[2]);

                    kernel1.SetFloatArgument(15, stepSizeShortDown[0]);
                    kernel1.SetFloatArgument(16, stepSizeShortDown[1]);
                    kernel1.SetFloatArgument(17, stepSizeShortDown[2]);

                    kernel1.SetFloatArgument(18, lengthShortUpY);


                    commands.Execute(kernel1, null, new long[] { (long)(lengthLongY + 0.49) }, null, null);


                }
                //==============



                //растеризация полигона - СВЕТА
                //=============================================



                for (int r = 0; r < polygons.Count; r++)
                {

                    float[] upper = { (float)dots[polygons[r].d1].x, (float)dots[polygons[r].d1].y, (float)dots[polygons[r].d1].z };
                    float[] mid = { (float)dots[polygons[r].d2].x, (float)dots[polygons[r].d2].y, (float)dots[polygons[r].d2].z };
                    float[] down = { (float)dots[polygons[r].d3].x, (float)dots[polygons[r].d3].y, (float)dots[polygons[r].d3].z };


                    float[] buf;
                    if (mid[1] < upper[1])
                    {
                        buf = mid;
                        mid = upper;
                        upper = buf;
                    }
                    if (down[1] < upper[1])
                    {
                        buf = down;
                        down = upper;
                        upper = buf;
                    }
                    if (down[1] < mid[1])
                    {
                        buf = down;
                        down = mid;
                        mid = buf;
                    }

                    float lengthLongY = down[1] - upper[1];
                    float lengthShortUpY = mid[1] - upper[1];
                    float lengthShortDownY = down[1] - mid[1];


                    float[] stepSizeLong = {
                        (upper[0] - down[0]) / lengthLongY,
                        (upper[1] - down[1]) / lengthLongY,
                        (upper[2] - down[2]) / lengthLongY
                    };

                    float[] stepSizeShortUp = {
                        (upper[0] - mid[0]) / lengthShortUpY,
                        (upper[1] - mid[1]) / lengthShortUpY,
                        (upper[2] - mid[2]) / lengthShortUpY
                    };

                    float[] stepSizeShortDown = {
                        (mid[0] - down[0]) / lengthShortDownY,
                        (mid[1] - down[1]) / lengthShortDownY,
                        (mid[2] - down[2]) / lengthShortDownY
                    };



                    if ((long)lengthLongY == 0) continue;



                    //присваивание параметров и запуск задачи на OpenCL
                    kernel2.SetFloatArgument(3, upper[0]);
                    kernel2.SetFloatArgument(4, upper[1]);
                    kernel2.SetFloatArgument(5, upper[2]);
                    kernel2.SetFloatArgument(6, mid[0]);
                    kernel2.SetFloatArgument(7, mid[1]);
                    kernel2.SetFloatArgument(8, mid[2]);
                    kernel2.SetFloatArgument(9, stepSizeLong[0]);
                    kernel2.SetFloatArgument(10, stepSizeLong[1]);
                    kernel2.SetFloatArgument(11, stepSizeLong[2]);

                    kernel2.SetFloatArgument(12, stepSizeShortUp[0]);
                    kernel2.SetFloatArgument(13, stepSizeShortUp[1]);
                    kernel2.SetFloatArgument(14, stepSizeShortUp[2]);

                    kernel2.SetFloatArgument(15, stepSizeShortDown[0]);
                    kernel2.SetFloatArgument(16, stepSizeShortDown[1]);
                    kernel2.SetFloatArgument(17, stepSizeShortDown[2]);

                    kernel2.SetFloatArgument(18, lengthShortUpY);



                    commands.Execute(kernel2, null, new long[] { (long)(lengthLongY + 0.49) }, null, null);


                }

                //===========


                commands.Finish();

                zBufferShadowCL.Dispose();
                rotateCoefsCL.Dispose();

            }

            ControlsForm.lighttime = DateTime.Now.Ticks - ControlsForm.lighttime;

            commands.ReadFromBuffer(bufferLightCL, ref bufferLight, false, null);

            double[,] bufferLightArr = new double[width, height];
            for (int s = 0; s < width; s++)
            {
                for (int q = 0; q < height; q++)
                {
                    bufferLightArr[s, q] = bufferLight[s * height + q];

                }
            }

            zBufferCL.Dispose();
            bufferLightCL.Dispose();

            return bufferLightArr;
        }




        /// <summary>
        /// Вычисление освещенности для глобального освещения
        /// </summary>
        /// <param name="lights"></param>
        /// <param name="lightIntensity"></param>
        /// <returns></returns>
        public double[,] AmbientOcclusionCycle(int lightsNum, double lightIntensity)
        {

            //заполнение массива источников света
            List<Dot> lights = new List<Dot>();
            Random r = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < lightsNum; i++)
            {
                lights.Add(new Dot((r.NextDouble() - 0.5) * 100000, 5000, (r.NextDouble() - 0.5) * 100000));
            }


            double[,] bufferLight = new double[width, height];

            ParallelOptions opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = cpu;

            ControlsForm.lighttime = DateTime.Now.Ticks;

            Parallel.For(0, lights.Count, opt, new Action<int>((i) =>
            {
                //for (int i = 0; i < lights.Count; i++)
                //{
                progress++;
                double[,] zBufferShadow = new double[width, height];

                Array.Copy(cleanBuffer, zBufferShadow, cleanBuffer.Length);

                //вычисление матрицы поворота
                double[,] rotateCoefs = CalcRotateCoefficents(lights[i]);
                //массив повернутых точек
                List<Dot> rotatedDots = new List<Dot>();
                //поворачиваем все точки
                for (int k = 0; k < dots.Count; k++)
                {
                    rotatedDots.Add(RotateDot(dots[k], rotateCoefs));
                }
                //рендерим повернутую модель, чтобы заполнить буфер теней
                RenderRasterPolygons(polygons, rotatedDots, ShaderPointShadowBuffer, false, false, zBufferShadow, null, null, 0);

                //вычисление буфера освещенности по буферу теней
                RenderRasterPolygons(polygons, dots, ShaderAmbientOcclusionLightBuffer, false, false, rotateCoefs, bufferLight, zBufferShadow, lightIntensity);
                //}
            }));


            ControlsForm.lighttime = DateTime.Now.Ticks - ControlsForm.lighttime;

            return bufferLight;
        }



        /// <summary>
        /// Обнуление буфера на заданное значение
        /// </summary>
        /// <param name="zBuffer"></param>
        /// <param name="value"></param>
        private void ClearBuffer(double[,] zBuffer, double value)
        {

            if (value == 0)
            {
                Array.Clear(zBuffer, 0, zBuffer.Length);
                return;
            }

            int len1 = zBuffer.GetLength(0);
            int len2 = zBuffer.GetLength(1);

            for (int w = 0; w < len1; w++)
            {
                for (int h = 0; h < len2; h++)
                {
                    zBuffer[w, h] = value;
                }
            }
        }



        /// <summary>
        /// Вычисление матрицы поворота на заданный угол
        /// </summary>
        /// <param name="light">Точечный источник света</param>
        /// <returns></returns>
        private double[,] CalcRotateCoefficents(Dot light)
        {
            //ось вращения
            Dot axis = new Dot(camera.z * light.y - camera.y * light.z, camera.x * light.z - camera.z * light.x, camera.y * light.x - camera.x * light.y);
            //нормируем ось вращения
            double axisLength = Math.Sqrt(axis.x * axis.x + axis.y * axis.y + axis.z * axis.z);
            axis = new Dot(axis.x / axisLength, axis.y / axisLength, axis.z / axisLength);

            //угол вращения (через скалярное произведение)
            double angle = Math.Acos((camera.x * light.x + camera.y * light.y + camera.z * light.z) / (Math.Sqrt(camera.x * camera.x + camera.y * camera.y + camera.z * camera.z) * Math.Sqrt(light.x * light.x + light.y * light.y + light.z * light.z)));

            //Вычисляем коэффициенты матрицы поворота
            double[,] result = new double[3, 3];

            result[0, 0] = (Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.x * axis.x);
            result[0, 1] = ((1 - Math.Cos(angle)) * axis.x * axis.y - Math.Sin(angle) * axis.z);
            result[0, 2] = ((1 - Math.Cos(angle)) * axis.x * axis.z + Math.Sin(angle) * axis.y);

            result[1, 0] = ((1 - Math.Cos(angle)) * axis.x * axis.y + Math.Sin(angle) * axis.z);
            result[1, 1] = (Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.y * axis.y);
            result[1, 2] = ((1 - Math.Cos(angle)) * axis.y * axis.z - Math.Sin(angle) * axis.x);

            result[2, 0] = ((1 - Math.Cos(angle)) * axis.z * axis.x - Math.Sin(angle) * axis.y);
            result[2, 1] = ((1 - Math.Cos(angle)) * axis.y * axis.z + Math.Sin(angle) * axis.x);
            result[2, 2] = (Math.Cos(angle) + (1 - Math.Cos(angle)) * axis.z * axis.z);

            return result;
        }



        /// <summary>
        /// Поворот точки на заданный угол по трем измерениям
        /// </summary>
        /// <param name="d">Точка для поворота</param>
        /// <param name="rotateCoefs">Матрица поворота</param>
        /// <returns></returns>
        private Dot RotateDot(Dot d, double[,] rotateCoefs)
        {
            Dot result = new Dot(
                rotateCoefs[0, 0] * d.x + rotateCoefs[0, 1] * d.y + rotateCoefs[0, 2] * d.z,
                rotateCoefs[1, 0] * d.x + rotateCoefs[1, 1] * d.y + rotateCoefs[1, 2] * d.z,
                rotateCoefs[2, 0] * d.x + rotateCoefs[2, 1] * d.y + rotateCoefs[2, 2] * d.z);

            return result;
        }


        /// <summary>
        /// Вычисление средней нормали к поверхности
        /// </summary>
        /// <param name="upper"></param>
        /// <param name="mid"></param>
        /// <param name="down"></param>
        private void CalcPolygonNormale(ref Dot upper, ref Dot mid, ref Dot down)
        {

            //векторы, представляющие две стороны полигона
            Dot vector1 = new Dot(upper.x - down.x, upper.y - down.y, upper.z - down.z);
            Dot vector2 = new Dot(mid.x - down.x, mid.y - down.y, mid.z - down.z);

            //нормаль к поверхности полигона - векторное произведение
            Dot normale = new Dot(vector1.y * vector2.z - vector1.z * vector2.y, vector1.z * vector2.x - vector1.x * vector2.z, vector1.x * vector2.y - vector1.y * vector2.x);

            //устанавливаем для трех точек полигона
            upper = new Dot(upper.x, upper.y, upper.z, upper.u, upper.v, normale.x, normale.y, normale.z);
            mid = new Dot(mid.x, mid.y, mid.z, mid.u, mid.v, normale.x, normale.y, normale.z);
            down = new Dot(down.x, down.y, down.z, down.u, down.v, normale.x, normale.y, normale.z);

        }


        /// <summary>
        /// Отрисовка одного пикселя в кадре
        /// </summary>
        /// <param name="lightIntensity">Интенсивность освещения</param>
        /// <param name="d">Отображаемая точка в 3D пространсве</param>
        /// <param name="frameX">Координата Х пикселя в кадре</param>
        /// <param name="frameY">Координата У пикселя в кадре</param>
        private void DrawPixel(double lightIntensity, Dot d, int frameX, int frameY)
        {

            //если пиксель не освещен - рисуем его черным
            if (lightIntensity <= 0)
            {
                frame.SetPixel(frameX, frameY, Color.Black);
            }
            else
            {

                //получаем цвет из текстуры
                Color colorBase;
                if (useTexture) colorBase = texture.GetPixel((int)(d.u + 0.5), (int)(d.v + 0.5));
                else colorBase = Color.White;

                //интенсивность цветов не должна быть больше 255
                int r = (int)(colorBase.R * lightIntensity);
                if (r > 255) r = 255;

                int g = (int)(colorBase.G * lightIntensity);
                if (g > 255) g = 255;

                int b = (int)(colorBase.B * lightIntensity);
                if (b > 255) b = 255;

                //применяем освещенность
                Color color = Color.FromArgb(colorBase.A, r, g, b);

                //записываем в кадр
                frame.SetPixel(frameX, frameY, color);
            }
        }



    }


}
