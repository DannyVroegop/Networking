﻿using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using MessageNS;

// SendTo();

/*

    Danny Vroegop 1058835
    Soufiane Boufarache 1053961

*/


class Program
{
    static void Main(string[] args)
    {
        ClientUDP cUDP = new ClientUDP();
        cUDP.start();
    }
}

class ClientUDP
{
    Socket sock { get; set; }
    bool running = false;

    int Index = 0;

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        running = true;
        sock = createSocket();
        Console.WriteLine("Client is starting...Attempting to send Hello");
        
        if (sock != null)
            {
                SendHello(sock, 20);
            }

        byte[] buffer = new byte[1000];
        while (running)
        {
            EndPoint serverendpoint;
                try{
                    serverendpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                }
                catch (Exception ex)
                {
                    serverendpoint = new IPEndPoint(IPAddress.Any, 0);
                }
                int bytes = sock.ReceiveFrom(buffer, ref serverendpoint);

                string data = Encoding.ASCII.GetString(buffer, 0, bytes);
                Message message = JsontoMessage(data);
                HandleData(message, serverendpoint);
        }
    }
    //TODO: create all needed objects for your sockets 

    public void HandleData(Message message, EndPoint serverendpoint) // clientendpoint in server endpoint veranderd
    {
        switch(message.Type)
        {
            case MessageType.Welcome:
                ReceiveWelcome(message, serverendpoint);
                break;
            case MessageType.Data:
                SendAck(serverendpoint);
                break;
            case MessageType.End:
                Terminate();
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

    public Socket? createSocket()
    {
        Socket sock;
        try {
            IPAddress ip = getIP();
            sock = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            
            return sock;
        }
        catch
        {
            throw new ArgumentException("Socket could not be created! [CLIENT]", nameof(sock));
        }
    }

    //TODO: [Send Hello message]
    public static string ObjectToJson(Object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        return json;
    }
    

    public static Message? JsontoMessage(string json)
    {
        try
        {
            Message? msg = JsonSerializer.Deserialize<Message>(json);
            return msg;
        }
        catch (Exception ex) { Console.WriteLine($"Message: {json} cannot be converted to a Message! [SERVER]", ex); return default;}
    }

    public void SendHello(Socket sock, int thershold = 20)
    {
        IPAddress iPAddress = getIP();

        IPEndPoint ServerEndpoint = new IPEndPoint(iPAddress, 32000);
        IPEndPoint sender = new IPEndPoint(iPAddress, 0);
        EndPoint remoteEP = (EndPoint) sender;

        Message message = new();
        message.Type = MessageType.Hello;
        message.Content = thershold.ToString();
        byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(message));

        

        sock.SendTo(send_data, ServerEndpoint);
    }

    public void ReceiveWelcome(Message? messgae, EndPoint serverEndpoint)
    {
        Console.WriteLine("Welcome message has been received"); 
        SendRequestData(serverEndpoint); 
    }


    public void SendRequestData(EndPoint serverEndpoint)
    {
        if (sock != null)
        {
            try
            {
                Message message = new Message
                {
                    Type = MessageType.RequestData,
                    Content = "hamlet.txt"
                };
                
                byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(message));
                Index = 0;
                sock?.SendTo(send_data, serverEndpoint);
                Console.WriteLine("The Requested data message has been sent to the client, awaiting data request before connecting.");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("The requested data could not be send to the server", ex);
            }
        }
    }

    public void SendAck(EndPoint serverEndpoint)
    {
        try
        {
            Message message = new Message
            {
                Type = MessageType.Ack,
                Content = Index.ToString("0000")
            };
            Index ++;

            byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(message));
            sock.Send(send_data);
        }
        catch (Exception ex)
        {
            Console.WriteLine("The Ack could not be send to the server", ex);
        }

    }

    public void Terminate()
    {
        try
        {
        Console.WriteLine("The activity will be terminated");
        sock.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("The activity could not bee terminated", ex);
        }
    }

    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]

    //TODO: [Receive Data]

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]

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

}