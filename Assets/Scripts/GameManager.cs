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
    [SerializeField] UIManager m_UIManager;
    [SerializeField] GameObject m_PlayerPrefab;
    [SerializeField] public List<Transform> m_SpawnPoints;
    [HideInInspector] public Player m_LocalPlayer;
    [HideInInspector] private Player m_BestPlayer = null;

    ////SERVER ONLY
    [HideInInspector] public Dictionary<ulong, Player> m_Players = new Dictionary<ulong, Player>();

    [HideInInspector] public int m_TotalTimeInSecondsLeft;
    [HideInInspector] public int m_CountdownSecondsLeft;

    public readonly int TOTAL_PLAYERS = 2;
    private const float RESPAWN_TIME = 2.5f;
    public readonly int START_MATCH_TIME = 10;
    [SerializeField] public int m_TotalMatchTimeInSeconds = 120;
    public int m_PlayersReady = 0;

    private bool takingTimeAway = false;

    [HideInInspector] public bool m_PlayState = false;

    [SerializeField] public GameObject m_LobbySetup;

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
        m_TotalTimeInSecondsLeft = m_TotalMatchTimeInSeconds;
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
        {
            return;
        }

        var player = Instantiate(m_PlayerPrefab, m_SpawnPoints[Random.Range(0, 1)].position, m_SpawnPoints[Random.Range(0, 1)].rotation);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        m_Players.Add(clientId, player.GetComponent<Player>());

    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!m_NetWorkManager.IsServer) return;

        m_Players.Remove(obj);
        UpdatePlayersCount();

        if (!m_PlayState)
        {
            UpdatePlayersReadyNumber(-1);
        }


    }

    public IEnumerator StartMatchInTime()
    {
        bool playerGone = false;
        int timeLeft = START_MATCH_TIME;
        while (timeLeft >= 0)
        {
            yield return new WaitForSeconds(1);
            if (m_Players.Count < TOTAL_PLAYERS) { playerGone = true; break; }
            foreach (var p in m_Players)
            {
                p.Value.UpdateCountDownGUIClientRpc(timeLeft);
            }
            //Actualizar counter de clientes
            timeLeft--;
        }
        if (!playerGone)
        {
            m_LobbySetup.SetActive(false);
            int i = 0;
            foreach (var p in m_Players)
            {
                p.Value.transform.position = m_SpawnPoints[i].position;
                p.Value.EnableWeaponsAndGUIClientRpc();
                i++;
            }
            m_PlayState = true;
        }
        else
        {
            foreach (var p in m_Players)
            {
                p.Value.UpdateCountDownGUIClientRpc(-1);
            }
        }

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
    public void UpdatePlayersReadyNumber(int n)
    {

        m_PlayersReady += n;
        foreach (var p in m_Players)
        {
            p.Value.m_ReadyFoes.Value = m_PlayersReady;
        }
        if (m_PlayersReady == TOTAL_PLAYERS)
        {
            StartCoroutine(StartMatchInTime());

        }
    }

    public void UpdatePlayersCount()
    {
        foreach (var p in m_Players)
        {
            p.Value.m_Foes.Value = m_Players.Count;
        }
    }

    public void RankPlayers()
    {
        List<int[]> pointsById = new List<int[]>();
        foreach (var p in m_Players)
        {
            int[] arr = { (int)p.Key, p.Value.m_Points.Value };
            pointsById.Add(arr);
        }
        pointsById.Sort(delegate (int[] arr1, int[] arr2)
        {
            if (arr1[1] < arr2[1]) return 1;
            if (arr1[1] == arr2[1]) return 0;
            else return -1;
        });
        Player player;
        m_Players.TryGetValue((ulong)pointsById[0][0], out player);
        if (player == m_BestPlayer)
        {
            m_Players.TryGetValue((ulong)pointsById[1][0], out player);
        }
        player?.SetCrownActive(true);
        m_BestPlayer?.SetCrownActive(false);
        m_BestPlayer = player;
    }

    public void ResetGame()
    {
        m_LobbySetup.SetActive(true);
        m_LocalPlayer = null;
        ////SERVER ONLY
        m_Players = new Dictionary<ulong, Player>();
        m_Players = new Dictionary<ulong, Player>(); 
        m_PlayersReady = 0;
        m_BestPlayer = null;
        m_TotalTimeInSecondsLeft = m_TotalMatchTimeInSeconds;
        takingTimeAway = false;
        m_PlayState = false;
    }

}
