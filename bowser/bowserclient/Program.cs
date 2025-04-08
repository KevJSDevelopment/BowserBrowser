using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ToadClient
{
    class Program
    {
        static UdpClient client;
        static IPEndPoint serverEndPoint;
        static string lastMessage;
        static string lastSeq;
        static bool ackReceived;
        static Timer retryTimer;

        static void Main()
        {
            client = new UdpClient(0);
            serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            retryTimer = new Timer(2000);
            retryTimer.Elapsed += OnRetryTimeout;
            retryTimer.AutoReset = false;

            try
            {
                // Handshake
                SendToadPacket("START", "1");
                if (!WaitForAck()) throw new Exception("Handshake failed");

                // Send BOWSER (HTTP/1.0) request
                string request = "GET / HTTP/1.0\r\n\r\n";
                SendToadPacket("DATA", "2", request);
                if (!WaitForAck()) throw new Exception("Request send failed");

                // Receive response
                string response = WaitForData();
                Console.WriteLine($"Response: {response}");

                // Teardown
                SendToadPacket("STOP", "3");
                if (!WaitForAck()) throw new Exception("Teardown failed");
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

        static string WaitForData()
        {
            while (true)
            {
                var result = client.Client.Poll(2000000, SelectMode.SelectRead);
                if (result)
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = client.Receive(ref from);
                    string message = Encoding.UTF8.GetString(receivedBytes);
                    string[] parts = message.Split('|');
                    string header = parts[0].TrimEnd(']');
                    string payload = parts.Length > 1 ? parts[1] : "";

                    string[] headerParts = header.Split(':');
                    string type = headerParts[0].TrimStart('[');
                    if (type == "DATA")
                    {
                        return payload; // HTTP response
                    }
                }
            }
        }
        static void SendToadPacket(string type, string seq, string payload = "")
        {
            lastMessage = $"[{type}:{seq}|{payload}]";
            lastSeq = seq;
            ackReceived = false;

            byte[] sendBytes = Encoding.UTF8.GetBytes(lastMessage);
            client.Send(sendBytes, sendBytes.Length, serverEndPoint);
            Console.WriteLine($"Sent: {lastMessage}");

            retryTimer.Start(); // Start retry timer
        }

        static bool WaitForAck()
        {
            while (!ackReceived)
            {
                var result = client.Client.Poll(2000000, SelectMode.SelectRead); // Wait up to 2 seconds
                if (result)
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = client.Receive(ref from);
                    string message = Encoding.UTF8.GetString(receivedBytes);
                    if (message == $"[ACK:{lastSeq}|]")
                    {
                        retryTimer.Stop();
                        ackReceived = true;
                        Console.WriteLine($"Received: {message}");
                        return true;
                    }
                }
            }
            return false;
        }

        static void OnRetryTimeout(object source, ElapsedEventArgs e)
        {
            if (!ackReceived)
            {
                Console.WriteLine($"Retrying: {lastMessage}");
                byte[] sendBytes = Encoding.UTF8.GetBytes(lastMessage);
                client.Send(sendBytes, sendBytes.Length, serverEndPoint);
                retryTimer.Start(); // Restart timer
            }
        }
    }
}