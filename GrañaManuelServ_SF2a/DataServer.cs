using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrañaManuelServ_SF2a
{
    internal class DataServer
    {   private static readonly object l = new object();
        Socket servidor;
        string rutaLogs = Environment.GetEnvironmentVariable("programdata") + "/examen/log.bin";
        private List<InfoClient> logs= new List<InfoClient>();
        int puerto;
        

        public void SaveLog() 
        {

            try
            {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(rutaLogs, FileMode.Create)))
                {
                    bw.Write(logs.Count);
                    foreach (InfoClient client in logs)
                    {
                        bw.Write(client.Ip);
                        bw.Write((int)client.Port);
                        bw.Write(client.Ip);
                    }
                    bw.Write(135);
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException)
            {
                Console.WriteLine(ex.Message);
            }
        }
        

        public int LoadPort() 
        {
            if (!File.Exists(rutaLogs)) 
            {
                return -1;
            }
            using (BinaryReader br = new BinaryReader(new FileStream(rutaLogs, FileMode.Open))) 
            {
                try 
                {
                    br.BaseStream.Seek(-4, SeekOrigin.End);
                    int port = br.ReadInt32();
                    return port;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is IOException)
                {
                    return -1;
                }
            }
        }

        public void LoadLog() 
        {

            if (File.Exists(rutaLogs)) 
            {
                try
                {
                    using (BinaryReader br = new BinaryReader(new FileStream(rutaLogs, FileMode.Open)))
                    {
                    
                        int size = br.ReadInt32();

                        for (int i = 0; i < size; i++)
                        {
                            string ip = br.ReadString();
                            int port = br.ReadInt32();
                            string time = br.ReadString();

                            InfoClient inf = new InfoClient(ip, port, time);
                            logs.Add(inf);
                        }
                    }  
                }
                catch (Exception ex) when (ex is ArgumentException || ex is IOException)
                {
                    Console.WriteLine($"{ex.Message}");
                }
            }
            
        }

        public void InitServer() 
        {
            puerto=LoadPort();

            if (puerto==-1||puerto>=65536||puerto<0)
            {
                puerto = 31416;
            }

            IPEndPoint ie = new IPEndPoint(IPAddress.Any,puerto);
            using (servidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) 
            {
                try
                {
                    servidor.Bind(ie);
                    servidor.Listen(10);
                    Console.WriteLine("Server Conectado en " + puerto);
                    LoadLog();

                    while (true)
                    {
                        Socket cliente = servidor.Accept();
                        Thread hilo = new Thread(ClientThread);
                        hilo.Start(cliente);
                    }
                }
                catch (SocketException e) 
                {
                    Console.WriteLine("Desconectado");
                }
            }

        }

        public void ClientThread(object cliente) 
        {
            string mensaje;
            bool conec = true;

            Socket client = (Socket) cliente;
            IPEndPoint ieCliente=(IPEndPoint) client.RemoteEndPoint;

            using (NetworkStream ns = new NetworkStream(client))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns)) 
            {
                sw.WriteLine("WELCOME");
                sw.Flush();

                string ip= ieCliente.Address.ToString();
                int port = ieCliente.Port;
                string time = DateTime.Now.ToString();

                InfoClient info = new InfoClient(ip,port,time);
                
                lock (l)
                {
                    logs.Add(info);
                }
                
                while (conec)
                {
                    try 
                    {
                        mensaje = sr.ReadLine();

                        if (mensaje != null)
                        {
                            mensaje = mensaje.ToUpper();

                            if (mensaje.StartsWith("DIR "))
                            {
                                string trayectoria = mensaje.Substring(3).Trim();

                                if (trayectoria == null || trayectoria == "")
                                {
                                    trayectoria = Environment.GetEnvironmentVariable("programdata") + "/examen";
                                }

                                try
                                {
                                    DirectoryInfo directoryInfo = new DirectoryInfo(trayectoria);

                                    FileInfo[] infoFile = directoryInfo.GetFiles();
                                    foreach (FileInfo fileInfo in infoFile)
                                    {
                                        sw.WriteLine(fileInfo.Name);
                                        sw.Flush();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error en el acceso al archivo");
                                }
                            }
                            else if (mensaje.StartsWith("GET "))
                            {
                                string archivo = mensaje.Substring(3).Trim();
                                string ruta = Environment.GetEnvironmentVariable("programdata") + "/examen/" + archivo + ".txt";

                                if (!File.Exists(ruta))
                                {
                                    sw.WriteLine("El archivo no existe");
                                    sw.Flush();
                                }
                                else
                                {
                                    try
                                    {
                                        string[] lines = File.ReadAllLines(ruta);
                                        foreach (string line in lines)
                                        {
                                            sw.WriteLine(line);
                                        }
                                        sw.Flush();
                                    }
                                    catch (Exception e)
                                    {
                                        sw.WriteLine("File_Error");
                                        sw.Flush();
                                    }
                                }
                            }
                            else if (mensaje.StartsWith("LOG "))
                            {
                                string log = mensaje.Substring(3).Trim();

                                if (mensaje == "LOG")
                                {
                                    lock (l)
                                    {
                                        foreach (InfoClient cl in logs)
                                        {
                                            sw.WriteLine(cl.ToString());
                                            sw.Flush();
                                        }
                                    }
                                }
                                else if (int.TryParse(log, out int _))
                                {
                                    lock (l)
                                    {
                                        for (int i = logs.Count - 1; i >= 0; i--)
                                        {
                                            logs.RemoveAt(i);
                                        }
                                    }
                                    sw.WriteLine("LOG_DEL");
                                }
                                else
                                {
                                    sw.Write("LOG_ERROR");
                                    sw.Flush();
                                }
                            }
                            else if (mensaje.StartsWith("CHPORT "))
                            {
                                string msg = mensaje.Substring(6).Trim();
                                if (int.TryParse(msg, out int _))
                                {
                                    int puerto = int.Parse(msg);

                                    using (BinaryWriter bw = new BinaryWriter(new FileStream(rutaLogs, FileMode.Open)))
                                    {
                                        try
                                        {
                                            bw.BaseStream.Seek(-4, SeekOrigin.End);

                                            bw.Write(puerto);
                                        }
                                        catch (Exception ex) when (ex is ArgumentException || ex is IOException)
                                        {
                                            sw.Write("CHPORT_ERROR");
                                            sw.Flush();
                                        }
                                    }
                                }
                                else
                                {
                                    sw.Write("CHPORT_ERROR");
                                    sw.Flush();
                                }
                            }
                            else if (mensaje.StartsWith("DNS "))
                            {
                                string dnsUrl = mensaje.Substring(3);
                                try
                                {
                                    IPHostEntry infoDns = Dns.GetHostEntry(dnsUrl);
                                    sw.WriteLine("Lista de IPs");
                                    sw.Flush();

                                    foreach (IPAddress ipDns in infoDns.AddressList)
                                    {
                                        if (ipDns.AddressFamily == AddressFamily.InterNetwork)
                                        {
                                            sw.WriteLine(ipDns);
                                            sw.Flush();
                                        }
                                    }
                                }
                                catch (Exception ex) when (ex is ArgumentException || ex is SocketException)
                                {
                                    sw.WriteLine(ex.Message);
                                    sw.Flush();
                                }

                            }
                            else if (mensaje == "CLOSE")
                            {
                                conec = false;
                            }
                            else if (mensaje == "HALT")
                            {
                                conec = false;
                                SaveLog();
                                servidor.Close();
                            }
                            else
                            {
                                sw.WriteLine("COMMAND_ERROR");
                                sw.Flush();
                            }
                        }
                    }catch (IOException ex) 
                    {
                        conec = false;
                    }
                    
                }
                
            }
            client.Close();
        }
    }
}
