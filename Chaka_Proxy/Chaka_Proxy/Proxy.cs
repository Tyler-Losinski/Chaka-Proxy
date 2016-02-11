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

        public static void ProcessClient(TcpClient client)
        {
            NetworkStream clientNS = client.GetStream();
            NetworkStream serverNS = null;

            List<string> headers = new List<string>();
            List<string> returnHeaders = new List<string>();

            string host = null;

            while (client.Connected)
            {
                string newHost;

                byte[] byteContent = null;
                byte[] returnByteContent = null;
                string buffer = ReadHeaders(clientNS);//Get request from client
                //put the headers in a list 
                headers = getHeaders(buffer);

                byteContent = ReadContent(clientNS, GetContentLength(headers));

                newHost = getHost(headers);

                if (newHost != host)
                {
                    TcpClient t = new TcpClient(); ;
                    // FIND THE IP OF THE DESTINATION
                    // IF CLIENT WANTS NEW ADDRESS WE SWITCH DESTINATIONS
                    IPAddress[] ip = Dns.GetHostAddresses(newHost);


                    for (int i = 0; i < ip.Length; i++)
                    {
                        if (ip[i].AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            t.Connect(ip[i], 80);
                            i = ip.Length;
                        }
                    }

                    serverNS = t.GetStream();
                    // CONNECTED
                    host = newHost;
                }

                PrintHeaders(headers, true);
                //Send the request from the client
                Send(serverNS, headers, byteContent);

                string buff = ReadHeaders(serverNS);

                returnHeaders = getHeaders(buff);

                Console.WriteLine(headers[0]);

                PrintHeaders(returnHeaders, false);

                returnByteContent = ReadContent(serverNS, GetContentLength(returnHeaders));

                if (client.Connected)
                {
                    Send(clientNS, returnHeaders, returnByteContent);
                }
                else
                {
                    Console.WriteLine("Exiting.");
                    return;
                }

            }

        }


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
                    Console.WriteLine("Error occured while sending header: " + ex.Message);
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
                    Console.WriteLine("Error occured while sending header: " + ex.Message);
                }
            }

        }

        /// <summary>
        /// Prints the headrs to the command line
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="isOldHeaderValues"></param>
        private static void PrintHeaders(List<string> headers, bool isOldHeaderValues)
        {
            Console.Write("\n\n====Headers ");
            Console.WriteLine(isOldHeaderValues ? " From Client====" : " From Server====");

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
        /// Takes the address and removes everything after the .com
        /// </summary>
        /// <param name="headers"></param>
        private static void Filter(List<string> headers)
        {
            string host = getHost(headers);
            string newHeader = "";
            string method = GetMethodHeader(headers);
            string[] words = method.Split(' ');

            foreach (string w in words)
            {
                if (w.Contains(host))
                {
                    int count = 0;
                    for (int i = 0; i < w.Length; i++)
                    {
                        if ('/' == w[i])
                        {
                            count++;
                            if (3 == count)
                            {
                                newHeader += w.Substring(i) + " ";
                            }
                        }
                    }
                }
                else
                {
                    newHeader += w + " ";
                }
            }
            newHeader = newHeader.TrimEnd(' ');
            for (int i = 0; i < headers.Count(); i++)
            {
                if (headers[i].Contains(host) && !headers[i].Contains("Host:") && !headers[i].Contains("Referer"))
                {
                    headers[i] = newHeader;
                    return;
                }
            }
        }

        private static string GetMethodHeader(List<string> headers)
        {
            string host = getHost(headers);
            foreach (string header in headers)
            {
                if (header.Contains(host) && !header.Contains("Host:"))
                {
                    //Console.WriteLine(header);
                    return header;
                }
            }
            return null;
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

            }
            if (host != null && host.EndsWith("\r\n"))
            {
                host = host.Split(new string[] { "\r\n" }, StringSplitOptions.None).ElementAt(0);
            }
            return host;
        }

        private static byte[] ReadContent(NetworkStream ns, int contentLength)
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

            ns.Read(b, 0, 1);
            string buff = encoding.GetString(b, 0, 1);
            while (!buff.EndsWith("\r\n\r\n"))
            {
                ns.Read(b, 0, 1);
                buff += encoding.GetString(b, 0, 1);
            }
            return buff;
        }
    }
}
