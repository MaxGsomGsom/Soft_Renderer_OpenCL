using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Timers;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Soft_Renderer
{


    /// <summary>
    /// Сервер частичного рендеринга глобального освещения
    /// </summary>
    public class RenderingServer
    {
        TcpListener listener;
        TcpClient client;

        public Renderer r;
        

        int width;
        int height;
        string objFileName="temp.obj";
        string textureFileName="temp.png";
        int lightsNum;
        double lightIntensity;
        bool optimization = false;
        double indent;

        double[,] zBuffer;



        public void Run()
        {
            listener = new TcpListener(IPAddress.Any, 7779);
            listener.Start();
            try {
                client = listener.AcceptTcpClient(); }
            catch { return;  }
            client.ReceiveBufferSize = int.MaxValue;
            client.SendBufferSize = int.MaxValue;
            client.NoDelay = true;

            int offset = sizeof(int);
            NetworkStream stream = client.GetStream();


            while (true)
            {
                if (stream.DataAvailable)
                {
                    byte[] dataSizeBytes = new byte[offset];
                    ReadNetStream(stream, dataSizeBytes, 0, offset);
                    int dataSize = BitConverter.ToInt32(dataSizeBytes, 0);

                    byte[] keyBytes = new byte[offset];
                    ReadNetStream(stream, keyBytes, 0, offset);
                    NetData key = (NetData)BitConverter.ToInt32(keyBytes, 0);

                    byte[] data = new byte[dataSize];
                    if (dataSize > 0) ReadNetStream(stream, data, 0, dataSize);



                    switch (key)
                    {
                        case NetData.Height:
                            height = BitConverter.ToInt32(data, 0);
                            break;
                        case NetData.Width:
                            width = BitConverter.ToInt32(data, 0);
                            break;
                        case NetData.Model:
                            FileStream fileObj = new FileStream(objFileName, FileMode.Create);
                            fileObj.Write(data, 0, data.Length);
                            fileObj.Close();
                            break;
                        case NetData.Texture:
                            FileStream fileTex = new FileStream(textureFileName, FileMode.Create);
                            fileTex.Write(data, 0, data.Length);
                            fileTex.Close();
                            break;
                        case NetData.CreateRenderer:
                            r = new Renderer(width, height, objFileName, textureFileName);
                            break;
                        case NetData.LightsNum:
                            lightsNum = BitConverter.ToInt32(data, 0);
                            break;
                        case NetData.LightIntensity:
                            lightIntensity = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.Optimization:
                            optimization = BitConverter.ToBoolean(data, 0);
                            break;
                        case NetData.Indent:
                            indent = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.ZBuffer:
                            zBuffer = new double[width, height];
                            Buffer.BlockCopy(data, 0, zBuffer, 0, data.Length);
                            break;

                        case NetData.StartRender:
                            if (r != null)
                            {
                                r.optimization = optimization;
                                r.indent = indent;
                                r.progress = 0;

                                double[,] lightBuffer = r.AmbientOcclusionCycleOpenCL(lightsNum, lightIntensity, 0, 0);


                                NetSendObject(lightBuffer, NetData.LightBuffer, stream);


                            }
                            break;

                        case NetData.PosX:
                            r.PosX = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.PosY:
                            r.PosY = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.AngleX:
                            r.AngleX = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.AngleY:
                            r.AngleY = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.AngleZ:
                            r.AngleZ = BitConverter.ToDouble(data, 0);
                            break;
                        case NetData.Zoom:
                            r.Zoom = BitConverter.ToDouble(data, 0);
                            break;
                    }
                }
            }
        }


        public void Stop()
        {
            if (client != null) client.Close();
            if (listener != null) listener.Stop();
        }

        /// <summary>
        /// Прочитать байты из сетевого потока
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public static void ReadNetStream(NetworkStream stream, byte[] buffer,
           int offset, int count)
        {
            int read;
            while (count > 0 && (read = stream.Read(buffer, offset, count)) > 0)
            {
                count -= read;
                offset += read;
            }
            if (count != 0) throw new EndOfStreamException();
        }



        /// <summary>
        /// Отправить объект или параметр на сервер
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        public static void NetSendObject(object obj, NetData key, NetworkStream stream, int offset = sizeof(int))
        {
            byte[] data;
            if (obj != null)
            {
                if (obj is double)
                {
                    data = BitConverter.GetBytes((double)obj);
                }
                else if (obj is int)
                {
                    data = BitConverter.GetBytes((int)obj);
                }
                else if (obj is byte[])
                {
                    data = (byte[])obj;
                }
                else if (obj is double[,])
                {
                    data = new byte[((double[,])obj).Length * sizeof(double)];
                    Buffer.BlockCopy((double[,])obj, 0, data, 0, data.Length);
                }
                else
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream();
                    bf.Serialize(ms, obj);
                    data = ms.ToArray();
                }
            }
            else data = new byte[0];


            stream.Write(BitConverter.GetBytes(data.Length), 0, offset);
            stream.Write(BitConverter.GetBytes((int)key), 0, offset);
            if (data.Length > 0) stream.Write(data, 0, data.Length);

        }

    }



    public enum NetData
    {
        Model,
        Texture,
        Height,
        Width,
        CreateRenderer,
        LightsNum,
        LightIntensity,
        Optimization,
        Indent,
        ZBuffer,
        StartRender,
        LightBuffer,

        PosX,
        PosY,
        AngleX,
        AngleY,
        AngleZ,
        Zoom

    }
}
