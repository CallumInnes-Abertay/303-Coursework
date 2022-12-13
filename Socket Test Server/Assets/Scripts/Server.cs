using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public delegate void PacketHandler(int _fromClient, Packet _packet);

    public static Dictionary<int, Client> clients = new();
    public static Dictionary<int, Player> players = new();

    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;
    public static int MaxPlayers { get; private set; }
    private static int Port { get; set; }

    /// <summary>
    /// Starts the server
    /// </summary>
    /// <param name="_maxPlayers">The max amount of connections to this server.</param>
    /// <param name="_port">The port they connect to.</param>
    public static void Start(int _maxPlayers, int _port)
    {
        MaxPlayers = _maxPlayers;
        Port = _port;

        Debug.Log("Starting server...");
        InitialiseServerData();

        //Set up TCP listener.
        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

        //Set up UDP listener binding to the servers port.
        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on port {Port}.");
    }

    /// <summary>
    /// Initialise the methods to handle every client request.
    /// </summary>
    private static void InitialiseServerData()
    {
        //Set every potential player up as a client assigning it a unique id. 
        for (var i = 1; i <= MaxPlayers; i++) clients.Add(i, new Client(i));

        //Sets up packet handler dictionary with all methods to handle client requests.
        packetHandlers = new Dictionary<int, PacketHandler>
        {
            { (int)ClientPackets.WelcomeReceived, ServerHandle.WelcomeReceived },
            { (int)ClientPackets.PlayerMovement, ServerHandle.PlayerMovement },
            { (int)ClientPackets.CollectableCollision, ServerHandle.CollectableCollision }
        };
        Debug.Log("Initialised packets.");
    }

    /// <summary>
    /// Handles any new TCP connections.
    /// </summary>
    /// <param name="_result">The result of the async operation</param>
    private static void TCPConnectCallback(IAsyncResult _result)
    {
        var client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");

        //Adds the incoming connection to one of the potential sockets 
        for (var i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(client);
                return;
            }
        }

        //If the for loop has ended that means that the server is full.
        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    /// <summary>
    /// Receives incoming data from the client
    /// </summary>
    /// <param name="_result">The result of the async operation.</param>
    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            //Ends a pending asynchronous receive and returns the bytes from the packet.
            byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);

            //Begins listening for UDP requests from clients.
            udpListener.BeginReceive(UDPReceiveCallback, null);

            //If less than an int then there must be no more data left to return early.
            if (data.Length < 4)
                return;

            using (var _packet = new Packet(data))
            {
                var clientId = _packet.ReadInt();

                if (clientId == 0)
                    return;

                //If not connected then connect to the endpoint.
                if (clients[clientId].udp.endPoint == null)
                {
                    clients[clientId].udp.Connect(clientEndPoint);
                    return;
                }

                //If it is now connected and not sending the wrong id start handling the received data.
                if (clients[clientId].udp.endPoint.Equals(clientEndPoint))
                    clients[clientId].udp.HandleData(_packet);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error receiving UDP data: {e}");
            udpListener.BeginReceive(UDPReceiveCallback, null);
        }
    }

    /// <summary>
    /// Sends data through UDP to clients.
    /// </summary>
    /// <param name="_clientEndpoint">IP and Port of the client to send to</param>
    /// <param name="_packet">Data to send</param>
    public static void SendUDPData(IPEndPoint _clientEndpoint, Packet _packet)
    {
        try
        {
            if (_clientEndpoint != null)
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndpoint, null, null);
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data to {_clientEndpoint} via UDP {e}");
        }
    }


    /// <summary>
    /// Stops the server and tells all clients to close.
    /// </summary>
    public static void Stop()
    {
        ServerSend.StopServer();
        try
        {
            tcpListener.Stop();
            udpListener.Close();

            tcpListener = null;
            udpListener = null;
        }
        catch
        {
            Debug.Log("Closing Server");
        }
    }
}