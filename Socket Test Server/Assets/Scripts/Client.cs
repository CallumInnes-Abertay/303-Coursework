using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    //The max size of a buffer/packet.
    private const int DataBufferSize = 4096;

    private readonly int id;
    public Player player;
    public TCP tcp;
    public UDP udp;

    public Client(int _clientId)
    {
        //Give them a id to tie them to their client side counterpart
        id = _clientId;
        //TCP and UDP for sending and listening.
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    /// <summary>Sends the client into the game and informs other clients of the new player.</summary>
    /// <param name="_playerName">The username of the new player.</param>
    public void SendIntoGame(string _playerName)
    {
        //Spawns the player server side.
        player = NetworkManager.instance.InstantiatePlayer(_playerName);
        player.Initialise(id, _playerName);

        // Send all previous players to the new player
        foreach (var client in Server.clients.Values)
            if (client.player != null)
                if (client.id != id)
                    ServerSend.SpawnPlayer(id, client.player);

        // Send the new player to all players (including themselves.)
        foreach (var client in Server.clients.Values)
            if (client.player != null)
                ServerSend.SpawnPlayer(client.id, player);

        Server.players.Add(id, player);
    }

    /// <summary>
    ///     If the client is no longer sending data and thus disconnected.
    /// </summary>
    private void Disconnect()
    {
        Debug.Log($"{player.username}: {tcp.socket.Client.RemoteEndPoint} has disconnected");

        //Destroys the player serverside and removes them from all collections.
        Server.players.Remove(id);
        ThreadManager.ExecuteOnMainThread(() =>
        {
            player.DestroyPlayer();
            player = null;
        });

        //Close the tcp and udp sockets and set their fields to null.
        tcp.Disconnect();
        udp.Disconnect();

        //Tells all clients that another client has disconnected and to destroy them client side.
        ServerSend.PlayerDisconnected(id);

        foreach (var currentPlayer in Server.players.Values)
            Debug.Log($"Id:{currentPlayer.id} Username:{currentPlayer.username} RemovedID:{id}");
        //If server empty then reset the timer.
        if (!Server.players.Any())
        {
            Debug.Log("No players in server, timer resetting.");
            NetworkManager.instance.StopTimer();
        }
    }


    /// <summary>
    ///     Handling TCP server side.
    /// </summary>
    public class TCP
    {
        private readonly int id;
        private byte[] receiveBuffer;
        private Packet receivedData;
        public TcpClient socket;
        private NetworkStream stream;

        public TCP(int _id)
        {
            id = _id;
        }

        /// <summary>
        /// </summary>
        /// <param name="_socket"></param>
        public void Connect(TcpClient _socket)
        {
            socket = _socket;

            //Set up buffer size of the socket
            socket.ReceiveBufferSize = DataBufferSize;
            socket.SendBufferSize = DataBufferSize;

            //returns the network stream.
            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[DataBufferSize];

            //Starts reading for any messages from the client.
            stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

            //Sends a welcome message to each player to confirm they've connected.
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        /// <summary>
        ///     Sends data to the client via TCP.
        /// </summary>
        /// <param name="_packet">The data to be sent.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                //Sends the data and its length through a networkstream.
                if (socket != null) stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {ex}");
            }
        }

        /// <summary>
        ///     Reads all incoming data coming from the client
        /// </summary>
        /// <param name="_result">The status of the async operation..</param>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                //Ends the async network stream read and returns the number of bytes read.
                var byteLength = stream.EndRead(_result);

                //If there was no bytes, then the client is no longer sending data and must have disconnected.
                if (byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                var data = new byte[byteLength];
                //Copy the data into a byte array 
                Array.Copy(receiveBuffer, data, byteLength);

                //So it can be read (and if the packet can be reused then reset the packet by clearing the buffer)
                receivedData.Reset(HandleData(data));

                //Recursion to continue reading till out of data.
                stream.BeginRead(receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error receiving TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        /// <summary>
        ///     Handles all incoming data.
        /// </summary>
        /// <param name="_data">The data to handle in the form of a byte array.</param>
        /// <returns></returns>
        private bool HandleData(byte[] _data)
        {
            var packetLength = 0;

            //prepares the byte array to be read.
            receivedData.SetBytes(_data);

            //If whats left is greater than an int (length) then it contains a packet.
            if (receivedData.UnreadLength() >= 4)
            {
                //Get the length.
                packetLength = receivedData.ReadInt();
                //If the length is 0 or less then packet empty and return early.
                if (packetLength <= 0)
                    //returns true to reset the receivedData and allow reuse.
                    return true;
            }

            //However, while the packet contains data and the length isnt longer than the packet we're currently reading.
            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                var packetBytes = receivedData.ReadBytes(packetLength);

                //Run the correct function to handle the data on the main thread (so to allow Unity specific functions).
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var packet = new Packet(packetBytes))
                    {
                        var packetId = packet.ReadInt();
                        //Return if not in the enum.
                        var isDefined = Enum.IsDefined(typeof(ClientPackets), packetId);

                        if (!isDefined)
                        {
                            Debug.Log("PacketID not defined");
                            return;
                        }


                        //Call the correct method to handle the packet.
                        Server.packetHandlers[packetId](id, packet);
                    }
                });

                //Reset the packet length
                packetLength = 0;
                //If the received data from client still contains more data
                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                        return true;
                }
            }

            if (packetLength <= 1)
                return true;

            //We've read everything.
            return false;
        }

        /// <summary>
        ///     Closes the TCP connection.
        /// </summary>
        public void Disconnect()
        {
            //Closes the socket
            socket.Close();
            //And sets all fields to null for clean up.
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    /// <summary>
    ///     Handling UDP server side.
    /// </summary>
    public class UDP
    {
        private readonly int id;
        public IPEndPoint endPoint;

        public UDP(int _id)
        {
            id = _id;
        }

        /// <summary>
        ///     Initialise method for the connected clients UDP datagrams.
        /// </summary>
        /// <param name="_endPoint">The IP address of the connected client.</param>
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        /// <summary>
        ///     Sends data to a client using UDP.
        /// </summary>
        /// <param name="_packet">The data to send.</param>
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        /// <summary>
        ///     Handles all incoming data from client and runs the handle method.
        /// </summary>
        /// <param name="_packetData">Data to handle.</param>
        public void HandleData(Packet _packetData)
        {
            var packetLength = _packetData.ReadInt();
            var packetBytes = _packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (var packet = new Packet(packetBytes))
                {
                    //Calls the appropriate handle method for the request.
                    var packetId = packet.ReadInt();

                    //Return if packetID not in enum.
                    var isDefined = Enum.IsDefined(typeof(ClientPackets), packetId);
                    if (!isDefined)
                    {
                        Debug.Log("PacketID not defined");
                        return;
                    }

                    //Call the correct method to handle the packet.
                    Server.packetHandlers[packetId](id, packet);
                }
            });
        }

        /// <summary>
        ///     Closes the UDP connection.
        /// </summary>
        public void Disconnect()
        {
            endPoint = null;
        }
    }
}