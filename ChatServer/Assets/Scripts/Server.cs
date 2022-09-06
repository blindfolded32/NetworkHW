using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public Button StartServerButton;
    public Button StopServerButton;
    
    private const int MAX_CONNECTION = 10;
    private int port = 5805;
    private int hostID;
    private int reliableChannel;
    private int unreliableChannel;
    private bool isStarted = false;
    private byte error;
    List<UserIdentifier> _connectionIDs = new List<UserIdentifier>();


    private void Start()
    {
        StartServerButton.onClick.AddListener(StartServer);
        StopServerButton.onClick.AddListener(ShutDownServer);
    }

    public void StartServer()
    {
        NetworkTransport.Init();//инициаализа€
        ConnectionConfig cc = new ConnectionConfig();
      //cc.ConnectTimeout = 500; //
      //Timeout in ms which library will wait before it will send another connection request.
      //cc.MaxConnectionAttempt = 2;
      //Defines the maximum number of times Unity Multiplayer will attempt
      //to send a connection request without receiving a response before
      //it reports that it cannot establish a connection. Default value = 10.
        reliableChannel = cc.AddChannel(QosType.Reliable);//гарантироованнна€ доставка 
        HostTopology topology = new HostTopology(cc, MAX_CONNECTION);
        
        hostID = NetworkTransport.AddHost(topology, port);
        isStarted = true;
        Debug.Log($"Server started on {NetworkTransport.GetHostPort(hostID)}");
    }
    public void ShutDownServer()
    {
        if (!isStarted) return;
        NetworkTransport.RemoveHost(hostID);
        NetworkTransport.Shutdown();
        isStarted = false;
    }
    void Update()
    {
        if (!isStarted) return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out
        channelId, recBuffer, bufferSize, out dataSize, out error);

        var currentUser = _connectionIDs.Find(x => x.ConnectionID == connectionId);
        while (recData != NetworkEventType.Nothing)
        {
            switch (recData)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.ConnectEvent:
                    if (!_connectionIDs.Exists(x => x.ConnectionID == connectionId))
                    {
                        _connectionIDs.Add(new UserIdentifier(connectionId));
                    }
                    else
                    {
                        SendMessage($"You are already Logged", currentUser.ConnectionID);
                    }
                    // Debug.Log($"Player {connectionId} has connected.");
                    break;
                case NetworkEventType.DataEvent:
                    string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    if (IsCommandMessage(currentUser.ConnectionID,message)) break;
                    if (currentUser.UserID == "1")
                    {
                        currentUser.UserID = message;
                    }
                    else
                    {
                        SendMessageToAll($"{currentUser.UserID} : {message}");
                    }
                    break;
                case NetworkEventType.DisconnectEvent:
                    SendMessageToAll($"{currentUser.UserID} has disconnected.");
                    _connectionIDs.Remove(currentUser);
                    
                   //Debug.Log($"Player {connectionId} has disconnected.");
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer,
            bufferSize, out dataSize, out error);
        }
    }

    private bool IsCommandMessage(int connectionID, string message)
    {
        if (message.StartsWith("/"))
        {
            var command =message.Remove(0,1);
            if (command.Contains("name"))
            {
                 var newName = command.Remove(0, 4).Trim(' ');
                 var currentUser = _connectionIDs.Find(x => x.ConnectionID == connectionID);
                     currentUser.UserID = newName;
                 SendMessage($"You changed name to {newName}",currentUser.ConnectionID);
                 return true;
            }
            if (command.Contains("exit"))
            {
                var currentUser = _connectionIDs.Find(x => x.ConnectionID == connectionID);
                SendMessageToAll($"{currentUser.UserID} has disconnected.");
                _connectionIDs.Remove(currentUser);
                return true;
            }

            return true;

        }

        return false;
    }
    public void SendMessageToAll(string message)
    {
        for (int i = 0; i < _connectionIDs.Count; i++)
        {
            SendMessage(message, _connectionIDs[i].ConnectionID);
        }
    }
    public void SendMessage(string message, int connectionID)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, reliableChannel, buffer, message.Length *
        sizeof(char), out error);
        if ((NetworkError)error != NetworkError.Ok) Debug.Log((NetworkError)error);
    }
    private void OnDestroy()
    {
        SendMessageToAll("/Server stopped");
        ShutDownServer();
        StartServerButton.onClick.RemoveAllListeners();
        StopServerButton.onClick.RemoveAllListeners();
    }
}

public class UserIdentifier
{
    public int ConnectionID;
    public string UserID;

    public UserIdentifier(int connectionID, string userID = "1")
    {
        ConnectionID = connectionID;
        UserID = userID;
    }
}
