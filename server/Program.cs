using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using MessageNS;


// Do not modify this class

/*

    Danny Vroegop 1058835
    Soufiane Boufarache 1053961

*/


class Program
{
    static void Main(string[] args)
    {
        ServerUDP sUDP = new ServerUDP();
        sUDP.start();
    }
}

class ServerUDP
{

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    bool running = false;
    Socket socket { get; set; }
    bool clientConnected { get; set; }
    int clientThreshold = 0;

    public void start()
    {
        running = true;
        
        Console.Write("Attempting to start server...");
        CreateSocket();
        
        byte[] buffer = new byte[1000]; 
        while (running)
        {
            try
            {
                EndPoint clientendpoint;
                try{
                    clientendpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                }
                catch (Exception ex)
                {
                    clientendpoint = new IPEndPoint(IPAddress.Any, 0);
                }
                int bytes = socket.ReceiveFrom(buffer, ref clientendpoint);

                string data = Encoding.UTF8.GetString(buffer, 0, bytes);
                Message message = JsontoMessage(data);
                HandleData(message, clientendpoint);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("There was an error recieving data!", ex);
            }
            if (socket == null)
            {
                CreateSocket();
            }
            
            
        }
    }

    public void HandleData(Message message, EndPoint clientendpoint)
    {
        switch(message.Type)
        {
            case MessageType.Hello:
                ReceiveHello(message, clientendpoint);
                break;
            default:
                Console.WriteLine("Invalid message type => Client -> Server");
                break;
        }
    }

    public IPAddress getIP()
    {
        string hostName = Dns.GetHostName();
        IPAddress userIP = Dns.GetHostEntry(hostName).AddressList[0];
        return userIP;
        
    }

    // Convert object to byte array
    public static string ObjectToJson(Object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        return json;
    }

    // Convert byte array to Message type
    public static Message JsontoMessage(string json)
    {
        Message? msg = JsonSerializer.Deserialize<Message>(json);
        return msg;
    }

    public void CreateSocket()
    {
        Socket sock;


        try
        {
            IPAddress iPAddress = getIP();
            IPEndPoint localEndpoint = new IPEndPoint(iPAddress, 32000);
            sock = new Socket(iPAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(localEndpoint);
            socket = sock;
            Console.Write("SUCCEEDED..Listening on port 32000\n");
        }
        catch (Exception ex)
        {
            Console.Write("ERROR\n");
            running = false;
            throw new ArgumentException($"{ex}  -  Socket could not be created! [SERVER]", nameof(sock));
        }
        
    }
        /* ASSIGNMENT TO DO
           The client sends a hello message, the content is a number (default value 20) that represents the threshold. AKA how many message per x it can handle
           the server sends back an welcome message, this has no content (check validation?)
           the client sends a RequestData, the content is the filename of a file in the root directory of the server (hamlet.txt)
           (check if in present in server)
           the server will send the data (lines from hamlet), this data has to be split into lines or even letters (test and find out ourselves)
           the server will double the amount of messages with the data sent until it reaches the threshold after which it will stop doubling
                if the calculated new value is more than the default value, the server will
                stop doubling the value and continue to send the last known amount
            data has the following structue:
                4 numbers that indicate the index of the data (eg 0001)
                data sent
            
            the client sends an ACK everytime the data is recieved. the content is the index of the data (eg 0001)
            the error message is to communicate to the other party that an error occured, be specific about the error in the content.
            upon recieving an error the server will reset the communication (and be ready again)
            the client will terminate, printing the error

            End has no content and marks that the last data was send
            welcome data and end can only be sent from the server
            hello requestdata and ack can only be sent from the client.
        */
    
    //TODO: create all needed objects for your sockets 

    //TODO: keep receiving messages from clients
    // you can call a dedicated method to handle each received type of messages

    //TODO: [Receive Hello]
    public void ReceiveHello(Message message, EndPoint clientendpoint)
    {
        if (clientConnected == false)
        {
            Console.WriteLine($"Server has recieved an hello, thershold of {message.Content}. Sending Welcome...");
            clientThreshold = int.Parse(message.Content);
            SendWelcome(clientendpoint);
        }
    }

    //TODO: [Send Welcome]
    public void SendWelcome(EndPoint clientendpoint)
    {
        try{
            Message message = new();
            message.Type = MessageType.Welcome;
            message.Content = null;
            byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(message));
            socket.SendTo(send_data, clientendpoint);
            Console.WriteLine("Welcome has been sent to client, awaiting data request before connecting.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"There has been an error sending the welcome message!: {ex.Message}");
        }
    }
    //TODO: [Receive RequestData]

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}