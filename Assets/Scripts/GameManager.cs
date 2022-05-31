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

    //Esta EED se comporta de forma distinta para servidor que para cliente. En el servidor se actualiza constantemente.
    //En el cliente solo guarda el numero total de jugadores que habia al principio de la partida
    [HideInInspector] public Dictionary<ulong, Player> m_Players = new Dictionary<ulong, Player>();

    [HideInInspector] public int m_TotalTimeInSecondsLeft;
    [HideInInspector] public int m_CountdownSecondsLeft;

    public readonly int TOTAL_PLAYERS = 4;
    private const float RESPAWN_TIME = 2.5f;
    public readonly int START_MATCH_TIME = 10;
    private const int SAFE_DISTANCE = 4
        ;
    [SerializeField] public int m_TotalMatchTimeInSeconds = 120;
    [HideInInspector] public int m_PlayersReady = 0;

    private bool takingTimeAway = false;

    [HideInInspector] public bool m_PlayState = false;

    [SerializeField] public GameObject m_LobbySetup;



    private void OnEnable()
    {
        m_NetWorkManager.OnClientConnectedCallback += OnClientConnected;
        m_NetWorkManager.OnClientDisconnectCallback += OnClientDisconnected;

    }
    private void OnDisable()
    {

        m_NetWorkManager.OnClientConnectedCallback -= OnClientConnected;
        m_NetWorkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
    private void Start()
    {
        //Setup del tiempo en la GUI
        m_TotalTimeInSecondsLeft = m_TotalMatchTimeInSeconds;
        int min = m_TotalTimeInSecondsLeft / 60;
        int seconds = m_TotalTimeInSecondsLeft % 60;
        string secondsText;
        if (seconds >= 10) secondsText = seconds.ToString(); else secondsText = "0" + seconds.ToString();
        m_UIManager.m_TimeLeft.text = min.ToString() + ":" + secondsText;
    }

    private void Update()
    {
        //Lógica del reloj global de partida
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
                    //Si se acaba el tiempo, el servidor avisa a todos los clientes y les pide que inicien las acciones
                    //pertinentes para acabar la partida
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

    //public void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    //{
    //    //Your logic here
    //    bool approve = true;
    //    if (m_NetWorkManager.ConnectedClients.Count == TOTAL_PLAYERS) approve = false;

    //    callback(false, null, approve, null, null);
    //}

    /// <summary>
    /// Once a client has connected, this method will be called upon
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!m_NetWorkManager.IsServer)
        {
            return;
        }
        if (m_Players.Count == TOTAL_PLAYERS)
        {
            //Si se sobrepasa el máximo de jugadores, se desconecta al cliente direcatmente
            m_NetWorkManager.DisconnectClient(clientId);
            return;
        }

        var player = Instantiate(m_PlayerPrefab, m_SpawnPoints[Random.Range(0, 1)].position, m_SpawnPoints[Random.Range(0, 1)].rotation);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        //El servidor añade al diccionario de jugadores el nuevo jugador
        m_Players.Add(clientId, player.GetComponent<Player>());

    }

    private void OnClientDisconnected(ulong id)
    {
        if (m_NetWorkManager.IsServer)
        {
            if (!m_PlayState)
            {
                //if (m_Players.Count == TOTAL_PLAYERS) return;
                if (m_PlayersReady > 0)
                    //Si se va un jugador en el lobby se actualizan las cifras de los jugadores preparados en los clientes
                    UpdatePlayersReadyNumber(-1);
            }
            //Se retira del diccionario el jugador que se fue y se actualizan las cifras;
            m_Players.Remove(id);
            UpdatePlayersCount();

            //Si hay cero es que todos se han ido y el servidor se debe reseterar
            if (m_Players.Count == 0 && m_NetWorkManager.IsServer && !m_NetWorkManager.IsHost)
            {
                m_NetWorkManager.Shutdown();
                ResetGame();
            }
        }
        else
        {
            //El cliente debe resetear su juego siempre que se desconecte
            ResetGame();
        }


    }
    /// <summary>
    /// Client side. Once the match had just started, clients will populate their player dictionaries.
    /// </summary>
    public void StorePlayersClientSide()
    {
        Player[] players = FindObjectsOfType<Player>();
        ulong i = 0;
        foreach (var p in players)
        {
            m_Players.Add(i, p);
            i++;
        }

    }

    /// <summary>
    /// Server side. Coroutine method in witch match start logic is implemented.
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartMatchInTime()
    {
        bool playerGone = false;
        int timeLeft = START_MATCH_TIME;
        while (timeLeft >= 0)
        {
            yield return new WaitForSeconds(1);
            //En caso de que haya alguna desconexion inesperada, se parará la cuentra atras y se reseteara el tiempo
            if (m_Players.Count < TOTAL_PLAYERS) { playerGone = true; break; }
            //Cada segundo se actualiza en los clientes el contador
            foreach (var p in m_Players)
            {
                p.Value.UpdateCountDownGUIClientRpc(timeLeft);
            }
            timeLeft--;
        }
        if (!playerGone)
        {
            m_LobbySetup.SetActive(false);
            m_UIManager.ActivateInGameHUD();
            int i = 0;
            //Se prepara a cada jugador para que empiece la partida
            foreach (var p in m_Players)
            {
                //Se guardan los jugadores en los clientes
                //p.Value.StorePlayersClientRpc();
                //En caso de que hubiera algun jugador colgado antes de empezar la partida, se le desactiva el estado
                p.Value.GetComponent<GrapplingHook>().DisableHook();
                p.Value.transform.position = m_SpawnPoints[i].position;
                //Se activa el sistema de disparos y la GUI
                p.Value.EnableWeaponsAndGUIClientRpc();
                i++;
            }
            m_PlayState = true;
        }
        else
        {
            foreach (var p in m_Players)
            {
                //En caso de salida inesperada, se resetea
                p.Value.UpdateCountDownGUIClientRpc(-1);
            }
        }

    }
    /// <summary>
    /// Server side. Coroutine method that waits an amount of time before respawning the player and sending the message to the clients.
    /// It also computes the logic behind where to spawn the player, always trying to choose the most secure spawn point.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    private IEnumerator RespawnPlayerInTime(float waitTime, GameObject player)
    {

        yield return new WaitForSeconds(waitTime);

        Vector3 spawnPoint = Vector3.zero;
        bool criteria = true;

        //Algorítmo de busqueda del sitio más seguro para reaparecer
        foreach (var point in m_SpawnPoints)
        {
            foreach (var p in m_Players)
            {
                if (p.Value.isActiveAndEnabled)
                    if ((p.Value.transform.position - point.position).magnitude < SAFE_DISTANCE) criteria = false;
            }
            if (criteria == true)
            {
                spawnPoint = point.position;
                break;
            }
            else criteria = true;

        }
        player.transform.position = spawnPoint;

        player.SetActive(true);
        player.GetComponent<Player>().RespawnPlayer();


    }
    /// <summary>
    /// Server side. Method encapsulating the coroutine call to respawn the player.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="player"></param>
    public void RespawnPlayer(GameObject player)
    {
        StartCoroutine(RespawnPlayerInTime(RESPAWN_TIME, player));
    }
    /// <summary>
    /// Server side. Disconnects the player.
    /// </summary>
    /// <param name="id"></param>
    public void DisconnectPlayer(ulong id)
    {
        m_NetWorkManager.DisconnectClient(id);
    }

    /// <summary>
    /// Server side. Coroutine method to compute the global match time remaining.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Server side. Updates the number of players ready in the lobby and send the information across the network players.
    /// If the maximun number of ready players is reached, the server then starts a countdown to start the match.
    /// </summary>
    /// <param name="n">1 for add a player. -1 for taking out a player</param>
    public void UpdatePlayersReadyNumber(int n)
    {
        m_UIManager.UpdatePlayersReadyNumber(m_PlayersReady);
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
    /// <summary>
    /// Srver side. Updates the number of players in the game and send the information across the network players.
    /// </summary>
    public void UpdatePlayersCount()
    {
        m_UIManager.UpdatePlayerNumber(m_Players.Count);
        foreach (var p in m_Players)
        {
            p.Value.m_Foes.Value = m_Players.Count;
        }
    }
    /// <summary>
    /// Server side. Ranks the players everytime there is a death and computes which player must the leader crown go to.
    /// </summary>
    public void RankPlayers()
    {
        //Se guardan los puntos e ids en una lista y se ordena atendiendo a los puntos para avergiuar quien es el mejor
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
        if (player == m_BestPlayer) return;
        player?.SetCrownActive(true);
        m_BestPlayer?.SetCrownActive(false);
        m_BestPlayer = player;
    }
    /// <summary>
    /// Resets the game state to deafult.
    /// </summary>
    public void ResetGame()
    {

        m_LobbySetup.SetActive(true);
        m_LocalPlayer = null;
        m_Players = new Dictionary<ulong, Player>();
        m_Players = new Dictionary<ulong, Player>();
        m_PlayersReady = 0;
        m_BestPlayer = null;
        m_TotalTimeInSecondsLeft = m_TotalMatchTimeInSeconds;
        takingTimeAway = false;
        m_PlayState = false;

       
    }

}
