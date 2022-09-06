using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class Client : MonoBehaviour
{
    public delegate void OnMessageReceive(object message);
    public event OnMessageReceive onMessageReceive;
    
    private const int MAX_CONNECTION = 10;
    private int _port = 0;
    private int _serverPort = 5805;
    private int _hostID;
    private int _reliableChannel;

    private int _connectionID;
    private bool _isConnected;
    private byte _error;
    public void Connect()
    {
        if (_isConnected) return;
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        _reliableChannel = cc.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(cc, MAX_CONNECTION);
        _hostID = NetworkTransport.AddHost(topology, _port);
        _connectionID = NetworkTransport.Connect(_hostID, "127.0.0.1", _serverPort, 0, out _error);
        if ((NetworkError)_error == NetworkError.Ok)
            _isConnected = true;
        else
            Debug.Log((NetworkError)_error);
    }
    public void Disconnect()
    {
        if (!_isConnected) return;
        NetworkTransport.Disconnect(_hostID, _connectionID, out _error);
        _isConnected = false;
    }
    void Update()
    {
        if (!_isConnected) return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out
        channelId, recBuffer, bufferSize, out dataSize, out _error);
        while (recData != NetworkEventType.Nothing)
        {
            switch (recData)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.ConnectEvent:
                    onMessageReceive?.Invoke($"You have been connected to server.");
                    break;
                case NetworkEventType.DataEvent:
                    string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    if (message == "/Server stopped")
                    {
                        Disconnect();
                        onMessageReceive?.Invoke($"Server is shutdown");
                        break;
                    }
                    onMessageReceive?.Invoke(message);
                    break;
                case NetworkEventType.DisconnectEvent:
                    _isConnected = false;
                    onMessageReceive?.Invoke($"You have been disconnected from server.");
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer,
            bufferSize, out dataSize, out _error);
        }
    }
    public void SendMessage(string message)
    {
        if(!_isConnected) return;
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        var messageSize = message.Length *
                          sizeof(char);
        NetworkTransport.Send(_hostID, _connectionID, _reliableChannel, buffer, message.Length *
        sizeof(char), out _error);
        if ((NetworkError)_error != NetworkError.Ok) Debug.Log((NetworkError)_error);
    }

    private void OnDestroy()
    {
        Disconnect();
        NetworkTransport.Shutdown();
    }
}