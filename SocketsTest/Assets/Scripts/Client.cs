using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security;
using UnityEditor;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client instance;
    private const int Port = 50000;
    private static readonly int dataBufferSize = 4096;
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandler;
    private string ip;

    private bool isConnected;
    [NonSerialized] public int myId = 0;
    public TCP tcp;
    public UDP udp;

    //When the player closes the application it'll tell the server they've disconnected.
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    //Singleton pattern.
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    public void ConnectToServer()
    {
        //Gets the ip address from player input
        ip = UIManager.instance.ipAddressField.text;

        //Creates the TCP and UDP objects and all packet functions
        InitialiseClientData();

        //Connects to the server via TCP.
        tcp.Connect();
        StartCoroutine(WaitUntilConnected());
    }

    /// <summary>
    ///     Keeps the connection message up till client connects.
    /// </summary>
    /// <returns>The same object as it waits. </returns>
    private IEnumerator WaitUntilConnected()
    {
        //Busy wait until statement true
        yield return new WaitUntil(() => isConnected);
        UIManager.instance.MenuToggle();
    }

    /// <summary>
    /// Sets up the client for connection to the server.
    /// </summary>
    private void InitialiseClientData()
    {
        tcp = new TCP();
        udp = new UDP();

        //Sets up packet handler dictionary with all methods to handle client side,
        //and their corresponding enum.
        packetHandler = new Dictionary<int, PacketHandler>
        {
            { (int)ServerPackets.Welcome, ClientHandle.Welcome },
            { (int)ServerPackets.StartTimer, ClientHandle.StartTimer },
            { (int)ServerPackets.SpawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.PlayerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.PlayerRotation, ClientHandle.PlayerRotation },
            { (int)ServerPackets.PlayerColour, ClientHandle.PlayerColour },
            { (int)ServerPackets.PlayerDisconnected, ClientHandle.PlayerDisconnected },
            { (int)ServerPackets.SpawnCollectable, ClientHandle.SpawnCollectable },
            { (int)ServerPackets.ScoreUpdate, ClientHandle.UpdateScore },
            { (int)ServerPackets.StopServer, ClientHandle.StopServer }
        };
        Debug.Log("Initialised packets.");
    }


    /// <summary>
    /// For disconnecting client from server.
    /// </summary>
    private void Disconnect()
    {
        if (isConnected)
        {
            tcp.socket.Close();
            udp.socket.Close();

            isConnected = false;

            Debug.Log("Disconnected from server.");
        }
    }

    /// <summary>
    ///     Handling TCP connections to server.
    /// </summary>
    public class TCP
    {
        private int attempt;
        private byte[] receiveBuffer;
        private Packet receivedData;
        public TcpClient socket;

        private NetworkStream stream;

        /// <summary>
        /// Start the process of connecting to the server
        /// </summary>
        public void Connect()
        {
            socket = new TcpClient
            {
                //Sets both the receive and send buffers to size for consistency sake
                //(this is done on both server/client side to ensure it's read correctly)
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize,
                ReceiveTimeout = 3000,
                SendTimeout = 1000
            };

            Debug.Log($"Starting connections with {instance.ip}");

            receiveBuffer = new byte[dataBufferSize];

            try
            {
                //Starts an async request to connect to the server.
                socket.BeginConnect(instance.ip, Port, ConnectCallback, socket);
            }
            catch (SocketException e)
            {
                Debug.Log($"Error with the network\n {e}");
            }
            catch (SecurityException e)
            {
                Debug.Log($"Don't have permission to do this\n {e}");
            }
            catch (ObjectDisposedException e)
            {
                Debug.Log($"Socket is closed, is the server actually up?{e}");
            }
        }

        /// <summary>
        ///     Async call back for connecting to server
        /// </summary>
        /// <param name="_result">The result of the async operation</param>
        private void ConnectCallback(IAsyncResult _result)
        {
            try
            {
                var success = _result.AsyncWaitHandle.WaitOne(5000, true);
                Debug.Log("Connecting");

                //Has successfully connected, so thus stop connecting
                if (socket.Connected)
                {
                    socket.EndConnect(_result);
                    instance.isConnected = true;
                    Debug.Log($"Connected successfully to {instance.ip} via TCP.");
                }
                //Hasn't connected
                else
                {
                    Debug.Log(
                        $"Failed to connect to server, is it up? Or do you have the right IP? \nAttempt:{attempt}");
                    if (attempt < 3)
                    {
                        attempt++;
                        socket.Close();
                        Connect();
                    }
                    else
                    {
                        Debug.Log("Failure to connect to server, closing socket.");

                        socket.Close();
                    }

                    return;
                }

                //Gets the network stream for sending and receiving data.
                stream = socket.GetStream();

                //Initialises packet for incoming data
                receivedData = new Packet();

                //Starts a reading down stream and putting anything into a buffer.
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        /// <summary>
        /// Sending data to server
        /// </summary>
        /// <param name="packet">The data to send</param>
        public void SendData(Packet packet)
        {
            //Starts writing the packet to the server
            try
            {
                if (socket != null) stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }
            catch (Exception e)
            {
                Debug.Log($"Error sending data to server via TCP: {e}");
            }
        }

        /// <summary>
        /// Receives incoming TCP data.
        /// </summary>
        /// <param name="_result">The result of the async operation</param>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                //Waits for async read to complete
                //Then returns the number of bytes read from stream
                var byteLength = stream.EndRead(_result);

                //In the case it's no longer receiving anything, then disconnect 
                if (byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                var newData = new byte[byteLength];
                Array.Copy(receiveBuffer, newData, byteLength);

                //Start handling that data.
                receivedData.Reset(HandleData(newData));
                //Recursion to make sure all data is read.
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                //No longer able to read thus the client can no longer continue.
                Debug.Log("Received call back failed, disconnecting");
                Disconnect();
            }
        }

        /// <summary>
        /// Handles all incoming TCP data.
        /// </summary>
        /// <param name="_data">The data to handle.</param>
        /// <returns>If the packet is to be reset, for reuse.</returns>
        private bool HandleData(byte[] _data)
        {
            var _packetLength = 0;

            //Sets the packet up to be read.
            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();

                //If contains no data, then returns true to reset packet for reuse.
                if (_packetLength <= 0) return true;
            }

            //Loops through all data 
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                var _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var _packet = new Packet(_packetBytes))
                    {
                        //Gets the id of the packet
                        //This would be the id of the function the server sent from,
                        //so we can get the appropriate handle response.
                        var packetId = _packet.ReadInt();
                        var isDefined = Enum.IsDefined(typeof(ServerPackets), packetId);

                        if (!isDefined)
                        {
                            Debug.Log("PacketID not defined");
                            return;
                        }


                        //Call the appropriate function to handle this specific packet.
                        packetHandler[packetId](_packet);
                        Debug.Log($"Handling TCP request ID:{packetId}");
                    }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() < 4)
                    continue;
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                    return true;
            }

            //Returns true or false if packetLength is <= to 1.
            //If true then 
            return _packetLength <= 1;
        }

        /// <summary>
        ///     Disconnects from the server
        /// </summary>
        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    /// <summary>
    ///     Handling UDP connection to server.
    /// </summary>
    public class UDP
    {
        private IPEndPoint endPoint;
        public UdpClient socket;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), Port);
        }

        /// <summary>
        ///     Connects to server via UDP.
        /// </summary>
        /// <param name="localPort">The port number to bind the UDP socket to. (Not the servers port)</param>
        public void Connect(int localPort)
        {
            socket = new UdpClient(localPort);

            //Initiate connection with server.
            socket.Connect(endPoint);
            //Start listening from the server
            socket.BeginReceive(ReceiveCallback, null);

            using (var _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        /// <summary>
        ///     Sends data via UDP.
        /// </summary>
        /// <param name="_packet">Data to send</param>
        public void SendData(Packet _packet)
        {
            try
            {
                //Starts the packet with ID so the server knows who sent it.
                _packet.InsertInt(instance.myId);
                //Starts sending this data.
                socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
            }
            catch (Exception e)
            {
                Debug.Log($"Error sending data to server via UDP: {e}");
            }
        }

        /// <summary>
        /// Receives incoming UDP data.
        /// </summary>
        /// <param name="result">The result of the async operation.</param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                //If the data is less than the size of a float.
                if (data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                //Then start handling the UDP data.
                HandleData(data);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Receive call back failed for UDP. Disconnecting: {e}");
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The received data.</param>
        private void HandleData(byte[] _data)
        {
            using (var packet = new Packet(_data))
            {
                var packetLength = packet.ReadInt();
                _data = packet.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (var packet = new Packet(_data))
                {
                    var packetId = packet.ReadInt();

                    //If packetId is not in the enum then return.
                    var isDefined = Enum.IsDefined(typeof(ServerPackets), packetId);
                    if (!isDefined)
                    {
                        Debug.Log("PacketID not defined");
                        return;
                    }

                    // Call whatever appropriate function to handle this packet.
                    packetHandler[packetId](packet);
                }
            });
        }

        /// <summary>
        /// Disconnects the UDP connection.
        /// </summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }
}