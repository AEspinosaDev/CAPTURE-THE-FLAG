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
    //[SerializeField] PlayerManager m_PlayerManager;
    [SerializeField] UIManager m_UIManager;

    [SerializeField] GameObject m_PlayerPrefab;
    [SerializeField] public List<Transform> m_SpawnPoints;
    [HideInInspector] public Player m_LocalPlayer;

    ////SERVER ONLY
    [HideInInspector] public Dictionary<ulong, Player> m_Players = new Dictionary<ulong, Player>();

    [HideInInspector] public int m_TotalTimeInSecondsLeft;

    public readonly int TOTAL_PLAYERS = 6;
    private const float RESPAWN_TIME = 2.5f;
    [SerializeField] public int m_TotalTimeInSeconds = 120;

    private bool takingTimeAway = false;

    [HideInInspector] public bool m_PlayState = false;


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
    private void Start()
    {
        m_TotalTimeInSecondsLeft = m_TotalTimeInSeconds;
        //Time setup on UI
        int min = m_TotalTimeInSecondsLeft / 60;
        int seconds = m_TotalTimeInSecondsLeft % 60;
        string secondsText;
        if (seconds >= 10) secondsText = seconds.ToString(); else secondsText = "0" + seconds.ToString();
        m_UIManager.m_TimeLeft.text = min.ToString() + ":" + secondsText;
    }

    private void Update()
    {
        if (m_NetWorkManager.IsServer)
        {
            if (m_PlayState)
            {
                if (m_TotalTimeInSecondsLeft > 0)
                {
                    if (!takingTimeAway)
                    {
                        StartCoroutine(TakeSecondAway());
                    }
                }
                else
                {
                    m_PlayState = false;
                    foreach (var p in m_Players)
                    {
                        p.Value.m_State.Value = PlayerState.Grounded;
                        p.Value.InitializeEndGameActionsClientRpc();
                    }

                }
            }
        }
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

        var player = Instantiate(m_PlayerPrefab, m_SpawnPoints[m_Players.Count].position, m_SpawnPoints[m_Players.Count].rotation);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        m_Players.Add(clientId, player.GetComponent<Player>());



    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!m_NetWorkManager.IsServer) return;

        if (m_PlayState)
        {
            m_Players.Remove(obj);
            UpdatePlayersCount();
        }

        //Player exitPlayer;
        //m_Players.TryGetValue(obj, out exitPlayer);
        //exitPlayer.GetComponent<NetworkObject>().Despawn();

    }
    public IEnumerator RespawnPlayerInTime(float waitTime, GameObject player)
    {

        yield return new WaitForSeconds(waitTime);

        Vector3 spawnPoint = Vector3.zero;
        bool criteria = true;

        foreach (var point in m_SpawnPoints)
        {
            foreach (var p in m_Players)
            {
                if (p.Value.isActiveAndEnabled)
                    if ((p.Value.transform.position - point.position).magnitude < 4) criteria = false;
            }
            if (criteria == true)
            {
                spawnPoint = point.position;
                break;
            }
            else criteria = true;

        }
        //player.transform.position = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Count)].position;
        player.transform.position = spawnPoint;

        player.SetActive(true);
        player.GetComponent<Player>().RespawnPlayer();


    }
    /// <summary>
    /// Server only.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="player"></param>
    public void RespawnPlayer(GameObject player)
    {
        StartCoroutine(RespawnPlayerInTime(RESPAWN_TIME, player));
    }
    /// <summary>
    /// Server only.
    /// </summary>
    /// <param name="id"></param>
    public void DisconnectPlayer(ulong id)
    {
        m_NetWorkManager.DisconnectClient(id);
    }


    private IEnumerator TakeSecondAway()
    {

        takingTimeAway = true;
        yield return new WaitForSeconds(1f);
        m_TotalTimeInSecondsLeft--;
        foreach (var p in m_Players)
        {
            p.Value.m_TimeLeft.Value = m_TotalTimeInSecondsLeft;
        }
        takingTimeAway = false;

    }

    public void UpdatePlayersCount()
    {
        foreach (var p in m_Players)
        {
            p.Value.m_Foes.Value = m_Players.Count;
        }
    }
   
    public void ResetGame()
    {
        m_LocalPlayer = null;
        ////SERVER ONLY
        m_Players = new Dictionary<ulong, Player>();
        m_TotalTimeInSecondsLeft = m_TotalTimeInSeconds;
        takingTimeAway = false;
        m_PlayState = false;
    }

}
