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
            IPAddress ipaddress = IPAddress.Loopback;
            int port = 8880;

            listener = new TcpListener(ipaddress, port);
            listener.Start();

            Console.WriteLine("Welcome to Chaka's Proxy!");
            Console.WriteLine("IP " + ipaddress.ToString() + " listening on port " + port.ToString() + "...");

            //Loop for accepting new connections
            while (true)
            {
                // Loop proceeds only after the TcpClient accepts a new connection.
                TcpClient client = listener.AcceptTcpClient();

                ThreadStart ts = delegate { Proxy.ProcessClient(client); };
                Thread t = new Thread(ts);
                t.Start();
            }

        }
    }
}
