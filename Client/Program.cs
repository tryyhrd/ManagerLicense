using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;

        static string ClientToken;
        static DateTime ClientDataConnection;

        static void Main(string[] args)
        {
            OnSettings();

            Thread tCheckToken = new Thread(CheckToken);
            tCheckToken.Start();

            while (true)
            {
                SetCommand();
            }
        }

        static void SetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string Command = Console.ReadLine();

            if (Command == "/config")
            {
                File.Delete(Directory.GetCurrentDirectory() + "/.config");
                OnSettings();
            }
            else if (Command == "/connect")
                ConnectServer();
            else if (Command == "/status")
                GetStatus();
            else if (Command == "/help")
                Help();
        }

        static void ConnectServer()
        {
            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream, 
                ProtocolType.Tcp);

            try {
                socket.Connect(endPoint);
            }
            catch(Exception exp) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + exp.Message);
            }

            if (socket.Connected)
            {

                Console.ForegroundColor = ConsoleColor.Green;
                socket.Send(Encoding.UTF8.GetBytes("/token"));

                byte[] Bytes = new byte[1048 * 1048];
                int BytesRec = socket.Receive(Bytes);

                string Response = Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                if (Response == "/limit")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("There is not enough space on the license server");
                }
                else
                {
                    ClientToken = Response; 
                    ClientDataConnection = DateTime.Now;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Recieved connection token: " + ClientToken);
                }
            }
        }

        static void CheckToken()
        {
            while (true)
            {
                Thread.Sleep(1000);

                if (!String.IsNullOrEmpty(ClientToken))
                {
                    IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
                    Socket socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                    try
                    {
                        socket.Connect(endPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + ex.Message);
                    }

                    if (socket.Connected)
                    {
                        socket.Send(Encoding.UTF8.GetBytes(ClientToken));

                        byte[] Bytes = new byte[1048 * 1048];
                        int BytesRec = socket.Receive(Bytes);

                        string Response = Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                        if (Response == "/disconnect")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("There client is disconnected from server");
                            ClientToken = String.Empty;
                        }
                    }
                }
            }
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Commands to the clients: ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - set initial settings");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/connect");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - connection to the server");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show list users");
        }

        static void GetStatus()
        {
            int duration = (int)DateTime.Now.Subtract(ClientDataConnection).TotalSeconds;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Client: {ClientToken}, time connection: {ClientDataConnection.ToString("HH:mm:ss dd.MM")}, " +
                $"duration: {duration}");
        }

        static void OnSettings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";
            string IpAddress = "";

            if (File.Exists(Path))
            {
                StreamReader streamReader = new StreamReader(Path);
                IpAddress = streamReader.ReadLine();


                ServerIpAddress = IPAddress.Parse(IpAddress);
                ServerPort = int.Parse(streamReader.ReadLine());
                streamReader.Close();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please provide the IP address if the license server: ");
                Console.ForegroundColor = ConsoleColor.Green;
                IpAddress = Console.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please provide the Port if the license server: ");
                Console.ForegroundColor = ConsoleColor.Green;
                ServerPort = int.Parse(Console.ReadLine());

                StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.Close();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change, write the command: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }
    }
}
