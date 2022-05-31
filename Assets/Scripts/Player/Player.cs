using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;
using System;

/// <summary>
/// Class where the main logic of the player is computed, also acting as a virtual representation of the client
/// and a bridge between the server and the client.
/// </summary>
public class Player : NetworkBehaviour
{
    #region Variables

    [SerializeField] public TextMesh m_PlayerNameRenderer;
    [SerializeField] public SpriteRenderer m_Crown;

    private UIManager m_UIManager;
    private GameManager m_GameManager;

    private const int MAX_HP = 6;
    private const int POINTS_PER_KILL = 100;
    private const int POINTS_PER_DEATH = 50;

    #region Network Variables
    [SerializeField] public NetworkVariable<PlayerState> m_State;
    [HideInInspector] public NetworkVariable<FixedString64Bytes> m_PlayerName;
    [HideInInspector] public NetworkVariable<int> m_Life;
    [HideInInspector] public NetworkVariable<int> m_Kills;
    [HideInInspector] public NetworkVariable<int> m_Deaths;
    [HideInInspector] public NetworkVariable<int> m_Points;
    [HideInInspector] public NetworkVariable<int> m_TimeLeft;
    [HideInInspector] public NetworkVariable<int> m_Foes;
    [HideInInspector] public NetworkVariable<int> m_ReadyFoes;
    [HideInInspector] public NetworkVariable<Color> m_Color;
    #endregion


    #endregion


    #region Unity Event Functions

    private void Awake()
    {
        m_PlayerNameRenderer = GetComponentInChildren<TextMesh>();
        m_State = new NetworkVariable<PlayerState>();
        m_PlayerName = new NetworkVariable<FixedString64Bytes>();
        m_Life = new NetworkVariable<int>();
        m_Kills = new NetworkVariable<int>();
        m_Deaths = new NetworkVariable<int>();
        m_Points = new NetworkVariable<int>();

        m_TimeLeft = new NetworkVariable<int>();
        m_Foes = new NetworkVariable<int>();
        m_ReadyFoes = new NetworkVariable<int>();
        m_Color = new NetworkVariable<Color>();

    }
    private void Start()
    {
        m_UIManager = FindObjectOfType<UIManager>();
        m_GameManager = FindObjectOfType<GameManager>();

        if (IsLocalPlayer)
        {
            m_Life.OnValueChanged += UpdateGUILife;
            m_Kills.OnValueChanged += UpdateGUIKills;
            m_Deaths.OnValueChanged += UpdateGUIDeaths;
            m_Points.OnValueChanged += UpdateGUIPoints;

            m_TimeLeft.OnValueChanged += UpdateGUITime;
            m_Foes.OnValueChanged += UpdateGUIPlayers;
            m_ReadyFoes.OnValueChanged += UpdateGUIReadyPlayers;

            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();

            UpdatePlayerCountServerRpc();
            SetPlayerReadyServerRpc(0);

        }
        if (IsServer)
        {
            m_Life.Value = MAX_HP;
            m_Color.Value = new Color(1, 1, 1, 1);

        }

        GetComponent<SpriteRenderer>().color = m_Color.Value;
        m_Color.OnValueChanged += ChangePlayerColor;

    }



    private void OnEnable()
    {

        m_PlayerName.OnValueChanged += OnPlayerNameChanged;
        m_Color.OnValueChanged += ChangePlayerColor;

        if (IsLocalPlayer)
        {
            m_Life.OnValueChanged += UpdateGUILife;
            m_Kills.OnValueChanged += UpdateGUIKills;
            m_Deaths.OnValueChanged += UpdateGUIDeaths;


            m_Points.OnValueChanged += UpdateGUIPoints;

            m_TimeLeft.OnValueChanged += UpdateGUITime;
            m_Foes.OnValueChanged += UpdateGUIPlayers;
            m_ReadyFoes.OnValueChanged += UpdateGUIReadyPlayers;
        }

    }


    private void OnDisable()
    {
        m_PlayerName.OnValueChanged -= OnPlayerNameChanged;
        m_Color.OnValueChanged -= ChangePlayerColor;

        if (IsLocalPlayer)
        {
            m_Life.OnValueChanged -= UpdateGUILife;
            m_Kills.OnValueChanged -= UpdateGUIKills;
            m_Deaths.OnValueChanged -= UpdateGUIDeaths;

            m_Points.OnValueChanged -= UpdateGUIPoints;

            m_TimeLeft.OnValueChanged -= UpdateGUITime;
            m_Foes.OnValueChanged -= UpdateGUIPlayers;
            m_ReadyFoes.OnValueChanged -= UpdateGUIReadyPlayers;
        }

    }
    private void Update()
    {
        m_PlayerNameRenderer.text = m_PlayerName.Value.ToString();
    }

