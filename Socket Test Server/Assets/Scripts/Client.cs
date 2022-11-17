using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client 
{
    private static readonly int dataBufferSize = 4096;

    private int id;
    public Player player;
    public TCP tcp;
    public UDP udp;

    public Client(int _clientId)
    {
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    /// <summary>Sends the client into the game and informs other clients of the new player.</summary>
    /// <param name="_playerName">The username of the new player.</param>
    public void SendIntoGame(string _playerName)
    {
        player = NetworkManager.instance.InstantiatePlayer(_playerName);
        player.Initialise(id, _playerName);

        // Send all players to the new player
        foreach (var _client in Server.clients.Values)
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                    
                }
            }
        
        // Send the new player to all players (including themselves.)
        foreach (var _client in Server.clients.Values)
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }

        Server.players.Add(id,player);
    }

    /// <summary>
    /// If the client is no longer sending data and thus disconnected. 
    /// </summary>
    private void Disconnect()
    {
        Debug.Log($"{player.username}: {tcp.socket.Client.RemoteEndPoint} has disconnected");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            player.DestroyPlayer();
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();
        Server.players.Remove(id);
        ServerSend.PlayerDisconnected(id);

        foreach (var currentPlayer in Server.players.Values)
        {
            Debug.Log($"Id:{currentPlayer.id} Username:{currentPlayer.username} RemovedID:{id}");
        }
    }


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

        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            //Sends a welcome message to each player just for visual feedback they've actually connected.
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null) stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                var _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                var _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error receiving TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        private bool HandleData(byte[] _data)
        {
            var _packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                    return true;
            }

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                var _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var _packet = new Packet(_packetBytes))
                    {
                        var _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                        return true;
                }
            }

            if (_packetLength <= 1)
                return true;

            return false;
        }

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        private readonly int id;
        public IPEndPoint endPoint;

        public UDP(int _id)
        {
            id = _id;
        }

        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        public void HandleData(Packet _packetData)
        {
            var _packetLength = _packetData.ReadInt();
            var _packetBytes = _packetData.ReadBytes(_packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (var _packet = new Packet(_packetBytes))
                {
                    var _packetId = _packet.ReadInt();
                    Server.packetHandlers[_packetId](id, _packet);
                }
            });
        }

        public void Disconnect()
        {
            endPoint = null;
        }
    }
}