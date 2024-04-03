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

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {

    }
    //TODO: create all needed objects for your sockets 

        public IPAddress getIP()
    {
        string hostName = Dns.GetHostName();
        IPAddress userIP = Dns.GetHostByName(hostName).AddressList[0];
        return userIP;
    }

    public Socket? createSocket()
    {
        Socket sock;
        try {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            return sock;
        }
        catch
        {
            throw new ArgumentException("Socket could not be created", nameof(sock));
        }
    }

    //TODO: [Send Hello message]
    public static byte[] ObjectToByte(Object obj)
    {
        BinaryFormatter b = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            b.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static Message ByteToMessage(byte[] arr)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm  = new BinaryFormatter();
            memStream.Write(arr, 0, arr.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object message = binForm.Deserialize(memStream);
            return (Message)message;
        }
    }

    public void SendHello(Socket sock)
    {
        byte[] buffer = new byte[1000];

        IPAddress iPAddress = getIP();

        IPEndPoint ServerEndpoint = new IPEndPoint(iPAddress, 32000);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remoteEP = (EndPoint) sender;

        Message message = new();
        message.Type = MessageType.Hello;
        message.Content = "1";
        byte[] send_data = ObjectToByte(message);

        sock.SendTo(send_data, remoteEP);
    }

    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]

    //TODO: [Receive Data]

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]


}