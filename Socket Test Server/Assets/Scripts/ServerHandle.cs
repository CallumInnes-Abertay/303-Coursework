using System.Linq;
using UnityEngine;

public class ServerHandle
{
    /// <summary>
    ///     The welcome has received
    /// </summary>
    /// <param name="_fromClient">The client it came from</param>
    /// <param name="_packet">The packet they sent (including their id and username)</param>
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        var _clientIdCheck = _packet.ReadInt();
        var _username = _packet.ReadString();

        Debug.Log($"Username: {_username} has {Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint}" +
                  $" connected successfully and is now player {_fromClient}.");

        //If the ids don't match then they've taken on the wrong client (which would break the game and theserver in that case)
        if (_fromClient != _clientIdCheck)
            Debug.Log(
                $"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");

        Server.clients[_fromClient].SendIntoGame(_username);

        //If this is the first player in
        if (Server.players.Count < 2)
            CollectableManager.instance.SpawnCollectable();
        else
            CollectableManager.instance.SpawnCollectable(false);
    }

    /// <summary>
    ///     Reads in each players movement from client.
    /// </summary>
    /// <param name="_fromClient">The client who sent the movement data</param>
    /// <param name="_packet">The keys they pressed</param>
    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        //Amount of different inputs
         var position = _packet.ReadVector3();
        
         var rotation = _packet.ReadQuaternion();

        Server.clients[_fromClient].player.SetPlayerTransform(position, rotation);

        ServerSend.UpdatePlayerTransform(_fromClient, Server.clients[_fromClient].player);

    }

    /// <summary>
    /// </summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void CollectableCollision(int _fromClient, Packet _packet)
    {
        var id = _packet.ReadInt();
        var username = _packet.ReadString();
        var score = _packet.ReadInt();


        foreach (var player in Server.players.Values.Where(player => player.id.Equals(id))) player.score += score;

        foreach (var player in Server.players) Debug.Log($"{player.Value.username} has {player.Value.score} score");

        CollectableManager.instance.SpawnCollectable();

        Debug.Log(
            $"Username: {username} {Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} has collected a coin.");
    }
}