using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIController : MonoBehaviour
{
    [SerializeField]
    private Button buttonConnectClient;
    [SerializeField]
    private Button buttonDisconnectClient;
    [SerializeField]
    private Button buttonSendMessage;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private ChatText textField;
    [SerializeField]
    private Client client;
    private void Start()
    {
        buttonConnectClient.onClick.AddListener( Connect);
        buttonDisconnectClient.onClick.AddListener(Disconnect);
        buttonSendMessage.onClick.AddListener(SendMessage);
        client.onMessageReceive += ReceiveMessage;
    }
   
    private void Connect()
    {
        client.Connect();
    }
    private void Disconnect()
    {
        client.Disconnect();
    }
    private void SendMessage()
    {
        client.SendMessage(inputField.text);
        inputField.text = "";
    }
    public void ReceiveMessage(object message)
    {
        textField.ReceiveMessage(message);
    }
    private void OnDestroy()
    {       
        buttonConnectClient.onClick.RemoveAllListeners();
        buttonDisconnectClient.onClick.RemoveAllListeners();
        buttonSendMessage.onClick.RemoveAllListeners();
        client.onMessageReceive -= ReceiveMessage;
    }
}