using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using MessageNS;
using System.IO;


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
    Socket? socket { get; set; }
    bool clientConnected { get; set; }
    int clientThreshold = 0;
    bool HelloRecieved = false;
    public EndPoint? connectedClient {get; private set;}

    Dictionary<string, string> sentmessages = new Dictionary<string, string>(); //Stores sent messages, when an ACK for an index (key) is recieved, remove that from the dict.

    private Queue<(EndPoint, Message?)> Message_Q = new Queue<(EndPoint, Message?)> ();

    private int timeout_time = 5000;
    HashSet<int> acknowledgements = new HashSet<int>(); //HashSet for performance friendly reasons.

    public void start()
    {
        running = true;
        
        Console.Write("Attempting to start server...");
        CreateSocket();
        
        byte[] buffer = new byte[1000]; 
        while (running && socket != null)
        {
            try
            {
                EndPoint clientendpoint;
                try{
                    clientendpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client does not use IP6", ex);
                    clientendpoint = new IPEndPoint(IPAddress.Any, 0);
                }
                int bytes = socket.ReceiveFrom(buffer, ref clientendpoint);

                string data = Encoding.ASCII.GetString(buffer, 0, bytes);
                Message? message = JsontoMessage(data);
                Message_Q.Enqueue((clientendpoint, message));

                (EndPoint, Message?) next = Message_Q.Dequeue();
                HandleData(next.Item2, next.Item1);
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
    
    #region handledata
    public void HandleData(Message? message, EndPoint clientendpoint)
    {
        if (message == null)
        {
            Console.WriteLine("Recieved message is of type: NULL.");
        }
        else{
            switch(message.Type)
            {
                case MessageType.Hello:
                    ReceiveHello(message, clientendpoint);
                    break;
                case MessageType.RequestData:
                    ReceiveRequestData(clientendpoint, message);
                    break;
                case MessageType.Error:
                    Console.WriteLine("Error recieved from client, terminating connection if one is present.");
                    EndConnection(clientendpoint);
                    break;
                case MessageType.Ack:
                    HandleAck(message, clientendpoint);
                    break;
                default:
                    Console.WriteLine("Invalid message type => Client -> Server");
                    SendError(clientendpoint, $"Invalid message type => Client -> Server");
                    break;
            }
        }
        
    }
    #endregion

    #region utility
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
    public static Message? JsontoMessage(string json)
    {
        try
        {
            Message? msg = JsonSerializer.Deserialize<Message>(json);
            return msg;
        }
        catch (Exception ex) { Console.WriteLine($"Message: {json} cannot be converted to a Message! [SERVER]", ex); return default;}
    }
    #endregion

    #region Socket creation
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
    #endregion
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
    #region handle Hello
    public void ReceiveHello(Message message, EndPoint clientendpoint)
    {
        if (clientConnected == false || HelloRecieved == false)
        {
            try {
                Console.WriteLine($"Server has recieved a hello, threshold of {message.Content}. Sending Welcome...");
                if (message.Content == null)
                {
                    Console.WriteLine("Invalid threshold format in Hello!, terminating connection attempt..");
                    SendError(clientendpoint, $"Invalid threshold format in Hello!, terminating connection attempt..");
                    return;
                }
                clientThreshold = int.Parse(message.Content);
                HelloRecieved = true;
                SendWelcome(clientendpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid message treshold recieved, are you certain the content is a number?", ex);
                SendError(clientendpoint, $"Invalid message treshold recieved, are you certain the content is a number?");
            }
        }
        else if (HelloRecieved == true)
        {
            Console.WriteLine("Server has already recieved an hello!");
        }
    }
    #endregion

    #region welcome
    //TODO: [Send Welcome]
    public void SendWelcome(EndPoint clientendpoint)
    {
        try{
            Message message = new()
            {
                Type = MessageType.Welcome,
                Content = ""
            };
            byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(message));
            socket?.SendTo(send_data, clientendpoint);
            Console.WriteLine("Welcome message has been sent to client, awaiting data request before connecting.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"There has been an error sending the welcome message!: {ex.Message}");
            SendError(clientendpoint, $"There has been an error sending the welcome message!");
        }
    }
    #endregion

    #region  sending data
    public void ReceiveRequestData(EndPoint clientendpoint, Message message)
    {
        if(clientConnected == true && HelloRecieved == true)
        {
            if (message.Content != null)
            {
                if (clientendpoint != connectedClient) { Console.WriteLine($"Request data recieved from non-connected client: {clientendpoint} - {connectedClient}"); return;}
                string requestedfile = message.Content;

                const int segmentsize = 500; //500 bytes per message, saving some for the message class and ID
                int congestionwindow = 1;
                try
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), requestedfile);
                    try
                    {
                        FileStream sr = new FileStream(path, FileMode.Open, FileAccess.Read);
                        using (sr)
                        {
                            int segmentindex = 1;
                            byte[] buffer = new byte[segmentsize];

                            int bytestoread;
                            while((bytestoread = sr.Read(buffer, 0, segmentsize)) > 0)
                            {
                                for (int i = 0; i < congestionwindow; i++)
                                {
                                    if (bytestoread > 0)
                                    {
                                        string segment = Encoding.ASCII.GetString(buffer, 0, bytestoread);
                                        string formatted = segmentindex.ToString("D4");
                                        string content = $"{formatted}{segment}";

                                        Message msg = new()
                                        {
                                            Type = MessageType.Data,
                                            Content = content
                                        };


                                        byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(msg));
                                        socket?.SendTo(send_data, clientendpoint);

                                        sentmessages.Add($"{formatted}",$"{segment}");

                                        segmentindex++;
                                        bytestoread = sr.Read(buffer, 0, segmentsize);
                                    }
                                    
                                }
                                if (congestionwindow >= clientThreshold)
                                {
                                    congestionwindow = clientThreshold;
                                }
                                else
                                {
                                    congestionwindow *= 2;
                                    if (congestionwindow >= clientThreshold) {congestionwindow = clientThreshold;}
                                }
                            }
                            Console.WriteLine("Preparing to send End message...");
                            Message mesg = new()
                            {
                                Type = MessageType.End
                            };

                            byte[] sendData = Encoding.UTF8.GetBytes(ObjectToJson(mesg));
                            socket?.SendTo(sendData, clientendpoint); 
                            Console.WriteLine("Final message of type End has been sent");
                            System.Timers.Timer timer = new System.Timers.Timer(timeout_time);
                            timer.Elapsed += (sender, e) => HandleTimer(clientendpoint);
                            timer.AutoReset = false;
                            timer.Start();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("There has been a problem reading the requested file");
                        SendError(clientendpoint, $"There has been a problem reading the requested file");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"File {message.Content} could not be found!", ex);
                    SendError(clientendpoint, $"File {message.Content} could not be found!");
                }
            }
            else {Console.WriteLine("Client has request an invalid file: NULL"); SendError(clientendpoint, "File of type NULL sent");}
        }
        else if (clientConnected == false || HelloRecieved == true)
        {
            Console.WriteLine("Server has recieved data request, in order. Connection to this client has been made: " + clientendpoint);
            clientConnected = true;
            connectedClient = clientendpoint;
            ReceiveRequestData(clientendpoint, message);
        }
        else if (HelloRecieved == false)
        {
            Console.WriteLine("Server recieved RequestData before Hello, Invalid order");
        }
    }
    #endregion


    #region Acknowledgement

    public void HandleAck(Message message, EndPoint clientendpoint)
    {
        if (message.Type == MessageType.Ack && message.Content != null)
        {
            int acksegment;
            if (int.TryParse(message.Content, out acksegment))
            {
                acknowledgements.Add(acksegment);
                if (sentmessages[acksegment.ToString("D4")] != null)
                {
                    sentmessages.Remove(acksegment.ToString("D4"));
                }
                //Console.WriteLine($"ACK of index: 0{acksegment} has been recieved");
            }
            else
            {
                Console.WriteLine("Invalid format for ACK recieved!");
                SendError(clientendpoint, "Invalid ACK format");
            }
        }
    }


    #endregion

    #region Timeout Handling
    
    
    
    public void HandleTimer(EndPoint clientendpoint)
    {
        List<int> missingACK = sentmessages.Keys
            .Select(key => int.Parse(key))
            .Where(key => !acknowledgements.Contains(key))
            .ToList();

        if (missingACK.Any())
        {

            foreach(var index in missingACK)
            {
                string contentToSend = sentmessages[index.ToString("D4")];
                ResendData(contentToSend, index, clientendpoint);
            }
        }
    }
    
    public void ResendData(string content, int segmentindex, EndPoint clientendpoint)
    {
        string formatted = segmentindex.ToString("D4");

        string sentcontent = $"{formatted}{content}";

        Message msg = new Message()
        {
            Type = MessageType.Data,
            Content = sentcontent
        };

        byte[] sendData = Encoding.ASCII.GetBytes(ObjectToJson(msg));

        socket?.SendTo(sendData, clientendpoint);

        sentmessages[formatted] = content;
    }
    
    #endregion


    #region endconnection
    public void EndConnection(EndPoint clientendpoint)
    {
        if (connectedClient == clientendpoint)
        {
            connectedClient = null;
            clientConnected = false;
        }
        HelloRecieved = false;
        sentmessages.Clear();
        Console.WriteLine("Connection with client has ended, awaiting new client...");
    }
    #endregion
    //TODO: [Receive RequestData]

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]
    #region ErrorHandling
    public void SendError(EndPoint ClientEndPoint, string error)
    {
        Message msg = new()
        {
            Type = MessageType.Error,
            Content = error
        };

        byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(msg));
        socket?.SendTo(send_data, ClientEndPoint);

        if (clientConnected == true && connectedClient == ClientEndPoint)
        {
            EndConnection(ClientEndPoint);
        }
        else if (HelloRecieved == true)
        {
            HelloRecieved = false;
        }
    }
    #endregion
    //TODO: create all needed methods to handle incoming messages


}