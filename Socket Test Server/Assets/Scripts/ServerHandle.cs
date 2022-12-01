using System;
using System.Linq;
using UnityEngine;

public class ServerHandle
{
    /// <summary>
    /// The welcome has received
    /// </summary>
    /// <param name="_fromClient">The client it came from</param>
    /// <param name="_packet">The packet they sent (including their id and username)</param>
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        var _clientIdCheck = _packet.ReadInt();
        var _username = _packet.ReadString();

        var ip = Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint;

        Debug.Log($"Username: {_username} has {ip}" +
                  $" connected successfully and is now player {_fromClient}.");

        //If the ids don't match then they've taken on the wrong client (which would break the game and theserver in that case)
        if (_fromClient != _clientIdCheck)
            Debug.Log(
                $"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");

        Server.clients[_fromClient].SendIntoGame(_username);

        //If this is the first player in
        if (Server.players.Count < 2)
        {
            CollectableManager.instance.SpawnCollectable();
        }
        //If it's not then don't spawn a new one, just send the previous ones position to where they are.
        else
        {
            CollectableManager.instance.SpawnCollectable(false);

        }


        var ping = NetworkManager.instance.PingClient(ip.ToString());


        //Ping cant be less than 0 (-1 ping is the error message)
        if (ping >= 0)
        {
            //Adjust the current time to ping 
            var currentTime = NetworkManager.instance.Time + ping;
            ServerSend.StartTimer(_fromClient, currentTime);
        }
        else
        {
            Debug.Log("Error: Could not ping client");
            ServerSend.StartTimer(_fromClient, NetworkManager.instance.Time);
        }

        if (Server.players.Count < 2)
        {
            NetworkManager.instance.StartTimer();
        }
    }


    /// <summary>
    ///     Reads in each players movement from client and updates that player accordingly.
    /// </summary>
    /// <param name="_fromClient">The client who sent the movement data</param>
    /// <param name="_packet">The new position/rotation of the client.</param>
    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        //Amount of different inputs
        var position = _packet.ReadVector3();

        var rotation = _packet.ReadQuaternion();

        try
        {
            //Set rotation serverside so we can debug.
            Server.clients[_fromClient].player.SetPlayerTransform(position, rotation);
            //Tell every other player where this player now is.
            ServerSend.UpdatePlayerTransform(_fromClient, Server.clients[_fromClient].player);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// That a client collected a collectable
    /// </summary>
    /// <param name="_fromClient">ID of client who collected the collectable.</param>
    /// <param name="_packet"></param>
    public static void CollectableCollision(int _fromClient, Packet _packet)
    {
        var id = _packet.ReadInt();
        var username = _packet.ReadString();

        //If the player was the same to pick up the collectable, then add 1 to their score.
        foreach (var player in Server.players.Values.Where(player =>
                     player.id.Equals(id)))
        {
            player.score++;
            
            //Tell every other client they've picked a collectable up.
            ServerSend.ScoreUpdate(player);
        }


        foreach (var player in Server.players)
            Debug.Log($"{player.Value.username} has {player.Value.score} score");


        //Spawns a new collectable for all clients to pick up (starting a cycle)
        CollectableManager.instance.SpawnCollectable();

        Debug.Log(
            $"Username: {username} {Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} has collected a coin.");
    }
}