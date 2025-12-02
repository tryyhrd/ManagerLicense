using Server.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static int MaxClient;
        static int Duration;
        static List<Client> AllClients = new List<Client>();
        static List<string> BlackList = new List<string>();

        static void Main(string[] args)
        {
            OnSettings();
            LoadBlackList();
            Thread tListener = new Thread(ConnectServer);
            tListener.Start();
            Thread tDisconnect = new Thread(CheckDisconnectClient);
            tDisconnect.Start();
            while (true)
            {
                SetCommand();
            }
        }

        static string GenerateStaticToken(string ipAddress)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress + "salt"));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 15);
            }
        }

        static void LoadBlackList()
        {
            string blackListPath = Directory.GetCurrentDirectory() + "/blacklist.txt";
            if (File.Exists(blackListPath))
            {
                string[] lines = File.ReadAllLines(blackListPath);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        BlackList.Add(line.Trim());
                    }
                }
            }
        }

        static void SaveToBlackList(string token)
        {
            string blackListPath = Directory.GetCurrentDirectory() + "/blacklist.txt";
            BlackList.Add(token);
            File.AppendAllText(blackListPath, token + Environment.NewLine);
        }

        static bool IsInBlackList(string token)
        {
            return BlackList.Contains(token);
        }

        static string SetCommandClient(string command, string clientIP)
        {
            if (command == "/token")
            {
                string staticToken = GenerateStaticToken(clientIP);

                if (IsInBlackList(staticToken))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"IP {clientIP} (token: {staticToken}) is blacklisted - connection rejected");
                    return "/blacklisted";
                }

                if (AllClients.Count < MaxClient)
                {
                    Classes.Client existingClient = AllClients.Find(x => x.Token == staticToken);

                    if (existingClient != null)
                    {
                        existingClient.DateConnect = DateTime.Now;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Client reconnected: {staticToken} (IP: {clientIP})");
                    }
                    else
                    {
                        Classes.Client newClient = new Classes.Client
                        {
                            Token = staticToken,
                            DateConnect = DateTime.Now
                        };
                        AllClients.Add(newClient);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"New client connection: {staticToken} (IP: {clientIP})");
                    }

                    return staticToken;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"There is not enough space on the license server");
                    return "/limit";
                }
            }
            else
            {
                if (IsInBlackList(command))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Client {command} is blacklisted - connection rejected");
                    return "/blacklisted";
                }

                Classes.Client client = AllClients.Find(x => x.Token == command);
                return client != null ? "/connect" : "/disconnect";
            }
        }

        static void CheckDisconnectClient()
        {
            while (true)
            {
                for (int iClient = 0; iClient < AllClients.Count; iClient++)
                {
                    int ClientDuration = (int)DateTime.Now.Subtract(AllClients[iClient].DateConnect).TotalSeconds;
                    if (ClientDuration > Duration)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Client: {AllClients[iClient].Token} disconnect from server due to timeout");
                        AllClients.RemoveAt(iClient);
                    }
                }
                Thread.Sleep(1000);
            }
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
                MaxClient = int.Parse(streamReader.ReadLine());
                Duration = int.Parse(streamReader.ReadLine());
                streamReader.Close();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Max count clients: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(MaxClient.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Token lifetime: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Duration.ToString());
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
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please indicate the largest number of clients: ");
                Console.ForegroundColor = ConsoleColor.Green;
                MaxClient = int.Parse(Console.ReadLine());
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Specify the token lifetime: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Duration = int.Parse(Console.ReadLine());
                StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.WriteLine(MaxClient.ToString());
                streamWriter.WriteLine(Duration.ToString());
                streamWriter.Close();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change, write the command: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }

        static void GetStatus()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Count clients: {AllClients.Count}");
            foreach (Classes.Client client in AllClients)
            {
                int duration = (int)DateTime.Now.Subtract(client.DateConnect).TotalSeconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Client: {client.Token}, time connection: {client.DateConnect.ToString("HH:mm:ss dd.MM")}, duration: {duration}");
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
            else if (Command.Contains("/disconnect"))
                DisconnectServer(Command);
            else if (Command.Contains("/blacklist"))
                BlacklistCommand(Command);
            else if (Command == "/status")
                GetStatus();
            else if (Command == "/help")
                Help();
        }

        static void BlacklistCommand(string command)
        {
            try
            {
                if (command.StartsWith("/blacklist add "))
                {
                    string token = command.Replace("/blacklist add ", "").Trim();
                    if (!string.IsNullOrEmpty(token))
                    {
                        SaveToBlackList(token);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Token {token} added to blacklist");
                        Classes.Client clientToDisconnect = AllClients.Find(x => x.Token == token);
                        if (clientToDisconnect != null)
                        {
                            AllClients.Remove(clientToDisconnect);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Client {token} disconnected from server (blacklisted)");
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + exp.Message);
            }
        }

        static void DisconnectServer(string command)
        {
            try
            {
                string token = command.Replace("/disconnect ", "").Trim();
                Classes.Client disconnectClient = AllClients.Find(x => x.Token == token);
                AllClients.Remove(disconnectClient);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Client: {token} disconnect from server");
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + exp.Message);
            }
        }

        static void ConnectServer()
        {
            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketListener.Bind(endPoint);
            socketListener.Listen(10);
            while (true)
            {
                Socket Handler = socketListener.Accept();
                string clientIP = ((IPEndPoint)Handler.RemoteEndPoint).Address.ToString();
                byte[] bytes = new byte[10485760];
                int byteRec = Handler.Receive(bytes);
                string Message = Encoding.UTF8.GetString(bytes, 0, byteRec);
                string Response = SetCommandClient(Message, clientIP);
                Handler.Send(Encoding.UTF8.GetBytes(Response));
            }
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Commands to the server: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - set initial settings");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/disconnect <token>");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - disconnect users from the server");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/blacklist add <token>");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - add token to blacklist");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show list users");
        }
    }
}