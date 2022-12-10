using System;
using System.Linq;
using UnityEngine;

public class ServerHandle
{
    /// <summary>
    /// The welcome has been received from the client.
    /// </summary>
    /// <param name="_fromClient">The client it came from</param>
    /// <param name="_packet">The packet they sent (including their id and username)</param>
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        var clientIdCheck = _packet.ReadInt();
        var username = _packet.ReadString();

        var ip = Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint;

        Debug.Log($"Username: {username} has {ip}" +
                  $" connected successfully and is now player {_fromClient}.");

        //If the ids don't match then they've taken on the wrong client (which would break the game and the server in that case)
        if (_fromClient != clientIdCheck)
            Debug.Log(
                $"Player \"{username}\" (ID: {_fromClient}) has assumed the wrong client ID ({clientIdCheck})!");

        //Spawn the player into the game.
        Server.clients[_fromClient].SendIntoGame(username);

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

        var ping = NetworkManager.PingClient(ip.ToString());


        //Ping cant be less than 0 (-1 ping is the error message)
        if (ping >= 0)
        {
            //Adjust the current time to ping 
            var currentTime = NetworkManager.instance.Tick + ping;
            ServerSend.StartTimer(_fromClient, currentTime);
        }
        else
        {
            Debug.Log("Error: Could not ping client");
            ServerSend.StartTimer(_fromClient, NetworkManager.instance.Tick);
        }

        //Start the timer only on the first client joining.
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
        //Reads all the keys pressed by the client
        bool[] inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = _packet.ReadBool();
        }
        //And gets their rotation
        Quaternion rotation = _packet.ReadQuaternion();

        //Update the players 
        Server.clients[_fromClient].player.SetInput(inputs, rotation);
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

        //Debug the players score.
        foreach (var player in Server.players)
            Debug.Log($"{player.Value.username} has {player.Value.score} score");


        //Spawns a new collectable for all clients to pick up (starting a cycle)
        CollectableManager.instance.SpawnCollectable();

        Debug.Log(
            $"Username: {username} {Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} has collected a coin.");
    }
}