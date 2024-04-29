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
    Socket sock { get; set; }

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        sock = createSocket();
        if (sock != null)
        {
            SendHello(sock, 20);
        }
    }
    //TODO: create all needed objects for your sockets 

        public IPAddress getIP()
    {
        
        string hostName = Dns.GetHostName();
        IPAddress userIP = Dns.GetHostByName(hostName).AddressList[0];
        return userIP;
        
        // IPAddress ip = IPAddress.Parse("127.0.0.1");
        // return ip;
    
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
    

    public static Message JsonToMessage(string json)
    {
        Message? msg = JsonSerializer.Deserialize<Message>(json);
        return msg;
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
        byte[] send_data = Encoding.UTF8.GetBytes(ObjectToJson(message));

        

        sock.SendTo(send_data, ServerEndpoint);
    }

    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]

    //TODO: [Receive Data]

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]


}