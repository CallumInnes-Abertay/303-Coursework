using UnityEngine;

public class ClientSend : MonoBehaviour
{
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The UDP packet to send to the sever.</param>
    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);
    }

    #region PacketFunctions

    /// <summary>
    ///     Lets the server know the welcome was received via TCP.
    /// </summary>
    public static void WelcomeReceived()
    {
        using (var _packet = new Packet((int)ClientPackets.WelcomeReceived))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }

    /// <summary>
    ///     Sends player input and rotation to the server via UDP.
    /// </summary>
    /// <param name="_inputs">The inputs to send.</param>
    public static void PlayerMovement(bool[] _inputs)
    {
        using (var _packet = new Packet((int)ClientPackets.PlayerMovement))
        {
            _packet.Write(_inputs.Length);
            foreach (var _input in _inputs) _packet.Write(_input);
            //Gets the the local players rotation and sends it.
            _packet.Write(GameManager.players[Client.instance.myId].transform.rotation);
            SendUDPData(_packet);
        }
    }


    /// <summary>
    ///     Sends the player id and username of who collected the id via TCP
    /// </summary>
    public static void CollectableCollision()
    {
        using (var _packet = new Packet((int)ClientPackets.CollectableCollision))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }

    #endregion
}