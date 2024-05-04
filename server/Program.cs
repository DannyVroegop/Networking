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
using System.Data;


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
    bool endofFile = false;
    public EndPoint? connectedClient {get; private set;}
    int lastRecievedAck = 0;
    DateTime lastRecievedAckTime = DateTime.Now;

    Dictionary<string, string> sentmessages = new Dictionary<string, string>(); //Stores sent messages, when an ACK for an index (key) is recieved, remove that from the dict.

    private Queue<(EndPoint, Message?)> Message_Q = new Queue<(EndPoint, Message?)> ();

    private int timeout_time = 1000;
    private TimeSpan long_timeout = TimeSpan.FromSeconds(5);
    HashSet<int> acknowledgements = new HashSet<int>(); //HashSet for performance friendly reasons.

    int congestionwindow = 1;
    bool waitData = false;
    DateTime lastActivity = DateTime.Now;
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
                if (socket.Poll(timeout_time, SelectMode.SelectRead)) //Checks for incoming data.
                {
                    EndPoint clientendpoint;
                    try{
                        clientendpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    }
                    catch
                    {
                        clientendpoint = new IPEndPoint(IPAddress.Any, 0);
                    }
                    int bytes = socket.ReceiveFrom(buffer, ref clientendpoint);

                    string data = Encoding.ASCII.GetString(buffer, 0, bytes);
                    Message? message = JsontoMessage(data);
                    Message_Q.Enqueue((clientendpoint, message));
                    //Reset activity timer
                    lastActivity = DateTime.Now;

                    //Dequeue next message, handle the data
                    (EndPoint, Message?) next = Message_Q.Dequeue();

                    HandleData(next.Item2, next.Item1);
                }

                //Elapsed time check for timeout
                TimeSpan elapsedTime = DateTime.Now - lastActivity;


                //checks for activity timeout (5s)
                if (elapsedTime >= long_timeout && connectedClient != null)
                {
                    Console.WriteLine($"There has been no activity from client {connectedClient} for a while");
                    SendError(connectedClient,"Activity Timeout");
                }
            }
            catch (SocketException ex)
            {
                //incase client crashes
                Console.WriteLine($"!!Client has disconnected unexpectedly!! ", ex);
                EndConnection(connectedClient);
            }
            catch (Exception ex)
            {
                //umbrella error
                throw new ArgumentException("There was an error recieving data!", ex);
            }
            if (socket == null)
            {
                //create socket if it's removed
                CreateSocket();
            }
            
            
        }
    }
    
    #region handledata
    //Umbrella function to handle the different message types, used switch case for ease of use and default
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
                    Console.WriteLine($"Error recieved from client {message.Content}, terminating connection if one is present.");
                    EndConnection(clientendpoint);
                    break;
                case MessageType.Ack:
                    HandleAck(message, clientendpoint);
                    break;
                default:
                    Console.WriteLine($"Invalid message type => Client {message.Type} -> Server");
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
    //Create socket and bind it
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

    #region handle Hello
    //Check if the message is valid, has a valid content (int that is above 0), then sends welcome if its valid. If hello has already been recieved before a connection
    //attempt has been cancelled/ended, cancel the hello.
    public void ReceiveHello(Message message, EndPoint clientendpoint)
    {
        if (clientConnected == false || HelloRecieved == false)
        {
            try {
                Console.WriteLine($"Server has recieved a hello, threshold of {message.Content}. Sending Welcome...");
                if (message.Content == null || int.Parse(message.Content) <= 0)
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
    
    //Send hello to client
    public void SendWelcome(EndPoint clientendpoint)
    {
        try{
            Message message = new()
            {
                Type = MessageType.Welcome,
            };
            byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(message));
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
    //Handle data requests
    public void ReceiveRequestData(EndPoint clientendpoint, Message message)
    {
        //Checks if a client is connected and a hello has been recieved
        if(clientConnected == true && HelloRecieved == true)
        {
            //if null then there is no file to find.
            if (message.Content != null)
            {
                //checks if the requesting client is the same as the one we're currently connected to
                if (clientendpoint != connectedClient) { Console.WriteLine($"Request data recieved from non-connected client: {clientendpoint} - {connectedClient}"); return;}
                string requestedfile = message.Content;

                const int segmentsize = 500; //500 bytes per message, saving some for the message class and ID
                int totalsegment = 0;

                try
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), requestedfile);
                    try
                    {
                        FileStream sr = new FileStream(path, FileMode.Open, FileAccess.Read);
                        using (sr)
                        {
                            byte[] buffer = new byte[segmentsize];
                            endofFile = false;
                            while(!endofFile)
                            {
                                if(!waitData)
                                {
                                    int segmentindex = lastRecievedAck > 0 ? lastRecievedAck : 1;
                                    for (int i = 0; i < congestionwindow; i++)
                                    {
                                        if (!clientConnected || connectedClient == null)
                                        {
                                            return;
                                            //check if the client is still connected.
                                        }
                                        int bytestoread = sr.Read(buffer, 0, segmentsize);
                                        if (bytestoread > 0)
                                        {
                                            //if there are bytes left to read, get 500 bytes worth of data, add an index id for ACKs and then read the next lines.
                                            string segment = Encoding.ASCII.GetString(buffer, 0, bytestoread);
                                            string formatted = segmentindex.ToString("D4");
                                            string content = $"{formatted}{segment}";

                                            Message msg = new()
                                            {
                                                Type = MessageType.Data,
                                                Content = content
                                            };


                                            byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(msg));
                                            socket?.SendTo(send_data, clientendpoint);

                                            if(!sentmessages.ContainsKey(formatted))
                                            {
                                                sentmessages.Add($"{formatted}",$"{segment}");
                                            }

                                            segmentindex++;
                                            totalsegment++;
                                            //bytestoread = sr.Read(buffer, 0, segmentsize);
                                        }
                                        else
                                        {
                                            endofFile = true;
                                            break;
                                        }
                                        try
                                        {
                                            if (socket != null && socket.Poll(0, SelectMode.SelectRead)) //continue getting data while sending messages
                                            {
                                                byte[] recvBuffer = new byte[1000];
                                                int bytesReceived = socket.ReceiveFrom(recvBuffer, ref clientendpoint);

                                                string data = Encoding.ASCII.GetString(recvBuffer, 0, bytesReceived);
                                                Message? receivedMessage = JsontoMessage(data);
                                                lastActivity = DateTime.Now;

                                                HandleData(receivedMessage, clientendpoint);
                                            }
                                        }
                                        catch (SocketException ex)
                                        {
                                            //incase client crashes
                                            Console.WriteLine($"!!Client has disconnected unexpectedly!! ", ex);
                                            EndConnection(connectedClient);
                                            waitData = false;
                                            endofFile = true;
                                            return;
                                        }
                                    }
                                }
                                waitData = true;
                                try
                                {
                                    if (socket != null && socket.Poll(0, SelectMode.SelectRead)) //continue checking for data while waiting for the timeout
                                    {
                                        byte[] recvBuffer = new byte[1000];
                                        int bytesReceived = socket.ReceiveFrom(recvBuffer, ref clientendpoint);

                                        string data = Encoding.ASCII.GetString(recvBuffer, 0, bytesReceived);
                                        Message? receivedMessage = JsontoMessage(data);
                                        lastActivity = DateTime.Now;

                                        HandleData(receivedMessage, clientendpoint);
                                    }
                                }
                                catch (SocketException ex)
                                {
                                    //incase client crashes
                                    Console.WriteLine($"!!Client has disconnected unexpectedly!! ", ex);
                                    EndConnection(connectedClient);
                                    waitData = false;
                                    endofFile = true;
                                    return;
                                }
                                if (!clientConnected || connectedClient == null)
                                {
                                    EndConnection(connectedClient);
                                    return;
                                }
                                else
                                {
                                    HandleTimer(clientendpoint);
                                }
                            }
                            //When all data has been read and sent, send the end message to client and set allDataSent to true for the ACK timer.
                            Console.WriteLine("Preparing to send End message...");
                            Message mesg = new()
                            {
                                Type = MessageType.End
                            };

                            byte[] sendData = Encoding.ASCII.GetBytes(ObjectToJson(mesg));
                            socket?.SendTo(sendData, clientendpoint); 
                            Console.WriteLine("Final message of type End has been sent");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
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
        //When the client is connecting clientconnected will be false, check if this was done in order and then connects client, recalling the function.
        else if (clientConnected == false || HelloRecieved == true)
        {
            Console.WriteLine("Server has recieved data request, in order. Connection to this client has been made: " + clientendpoint);
            clientConnected = true;
            connectedClient = clientendpoint;
            ReceiveRequestData(clientendpoint, message);
        }
        else if (HelloRecieved == false) //message order check
        {
            Console.WriteLine("Server recieved RequestData before Hello, Invalid order");
        }
    }
    #endregion


    #region Acknowledgement

    //Checks if the ack is an ack, has content, if there is a client that is connected and if it is in order
    public void HandleAck(Message message, EndPoint clientendpoint)
    {
        if (message.Type == MessageType.Ack && message.Content != null && connectedClient != null && HelloRecieved == true)
        {
            //checks if the ack index (content) was sent, if it was remove this from the sentmessages list. also checks for format.
            int acksegment;
            if (int.TryParse(message.Content, out acksegment))
            {
                if (acksegment.ToString("D4").Length == 4 && acksegment > 0)
                {
                    string tocheck = acksegment.ToString("D4");
                    acknowledgements.Add(acksegment);
                    if (acksegment > lastRecievedAck)
                    {
                        lastRecievedAck = acksegment+1;
                    }
                    if (sentmessages.ContainsKey(tocheck))
                    {
                        sentmessages.Remove(tocheck);
                    }
                    Console.WriteLine($"Recieved ACK: {tocheck}");
                }
                else
                {
                    Console.WriteLine("Invalid format for ACK recieved! invalid length on index!");
                    SendError(clientendpoint, "Invalid ACK format, invalid length on index");
                }
            }
            else
            {
                Console.WriteLine("Invalid format for ACK recieved! Contains non-digit");
                SendError(clientendpoint, "Invalid ACK format, Contains non-digit");
            }
        }
    }


    #endregion

    #region Timeout Handling
    public void HandleTimer(EndPoint clientendpoint)
    {
        if (DateTime.Now - lastRecievedAckTime >= TimeSpan.FromSeconds(1))
        {
            waitData = false;
            lastRecievedAckTime = DateTime.Now;
            List<int> missingACK = sentmessages.Keys
                .Select(key => int.Parse(key))
                .Where(key => !acknowledgements.Contains(key))
                .ToList();

            if (missingACK.Any())
            {
                //Console.WriteLine("Acks missing, resetting slowstart");
                congestionwindow = 1;
                foreach(var index in missingACK)
                {
                    // string contentToSend = sentmessages[index.ToString("D4")];
                    //ResendData(contentToSend, index, clientendpoint);
                   lastRecievedAck = missingACK.First();
                   Console.WriteLine($"ACK missing, resetting congestionwindow, lastack: {lastRecievedAck}");
                }
            }
            else
            {
                string con = Math.Min(congestionwindow*2, clientThreshold) <= clientThreshold ? $", double window: OLD WINDOW {congestionwindow} - NEW {Math.Min(congestionwindow*2, clientThreshold)}" : $"WINDOW: {clientThreshold}.";
                Console.WriteLine("All acks recieved" + con);
                congestionwindow = Math.Min(congestionwindow*2, clientThreshold);
            }
        }
        else
        {
            //Console.WriteLine("Timeout has not yet ended");
            return;
        }
    }
    
    //function to resend the data, doesnt use a slowstart algorithm because of its single message(s) nature.
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
    //terminate connection function, reinstate socket
    public void EndConnection(EndPoint? clientendpoint)
    {
        connectedClient = null;
        clientConnected = false;
        congestionwindow = 1;
        clientThreshold = 0;
        sentmessages.Clear();
        acknowledgements.Clear();
        waitData = false;
        lastRecievedAck = 0;
        endofFile = false;
        HelloRecieved = false;
        sentmessages.Clear();

        if (socket != null)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
            catch
            {
                Console.WriteLine("Something went wrong cleaning up the socket..");
            }
        }

        Console.WriteLine("Connection with client has ended, awaiting new client...");
    }
    #endregion
    #region ErrorHandling
    //Error handling, also terminates connection if a client is connected, or terminates a connection attempt if a hello has been recieved
    public void SendError(EndPoint ClientEndPoint, string error)
    {
        Message msg = new()
        {
            Type = MessageType.Error,
            Content = error
        };

        byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(msg));
        try
        {
            socket?.SendTo(send_data, ClientEndPoint);
        }
        catch (SocketException ex) {Console.WriteLine("Could not send error message to client: ", ex);}
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


}