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
    ///     Starts the server
    /// </summary>
    /// <param name="_maxPlayers">The max amount of connections to this server.</param>
    /// <param name="_port">The port they connect to.</param>
    public static void Start(int _maxPlayers, int _port)
    {
        MaxPlayers = _maxPlayers;
        Port = _port;

        Debug.Log("Starting server...");
        InitialiseServerData();

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

        udpListener = new UdpClient(_port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on port {Port}.");
    }

    private static void InitialiseServerData()
    {
        for (var i = 1; i <= MaxPlayers; i++) clients.Add(i, new Client(i));

        //Sets up packet handler dictionary with all methods to handle server side,
        //and their corresponding enum (which is the same client side).
        packetHandlers = new Dictionary<int, PacketHandler>
        {
            { (int)ClientPackets.WelcomeReceived, ServerHandle.WelcomeReceived },
            { (int)ClientPackets.PlayerMovement, ServerHandle.PlayerMovement },
            { (int)ClientPackets.CollectableCollision, ServerHandle.CollectableCollision }
        };
        Debug.Log("Initialised packets.");
    }

    /// <summary>
    ///     Handles any new TCP connections.
    /// </summary>
    /// <param name="result">References the asynchronous creation of the TcpClient.</param>
    private static void TCPConnectCallback(IAsyncResult result)
    {
        var client = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");

        //Adds the incoming connection to one of the potential sockets 
        for (var i = 1; i <= MaxPlayers; i++)
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(client);
                return;
            }

        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    private static void UDPReceiveCallback(IAsyncResult result)
    {
        try
        {
            var _clientEndPoint = new IPEndPoint(IPAddress.Any, 50000);
            var _data = udpListener.EndReceive(result, ref _clientEndPoint);
            //Begins listening
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4)
                return;

            using (var _packet = new Packet(_data))
            {
                var _clientId = _packet.ReadInt();

                if (_clientId == 0)
                    return;

                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                if (clients[_clientId].udp.endPoint.Equals(_clientEndPoint))
                    clients[_clientId].udp.HandleData(_packet);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error receiving UDP data: {e}");
        }
    }

    public static void SendUDPData(IPEndPoint clientEndpoint, Packet packet)
    {
        try
        {
            if (clientEndpoint != null)
                udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndpoint
                    , null, null);
        }
        catch (Exception e)
        {
            Debug.Log($"Error receiving data to {clientEndpoint} via UDP {e}");
        }
    }


    /// <summary>
    ///     Stops the server and tells all clients to close.
    /// </summary>
    public static void Stop()
    {
        ServerSend.StopServer();
        try
        {
            tcpListener.Stop();
            udpListener.Close();
        }
        catch
        {
            Debug.Log("Closing Server");
        }
    }
}