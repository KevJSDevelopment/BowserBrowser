// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;

TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);

try 
{
    listener.Start();
    Console.WriteLine("Server started, listening on localhost:8080");

    while(true)
    {
        TcpClient client = listener.AcceptTcpClient();
        Console.WriteLine("Client connected");

        client.Close();
        Console.WriteLine("Client disconnected");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}


