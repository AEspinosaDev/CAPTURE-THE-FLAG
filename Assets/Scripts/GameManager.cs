using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Central class that controls the flow of the game
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager m_NetWorkManager;
    [SerializeField] PlayerManager m_PlayerManager;
    [SerializeField] UIManager m_UIManager;

    [SerializeField] GameObject m_PlayerPrefab;

    [SerializeField] public List<Transform> m_SpawnPoints;

    [HideInInspector] public Player m_LocalPlayer;

    ////SERVER ONLY
    [HideInInspector] public Dictionary<ulong, Player> m_Players = new Dictionary<ulong, Player>();


    //UnityEvent OnPlayerCreated;

   
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
        //if (m_NetWorkManager.IsServer)
        //{
        //    m_Players = new Dictionary<ulong, Player>();
        //}
    }

    /// <summary>
    /// Once a client has connected, this method will be called upon
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!m_NetWorkManager.IsServer)
        { //m_PlayerManager.NetworkObject.ChangeOwnership(clientId);
            return;
        }

        var player = Instantiate(m_PlayerPrefab, m_SpawnPoints[m_PlayerManager.m_NumPlayers].position, m_SpawnPoints[m_PlayerManager.m_NumPlayers].rotation);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        m_Players.Add(clientId, player.GetComponent<Player>());


        m_PlayerManager.m_NumPlayers++;
        m_PlayerManager.UpdatePlayerNumberClientRPC(m_PlayerManager.m_NumPlayers);
        m_UIManager.UpdatePlayerNumber(m_PlayerManager.m_NumPlayers);

    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!m_NetWorkManager.IsServer) return;


        m_PlayerManager.m_NumPlayers--;
        m_PlayerManager.UpdatePlayerNumberClientRPC(m_PlayerManager.m_NumPlayers);
        //m_PlayerManager.ShowDisconnectedClientRPC(obj);
        m_UIManager.UpdatePlayerNumber(m_PlayerManager.m_NumPlayers);

    }
    public IEnumerator RespawnPlayerInTime(float waitTime, GameObject player)
    {
       
        yield return new WaitForSeconds(waitTime);

        player.transform.position = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Count)].position;
        player.SetActive(true);
        player.GetComponent<Player>().RespawnPlayer();


    }
    /// <summary>
    /// Server only.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="player"></param>
    public void RespawnPlayer(float waitTime, GameObject player)
    {
        StartCoroutine(RespawnPlayerInTime(waitTime, player));
    }

}