    #endregion

    #region Config Methods
    /// <summary>
    /// Main configuration of the local player. It stores in the local game manager a reference to self and tells the server to update its
    /// name across the network
    /// </summary>
    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        UpdatePlayerNameServerRpc(m_UIManager.m_InputFieldName.text, NetworkObjectId);
        m_GameManager.m_LocalPlayer = this;
    }
    /// <summary>
    /// Main configuration of the camera.
    /// </summary>
    void ConfigureCamera()
    {
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }
    /// <summary>
    /// Main configuration of the player controls
    /// </summary>
    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }
    /// <summary>
    /// Server side. Sets the state of the leader crown and propagates it across the network
    /// </summary>
    /// <param name="condition"></param>
    public void SetCrownActive(bool condition)
    {
        m_Crown.enabled = condition;
        ManageCrownRenderClientRpc(condition);
    }


    /// <summary>
    /// Server side. Called when a bullet hits the player on the server.
    /// </summary>
    /// <param name="enemyClientId">The id of the attacker. It will be usefull in case of killing the player</param>
    public void ComputeDamage(ulong enemyClientId)
    {
        m_Life.Value--;
        if (m_Life.Value == 0)
        {
            //Si la vida llega a 0 se mata al jugador
            KillPlayer(enemyClientId);
        }
    }
    /// <summary>
    /// Server side. Updates the number of kills and points for the killer, and of course, kills the player by disabling it.
    /// </summary>
    /// <param name="killerName">The id of the attacker</param>
    public void KillPlayer(ulong enemyClientId)
    {
        Player killer;
        //Busca al asesino en la lista de jugadores del manager y le suma un tanto
        m_GameManager.m_Players.TryGetValue(enemyClientId, out killer);
        killer.m_Kills.Value++;
        killer.m_Points.Value += POINTS_PER_KILL;
        //Propaga la muerte a traves de la red
        KillPlayerClientRpc(killer.m_PlayerName.Value.ToString());
        //Le pide al manager que compute el ranking para actualizar al poseedor de la corona
        m_GameManager.RankPlayers();
        //Le pide al manager que inicie la corutina de respawneo del jugador
        m_GameManager.RespawnPlayer(gameObject);
        //Desactiva completamente a este jugador
        gameObject.SetActive(false);

    }
    /// <summary>
    /// Server side. When the respawning time is over, this function will be called upon, telling the clients to respawn the player
    /// and updating the death count
    /// </summary>
    public void RespawnPlayer()
    {
        //Propaga el respawn a traves de la red
        RespawnPlayerClientRpc();
        //Desactiva el hook en caso de que hubiese muerto colgando
        GetComponent<GrapplingHook>().DisableHook();
        //Actualiza las muertes, los puntos y resetea la vida.
        m_Deaths.Value++;
        if (m_Points.Value - POINTS_PER_DEATH < 0) m_Points.Value = 0; else m_Points.Value -= POINTS_PER_DEATH;
        m_Life.Value = MAX_HP;
    }
    #endregion

    #region RPC
    /// <summary>
    /// Tells the server to update the player state network variable
    /// </summary>
    /// <param name="state"></param>

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        m_State.Value = state;
    }
    /// <summary>
    /// Called on spawn. Tells the server to update the name of the player given by the user, propagating it across the network.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="clientId"></param>
    [ServerRpc]
    public void UpdatePlayerNameServerRpc(string name, ulong clientId)
    {
        m_PlayerName.Value = name;

    }
    /// <summary>
    /// Tells the server to update the number of players in the game
    /// </summary>
    [ServerRpc]
    public void UpdatePlayerCountServerRpc()
    {
        m_GameManager.UpdatePlayersCount();
    }
    /// <summary>
    /// Tells the server to disconnect the player
    /// </summary>
    /// <param name="id"></param>
    [ServerRpc]
    public void DisconnectPlayerServerRpc(ulong id)
    {
        m_GameManager.DisconnectPlayer(id);
    }
    /// <summary>
    /// Tells the server to update the number of players ready
    /// </summary>
    /// <param name="n"></param>
    [ServerRpc]
    public void SetPlayerReadyServerRpc(int n)
    {
        m_GameManager.UpdatePlayersReadyNumber(n);
    }
    /// <summary>
    /// Tells the server to update the color of the platyer sprite given a color chosen by the client
    /// </summary>
    /// <param name="color">Color chosen by the client</param>
    [ServerRpc]

    public void ChangePlayerColorServerRpc(Color color)
    {
        m_Color.Value = color;
        GetComponent<SpriteRenderer>().color = color;
    }
    /// <summary>
    /// Updates the time left only if local player
    /// </summary>
    /// <param name="time"></param>
    [ClientRpc]
    public void UpdateCountDownGUIClientRpc(int time)
    {
        if (IsLocalPlayer)
        {
            //Solo si es local, ya que la GUI pertenece al local
            m_UIManager.UpdateCountDownTime(time);

        }
    }
    /// <summary>
    /// Tell the clients to activate weapons and chooting systems and to activate the ingame GUI
    /// </summary>
    [ClientRpc]
    public void EnableWeaponsAndGUIClientRpc()
    {
        GetComponent<WeaponAim>().EnableWeapons();
        if (IsLocalPlayer)
        {
            //Solo si es local, ya que la GUI pertenece al local
            m_UIManager.ActivateInGameHUD();
            m_GameManager.m_LobbySetup.SetActive(false);
        }
    }
    /// <summary>
    /// Tells the clients to store in their managers the players
    /// </summary>
    [ClientRpc]
    public void StorePlayersClientRpc()
    {
        //Solo si es local, ya que el manager pertenece al local
        if (IsLocalPlayer) m_GameManager.StorePlayersClientSide();
    }

    /// <summary>
    /// Propagates the death of the player across the network, reaching every client
    /// </summary>
    /// <param name="killerName">Name of the killer</param>
    [ClientRpc]
    public void KillPlayerClientRpc(string killerName)
    {
        //Se pinta en la gui un mensaje avisando de quien a matado a quien para todos los clientes
        m_UIManager.ActivateAndUpdateKillNotification(killerName + " KILLED " + m_PlayerName.Value + "!");
        if (IsLocalPlayer)
        {
            //Si es el jugador que ha muerto, se activa la pantalla de muerte y espera a respawnear
            m_UIManager.ActivateDeathCanvas();
        }
        //Se desactiva a si mismo hasta nuevo aviso
        gameObject.SetActive(false);
    }
    /// <summary>
    /// Tells the client to update the state of the player crown, given a condition
    /// </summary>
    /// <param name="condition"></param>
    [ClientRpc]
    private void ManageCrownRenderClientRpc(bool condition)
    {
        m_Crown.enabled = condition;
    }
    /// <summary>
    /// Tells the clients to respawn the player
    /// </summary>
    [ClientRpc]
    public void RespawnPlayerClientRpc()
    {
        //Vuelve a activar al jugador, como las funciones de onEnable estan fastanticamente implementadas
        //todo va a la perfeccion
        gameObject.SetActive(true);

    }
    /// <summary>
    /// Tells the clients to prepare for the end game
    /// </summary>
    [ClientRpc]
    public void InitializeEndGameActionsClientRpc()
    {
        if (IsLocalPlayer)
        {
            //Se desactiva el control
            GetComponent<InputHandler>().enabled = false;
            //Se desactiva la velocidad
            GetComponent<PlayerController>().m_Body.velocity = new Vector2(0, GetComponent<PlayerController>().m_Body.velocity.y);
            //Se activa la pantalla de leaderboards y ranking
            m_UIManager.ActivateEndGameCanvas();

        }
    }


    #endregion

    #region Netcode Related Methods
    //Network variables simple callback methods
    void OnPlayerNameChanged(FixedString64Bytes old, FixedString64Bytes current)
    {
        m_PlayerNameRenderer.text = m_PlayerName.Value.ToString();
    }
    private void UpdateGUILife(int previousValue, int newValue)
    {
        m_UIManager.UpdateLifeUI(newValue);
    }
    private void UpdateGUIKills(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerKills(newValue);
        m_UIManager.ActivateKillCanvas();
    }
    private void UpdateGUIDeaths(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerDeaths(newValue);
    }
    private void UpdateGUITime(int PreviousTotalTimeLeft, int NewTimeLeft)
    {
        m_UIManager.UpdateTimeLeft(NewTimeLeft);
    }
    private void UpdateGUIPlayers(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerNumber(newValue);
    }
    private void UpdateGUIPoints(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerPoints(newValue);
    }
    private void UpdateGUIReadyPlayers(int previousValue, int newValue)
    {
        m_GameManager.m_PlayersReady = newValue;
        m_UIManager.UpdatePlayersReadyNumber(newValue);
    }
    private void ChangePlayerColor(Color previousValue, Color newValue)
    {
        GetComponent<SpriteRenderer>().color = newValue;
    }
    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
