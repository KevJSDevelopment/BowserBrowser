using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BowserUdpClient
{
    class Program
    {
        static void Main()
        {
            // Create UDP client
            UdpClient client = new UdpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            try
            {
                // Send a test message
                string message = "Hi from Bowser!";
                byte[] sendBytes = Encoding.UTF8.GetBytes(message);
                client.Send(sendBytes, sendBytes.Length, serverEndPoint);
                Console.WriteLine($"Sent: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}