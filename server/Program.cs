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

    public void start()
    {
        running = true;
        Console.Write("Attempting to start server...");
        while (running)
        {
            if (socket == null)
            {
                CreateSocket();
            }
            if (clientConnected == false)
            {
                //awaitclient();
            }
            ReceiveHello();
        }
    }

    private void awaitclient()
    {
        Console.Write("Awaiting client");
        int dotcount = 0;
        while (clientConnected == false)
        {
            Console.Write(".");
            dotcount++;
            Thread.Sleep(1000);
            if (dotcount == 3)
            {
                Console.Write("\b\b\b   \b\b\b");
                dotcount = 0;
            }
        }
    }

    public IPAddress getIP()
    {
        string hostName = Dns.GetHostName();
        IPAddress userIP = Dns.GetHostByName(hostName).AddressList[0];
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
            Console.Write("SUCCEEDED\n");
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
    public void ReceiveHello()
    {
        if (clientConnected == false)
        {
            byte[] buffer = new byte[1000]; 
            IPAddress ipAddress = getIP();   
            IPEndPoint sender = new IPEndPoint(ipAddress, 0);
            EndPoint remoteEP = (EndPoint) sender;
            int b = socket.ReceiveFrom(buffer, ref remoteEP);
            string data = Encoding.ASCII.GetString(buffer, 0, b);
            clientConnected = true;
            Console.WriteLine(data);
        }
    }

    //TODO: [Send Welcome]

    //TODO: [Receive RequestData]

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}