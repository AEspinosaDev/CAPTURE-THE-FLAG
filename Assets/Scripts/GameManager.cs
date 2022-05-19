using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using System;

/// <summary>
/// Central class that controls the flow of the game
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager m_NetWorkManager;
    [SerializeField] PlayerManager m_PlayerManager;
    [SerializeField] UIManager m_UIManager;

    [SerializeField] GameObject m_PlayerPrefab;

    [SerializeField] private List<Transform> m_SpawnPoints;

    [HideInInspector] public string m_LocalPlayerNickName;

    //UnityEvent OnPlayerCreated;

    private void Start()
    {
        //print("started");
    }
    private void OnEnable()
    {
        m_NetWorkManager.OnServerStarted += OnServerReady;
        m_NetWorkManager.OnClientConnectedCallback += OnClientConnected;
        m_NetWorkManager.OnClientDisconnectCallback += OnClientDisconnected;
       
    }
    private void OnDisable()
    {

        //networkManager.OnServerStarted -= OnServerReady;
        m_NetWorkManager.OnClientConnectedCallback -= OnClientConnected;
        m_NetWorkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    /// <summary>
    /// Once the server has confirmed it is ready and functioning, this method will be called upon
    /// </summary>
    private void OnServerReady()
    {
        print("Server ready");
    }

    /// <summary>
    /// Once a client has connected, this method will be called upon
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!m_NetWorkManager.IsServer) return;

        var player = Instantiate(m_PlayerPrefab,m_SpawnPoints[m_PlayerManager.m_NumPlayers]);

        player.GetComponent<Player>().m_PlayerName.text = clientId.ToString();
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        m_PlayerManager.m_NumPlayers++;
        m_PlayerManager.UpdatePlayerNumberClientRPC(m_PlayerManager.m_NumPlayers);
        m_UIManager.UpdatePlayerNumber(m_PlayerManager.m_NumPlayers);

    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!m_NetWorkManager.IsServer) return;


        m_PlayerManager.m_NumPlayers--;
        m_PlayerManager.UpdatePlayerNumberClientRPC(m_PlayerManager.m_NumPlayers);
        m_UIManager.UpdatePlayerNumber(m_PlayerManager.m_NumPlayers);

    }




}
