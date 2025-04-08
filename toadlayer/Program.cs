// See https://aka.ms/new-console-template for more information
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

internal class Program
{
    private static void Main(string[] args)
    {
        UdpClient server = new UdpClient(8080);
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Dictionary<string, bool> connectedClients = new Dictionary<string, bool>(); // Track client connections

        Console.WriteLine("Toad Server running on localhost:8080...");
        try
        {
            while (true)
            {
                byte[] receivedBytes = server.Receive(ref clientEndPoint);
                string message = Encoding.UTF8.GetString(receivedBytes);
                Console.WriteLine($"Received from {clientEndPoint}: {message}");

                string clientKey = clientEndPoint.ToString();
                ProcessToadPacket(server, clientEndPoint, message, clientKey, connectedClients);
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

    static void ProcessToadPacket(UdpClient server, IPEndPoint client, string message, string clientKey, Dictionary<string, bool> connectedClients)
    {
        string[] parts = message.Split('|');
        string header = parts[0].TrimEnd(']');
        string payload = parts.Length > 1 ? parts[1] : "";

        string[] headerParts = header.Split(':');
        string type = headerParts[0].TrimStart('[');
        string seq = headerParts[1];

        if (type == "START")
        {
            connectedClients[clientKey] = true;
            SendAck(server, client, seq);
            Console.WriteLine($"Toad connected: {clientKey}");
        }
        else if (type == "DATA" && connectedClients.ContainsKey(clientKey))
        {
            SendAck(server, client, seq);
            // Parse as BOWSER (HTTP/1.0) request
            string[] requestLines = payload.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (requestLines.Length > 0 && requestLines[0].StartsWith("GET"))
            {
                string response = "HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\nWelcome to Bowser's Kingdom!";
                SendToadPacket(server, client, (int.Parse(seq) + 1).ToString(), response);
            }
        }
        else if (type == "STOP" && connectedClients.ContainsKey(clientKey))
        {
            SendAck(server, client, seq);
            connectedClients.Remove(clientKey);
            Console.WriteLine($"Toad disconnected: {clientKey}");
        }
    }

    static void SendToadPacket(UdpClient server, IPEndPoint client, string seq, string payload)
    {
        string message = $"[DATA:{seq}|{payload}]";
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        server.Send(sendBytes, sendBytes.Length, client);
        Console.WriteLine($"Sent: {message}");
    }

    static void SendAck(UdpClient server, IPEndPoint client, string seq)
    {
        string ack = $"[ACK:{seq}|]";
        byte[] ackBytes = Encoding.UTF8.GetBytes(ack);
        server.Send(ackBytes, ackBytes.Length, client);
    }
}