using System.ComponentModel;
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
    Socket? sock { get; set; }
    bool running = false;
    string? FileName = "hamlet.txt";
    List<string> data = new List<string>();
    DateTime lastActivity = DateTime.Now;
    private TimeSpan long_timeout = TimeSpan.FromSeconds(5);

    private EndPoint? connectedServer;
    bool helloSent = false;


    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
   
    #region start
    public void start()
    {
        ClearFiles(FileName="hamlet.txt");
        running = true;
        createSocket();
        Console.WriteLine("Client is starting...Attempting to send Hello");
        
        if (sock != null)
        {
            SendHello(sock, 20);
        }

        byte[] buffer = new byte[1000];
        while (running && sock != null)
        {
            try
            {
                if(helloSent == true && sock.Poll(1000, SelectMode.SelectWrite)) 
                {
                    EndPoint serverendpoint;
                    try{
                        serverendpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    }
                    catch
                    {
                        serverendpoint = new IPEndPoint(IPAddress.Any, 0);
                    }
                    int bytes = sock.ReceiveFrom(buffer, ref serverendpoint);

                    string data = Encoding.ASCII.GetString(buffer, 0, bytes);
                    Message? message = JsontoMessage(data);
                    HandleData(message, serverendpoint);
                    TimeSpan elapsedTime = DateTime.Now - lastActivity;


                    //checks for activity timeout (5s)
                    if (elapsedTime >= long_timeout && connectedServer != null)
                    {
                        Console.WriteLine($"There has been no activity from Server for a while");
                        SendError(serverendpoint,"Activity Timeout");
                    }
                }
            }
            catch (SocketException ex)
            {
                //incase client crashes
                Console.WriteLine($"!!Server has disconnected unexpectedly or is not online!! ", ex.Message);
                Terminate();
            }
        }
    }
    //TODO: create all needed objects for your sockets 
    #endregion
    #region file
    public void ClearFiles(string filename)
    {
        
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), filename);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                Console.WriteLine($"There was an error deleting {filename}");
            }
        }

    }
    
     public void HandleFile(string filename)
    {
        if (!File.Exists(filename))
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), filename);
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    foreach (string line in data)
                    {
                        if (line.Length >= 4)
                        {
                            sw.Write(line.Substring(4));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data could not been written to the file", ex);
            }
        }
        else
        {
            Console.WriteLine("Filename already exists");
        }


    }


    public void AddData()
    {
        try
        {
            data.Sort();
            HandleFile(FileName="hamlet.txt");

        }
        catch (Exception ex)
        {
            Console.WriteLine("The data could not be added", ex);
        }
    }


    #endregion
    #region incoming message handling
    public void HandleData(Message? message, EndPoint serverendpoint) // clientendpoint in server endpoint veranderd
    {
        if (message == null) {return;}
        switch(message.Type)
        {
            case MessageType.Welcome:
                ReceiveWelcome(message, serverendpoint);
                break;
            case MessageType.Data:
                SendAck(serverendpoint, message.Content);
                break;
            case MessageType.End:
                if (connectedServer != null && message.Content == null)
                {
                    AddData();
                    Terminate();
                }
                else
                {
                    Console.WriteLine("Invalid End message recieved, has content || recieved before being connected");
                }
                break;
            case MessageType.Error:
                Console.WriteLine($"Error from server: {message.Content}");
                Terminate();
                break;
            default:
                Console.WriteLine("Invalid message type => Client -> Server");
                break;
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

    public void createSocket()
    {
        try {
            IPAddress ip = getIP();
            sock = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

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
    #endregion
    #region hello sending
    public void SendHello(Socket sock, int threshold = 20) //default parameter of 20, function in start called with 20 aswell.
    {
        try
        {
            IPAddress iPAddress = getIP();
            IPEndPoint ServerEndpoint = new IPEndPoint(iPAddress, 32000);
            IPEndPoint sender = new IPEndPoint(iPAddress, 0);
            EndPoint remoteEP = (EndPoint) sender;

            Message message = new()
            {
                Type = MessageType.Hello,
                Content = threshold.ToString()
            };
            byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(message));

            

            sock.SendTo(send_data, ServerEndpoint);
            helloSent = true;
        }
        catch(SocketException ex)
        {
            Console.WriteLine("Error in socket connection: ", ex);
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error in sending Hello!", ex);
        }
    }
    #endregion
    #region recieve welcome
    public void ReceiveWelcome(Message? message, EndPoint serverEndpoint)
    {
        if (helloSent == true)
        {
            if (message != null && message.Content == null)
            {
                Console.WriteLine("Welcome message has been received"); 
                connectedServer = serverEndpoint;
                SendRequestData(serverEndpoint);
            }
            else
            {
                Console.WriteLine("Welcome message not found, OR there is content in the Welcome message.");
            }
        }
        else
        {
            Console.WriteLine("Welcome recieved before hello has been sent?");
        }
    }
    #endregion
    #region send data request
    public void SendRequestData(EndPoint serverEndpoint)
    {
        if (sock != null && connectedServer != null)
        {
            try
            {
                Message message = new Message
                {
                    Type = MessageType.RequestData,
                    Content = FileName
                };
                
                byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(message));

                sock?.SendTo(send_data, serverEndpoint);
                Console.WriteLine("The Requested data message has been sent to the Server.");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("The requested data could not be send to the server", ex);
            }
        }
        else
        {
            Console.WriteLine("Invalid socket or invalid order (sending before connecting)");
        }
    }
    #endregion
   
    #region send ack
    public void SendAck(EndPoint serverEndpoint, string? index)
    {
        if (index == null || connectedServer == null) {Console.WriteLine("Invalid message, or invalid order (sending before connecting");return;}

        if (!data.Contains(index))
        {
            data.Add(index);
        }
        else
        {
            return;
        }
        

        try
        {
            Message message = new Message
            {
                Type = MessageType.Ack,
                Content = index.Substring(0,4)
            };
            byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(message));
            sock?.SendTo(send_data, serverEndpoint);
            lastActivity = DateTime.Now;
            Console.WriteLine($"Sent ACK for: {message.Content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("The Ack could not be send to the server", ex);
        }

    }
    #endregion

    #region terminate
    public void Terminate()
    {
        try
        {
            Console.WriteLine("The client will be terminated");
            running = false;
            connectedServer = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("The client could not be terminated", ex);
        }
    }
    #endregion
    #region error handling
    public void SendError(EndPoint ServerEndPoint, string error)
    {
        Message msg = new()
        {
            Type = MessageType.Error,
            Content = error
        };

        byte[] send_data = Encoding.ASCII.GetBytes(ObjectToJson(msg));
        try
        {
            sock?.SendTo(send_data, ServerEndPoint);
        }
        catch {Console.WriteLine("Could not send error to server");}
        Terminate();
    }
    #endregion

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