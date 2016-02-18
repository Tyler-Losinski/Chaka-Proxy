using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chaka_Proxy
{
    class Proxy
    {
        private static Dictionary<string, byte[]> serverCache = new Dictionary<string, byte[]>();
        private static Logger log = new Logger();

        public static void ProcessRequest(TcpClient client)
        {
            NetworkStream clientNS = client.GetStream();
            NetworkStream serverNS = null;

            List<string> headers = new List<string>();
            List<string> returnHeaders = new List<string>();

            string host = null;
            string url = "";
            string hostIP = null;

            while (client.Connected)
            {
                string newHost;

                byte[] byteContent = null;
                byte[] returnByteContent = null;
                string buffer = ReadHeaders(clientNS);//Get request from client
                //put the headers in a list 
                headers = getHeaders(buffer);
                url = GetFullURL(headers);
                byteContent = ReadContentAsByteArray(clientNS, GetContentLength(headers));

                newHost = getHost(headers);

                if (newHost != host)
                {
                    try
                    {
                        TcpClient t = new TcpClient(); ;
                        IPAddress[] ip = Dns.GetHostAddresses(newHost);

                        for (int i = 0; i < ip.Length; i++)
                        {
                            if (ip[i].AddressFamily != AddressFamily.InterNetworkV6)
                            {
                                t.Connect(ip[i], 80);                  
                                hostIP = ip[i].ToString();
                                i = ip.Length;
                            }
                        }

                        serverNS = t.GetStream();
                        host = newHost;
                    }
                    catch (Exception) 
                    {
                        return;
                        //Console.WriteLine("Error occured setting up new host " + ex.Message);
                    }
                }

                
                PrintHeaders(headers, true);
                //Send the request from the client
                Send(serverNS, headers, byteContent);

                string buff = ReadHeaders(serverNS);

                returnHeaders = getHeaders(buff);

                PrintHeaders(returnHeaders, false);

                returnByteContent = ReadContentAsByteArray(serverNS, GetContentLength(returnHeaders));

                if (host == null)
                    host = "";
                if (url == null)
                    url = "";

                if (client.Connected && serverCache.ContainsKey(url))
                {
                    Console.WriteLine("Pulling " + host + " from cache!");
                    Console.WriteLine("Number of items in cache: " + serverCache.Count);
                    Send(clientNS, returnHeaders, serverCache[url]);
                }
                else if (client.Connected)
                {
                    serverCache.Add(url, returnByteContent);
                    Send(clientNS, returnHeaders, returnByteContent);
                }
                else
                {
                    Console.WriteLine("Exiting.");
                    return;
                }

                log.LogRequest(hostIP, host, GetContentLength(returnHeaders).ToString());

                try
                {
                    if (clientNS != null)
                    {
                        //Dispose of resources
                        clientNS.Close();
                        clientNS.Dispose();
                    }
                    if (serverNS != null)
                    {
                        serverNS.Close();
                        serverNS.Dispose();
                    }
                    
                }
                catch (Exception ex) 
                {
                    return;
                    Console.WriteLine("Error disposing: " + ex.Message);
                }

            }

        }

        /// <summary>
        /// Send the request using a network stream
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        private static void Send(NetworkStream ns, List<string> headers, byte[] content)
        {

            foreach (string header in headers)
            {
                try
                {

                    ns.Write(Encoding.ASCII.GetBytes(header), 0, header.Length);
                }
                catch (Exception ex)
                {
                    return;

                    //Console.WriteLine("Error occured while sending header: " + ex.Message);
                }
            }
            if (content != null)
            {
                try
                {
                    ns.Write(content, 0, content.Length);
                }
                catch (Exception ex)
                {
                    return;

                    //Console.WriteLine("Error occured while sending content: " + ex.Message);
                }
            }

        }

        /// <summary>
        /// Prints the headrs to the command line
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="client"></param>
        private static void PrintHeaders(List<string> headers, bool client)
        {
            Console.Write("\n\n====Headers ");
            Console.WriteLine(client ? " From Client====" : " From Server====");

            for (int i = 0; i < headers.Count; i++)
            {
                for (int j = 0; j < headers[i].Length; j++)
                {
                    if (headers[i][j] == '\r')
                    {
                        Console.Write("\\r");
                    }
                    else if (headers[i][j] == '\n')
                    {
                        Console.Write("\\n");
                    }

                    if (headers[i][j] != '\r')
                    {
                        Console.Write(headers[i][j]);
                    }
                }
            }
            Console.WriteLine("====End Headers====");
        }

        /// <summary>
        /// Pulls the host our of the header
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private static string getHost(List<string> headers)
        {
            string host = null;
            foreach (string s in headers)
            {
                if (s.Contains("Host:"))
                {
                    host = s.Split(' ').ElementAt(1);
                }
                else if(s.Contains("GET "))
                    host = s.Split(' ').ElementAt(1);

            }
            if (host != null && host.EndsWith(":443\r\n"))
                host = host.Split(new string[] { ":443\r\n" }, StringSplitOptions.None).ElementAt(0);
            else if (host != null && host.EndsWith("\r\n"))
                host = host.Split(new string[] { "\r\n" }, StringSplitOptions.None).ElementAt(0);

            return host;
        }

        private static string GetFullURL(List<string> headers) 
        {
            string host = null;
            foreach (string s in headers)
            {
                
                if (s.Contains("GET "))
                    host = s.Split(' ').ElementAt(1);

            }
            if (host != null && host.EndsWith("HTTP/1.1\r\n"))
                host = host.Split(new string[] { "HTTP/1.1\r\n" }, StringSplitOptions.None).ElementAt(0);

            return host;
        }

        private static byte[] ReadContentAsByteArray(NetworkStream ns, int contentLength)
        {
            byte[] returnByte = null;

            if (contentLength != 0)
            {
                int bytesRead = 0;
                returnByte = new byte[contentLength];

                while (bytesRead < contentLength)
                {
                    bytesRead += ns.Read(returnByte, bytesRead, contentLength - bytesRead);
                }
            }

            return returnByte;
        }

        /// <summary>
        /// Gets the length of the content
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private static int GetContentLength(List<string> headers)
        {
            int length = 0;

            foreach (string header in headers)
            {
                if (header.Contains("Content-Length"))
                {
                    length = int.Parse(header.Split(' ').ElementAt(1));
                }
            }

            return length;
        }


        /// <summary>
        /// Returns headers as a list with each line of the header as a new item in the list
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static List<string> getHeaders(string buffer)
        {
            List<string> returnHeaders;
            //Split the buffer into an array list
            returnHeaders = buffer.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            returnHeaders.Remove(returnHeaders[returnHeaders.Count() - 1]);

            for (int i = 0; i < returnHeaders.Count; i++)
            {
                returnHeaders[i] += "\r\n";
            }
            return returnHeaders;
        }

        /// <summary>
        /// Reads the Headers passed in from either the client or server
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        private static string ReadHeaders(NetworkStream ns)
        {
            byte[] b = new byte[1];
            ASCIIEncoding encoding = new ASCIIEncoding();
            string buff = "";

            try
            {
                if (ns.CanRead)
                {
                    ns.Read(b, 0, 1);
                    buff = encoding.GetString(b, 0, 1);
                    while (!buff.Contains("\r\n\r\n"))
                    {
                        ns.Read(b, 0, 1);
                        buff += encoding.GetString(b, 0, 1);
                    }
                }
            }
            catch (Exception ex) 
            {
                
                Console.WriteLine("Error reading headers: " + ex.Message);
            }

            return buff;
        }
    }
}
