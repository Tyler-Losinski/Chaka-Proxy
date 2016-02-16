using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chaka_Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener;
            IPAddress ipaddress;
            string port;
            string ip = null;

            

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Welcome to Chaka's Proxy!");
            Console.ForegroundColor = ConsoleColor.White;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("What ip do you want to listen to?: ");
            Console.ForegroundColor = ConsoleColor.White;

            ip = Console.ReadLine();

            if(!ip.ToString().ToLower().Equals( "localhost"))
                ipaddress = IPAddress.Parse(Console.Read().ToString());
            else
                ipaddress = IPAddress.Loopback;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("What port do you want to listen to?: ");
            Console.ForegroundColor = ConsoleColor.White;
            port = Console.ReadLine();

            if (port == "")
                port = "8880";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("IP " + ipaddress.ToString() + " listening on port " + port.ToString() + "...");
            Console.ForegroundColor = ConsoleColor.White;

            listener = new TcpListener(ipaddress, Convert.ToInt32(port));
            listener.Start();

            //Loop for accepting new connections
            while (true)
            {
                // Loop proceeds only after the TcpClient accepts a new connection.
                TcpClient client = listener.AcceptTcpClient();

                ThreadStart ts = delegate { Proxy.ProcessRequest(client); };
                Thread t = new Thread(ts);
                t.Start();
            }

        }
    }
}
