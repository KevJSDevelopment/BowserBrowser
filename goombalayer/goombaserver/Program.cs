using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BowserUdpServer
{
    class Program
    {
        static void Main()
        {
            // Create UDP server on localhost:8080
            UdpClient server = new UdpClient(8080);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("UDP Server running on localhost:8080...");

            try
            {
                while (true)
                {
                    // Receive a packet (blocks until something arrives)
                    byte[] receivedBytes = server.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(receivedBytes);
                    Console.WriteLine($"Received from {remoteEndPoint}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                server.Close();
            }
        }
    }
}