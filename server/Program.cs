﻿using System;
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

    public void start()
    {

    }

    public IPAddress getIP()
    {
        string hostName = Dns.GetHostName();
        IPAddress userIP = Dns.GetHostByName(hostName).AddressList[0];
        return userIP;
    }

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

    public void CreateSocket()
    {
        byte[] buffer = new byte[1000];
        Socket sock;

        IPAddress iPAddress = getIP();

        IPEndPoint ServerEndpoint = new IPEndPoint(iPAddress, 32000);
        IPEndPoint localEndpoint = new IPEndPoint(iPAddress, 32000);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remoteEP = (EndPoint) sender;

        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(localEndpoint);
    }

    
    //TODO: create all needed objects for your sockets 

    //TODO: keep receiving messages from clients
    // you can call a dedicated method to handle each received type of messages

    //TODO: [Receive Hello]
    public void ReceiveHello(Socket sock)
    {
        byte[] buffer = 
    }

    //TODO: [Send Welcome]

    //TODO: [Receive RequestData]

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}