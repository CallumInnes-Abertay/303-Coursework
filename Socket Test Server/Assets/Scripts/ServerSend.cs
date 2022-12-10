using UnityEngine;

public class ServerSend
{
    /// <summary>
    /// Sends TCP Data to one client.
    /// </summary>
    /// <param name="_toClient">The client to send to.</param>
    /// <param name="_packet">The data to send.</param>
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    /// <summary>
    /// Sends TCP data to all clients.
    /// </summary>
    /// <param name="packet">The data to send.</param>
    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (var i = 1; i <= Server.MaxPlayers; i++)
            Server.clients[i].tcp.SendData(packet);
    }

    /// <summary>
    /// Sends TCP data to all except one client.
    /// </summary>
    /// <param name="_exceptClient">The client to ignore.</param>
    /// <param name="_packet">The data to send.</param>
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (var i = 1; i <= Server.MaxPlayers; i++)
            if (i != _exceptClient)
                Server.clients[i].tcp.SendData(_packet);
    }

    /// <summary>
    /// Sends UDP data to one client.
    /// </summary>
    /// <param name="_toClient">The client to send to.</param>
    /// <param name="_packet"></param>
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    /// <summary>
    /// Sends UDP data to all clients.
    /// </summary>
    /// <param name="_packet">The data to send.</param>
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (var i = 1; i <= Server.MaxPlayers; i++)
            Server.clients[i].udp.SendData(_packet);
    }

    /// <summary>Sends UDP data to all except one client.</summary>
    /// <param name="_exceptClient">The client to ignore.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (var i = 1; i <= Server.MaxPlayers; i++)
            if (i != _exceptClient)
                Server.clients[i].udp.SendData(_packet);
    }

    #region PacketFunctions

    /// <summary>
    /// Sends a welcome message to joining client via TCP.
    /// </summary>
    /// <param name="_toClient">The client to welcome</param>
    /// <param name="_msg">The message to send to them</param>
    public static void Welcome(int _toClient, string _msg)
    {
        using (var packet = new Packet((int)ServerPackets.Welcome))
        {
            packet.Write(_msg);
            packet.Write(_toClient);

            SendTCPData(_toClient, packet);
        }
    }

    /// <summary>
    ///     Sends a welcome message to joining client via TCP.
    /// </summary>
    /// <param name="_toClient">The client to welcome</param>
    public static void StartTimer(int _toClient, int _currentTick)
    {
        using (var packet = new Packet((int)ServerPackets.StartTimer))
        {
            packet.Write(_currentTick);

            SendTCPData(_toClient, packet);
        }
    }


    /// <summary>
    /// Spawns the client with all relevant information such as id, username, position, rotation and colour.
    /// </summary>
    /// <param name="_toClient">Client to spawn </param>
    /// <param name="_player">If it should spawn a local or external player.</param>
    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (var packet = new Packet((int)ServerPackets.SpawnPlayer))
        {
            packet.Write(_player.id);
            packet.Write(_player.username);
            packet.Write(_player.transform.position);
            packet.Write(_player.transform.rotation);
            packet.Write(_player.colour);
            SendTCPData(_toClient, packet);
        }
    }

    /// <summary>
    /// Sends the players position.
    /// </summary>
    /// <param name="_player">The players position to send.</param>
    public static void PlayerPosition(Player _player)
    {
        using (var _packet = new Packet((int)ServerPackets.PlayerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(NetworkManager.instance.Tick);
            _packet.Write(_player.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    /// <summary>
    ///     Sends the players rotation (to be sent to every client but the one who sent it initially).
    /// </summary>
    /// <param name="_player">The players rotation to send from.</param>
    public static void PlayerRotation(Player _player)
    {
        using (var _packet = new Packet((int)ServerPackets.PlayerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    /// <summary>
    ///     Sends the new players colour to all previous clients.
    /// </summary>
    /// <param name="_player">Players id and colour (id so they don't send to themselves)</param>
    public static void PlayerColour(Player _player)
    {
        using (var _packet = new Packet((int)ServerPackets.PlayerColour))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.colour);

            SendTCPDataToAll(_player.id, _packet);
        }
    }

    /// <summary>
    ///     If the player has disconnected then send a request to destroy that player on all clients.
    /// </summary>
    public static void PlayerDisconnected(int _playerid)
    {
        using (var _packet = new Packet((int)ServerPackets.PlayerDisconnected))
        {
            _packet.Write(_playerid);
            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>
    ///     Spawns a collectable on all clients
    /// </summary>
    /// <param name="_collectablePos">The position to spawn the collectable. </param>
    public static void SpawnCollectable(Vector3 _collectablePos)
    {
        using (var _packet = new Packet((int)ServerPackets.SpawnCollectable))
        {
            _packet.Write(_collectablePos);
            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>
    ///     Tells all clients that a player has collected a collectable.
    /// </summary>
    /// <param name="_player">Player who collected the collectable.</param>
    public static void ScoreUpdate(Player _player)
    {
        using (var _packet = new Packet((int)ServerPackets.ScoreUpdate))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.score);
            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>Sends a message to all players the server is stopping and disconnects them.</summary>
    public static void StopServer()
    {
        using (var _packet = new Packet((int)ServerPackets.StopServer))
        {
            _packet.Write("Stopping server");
            SendTCPDataToAll(_packet);
        }
    }

    #endregion PacketFunctions
}