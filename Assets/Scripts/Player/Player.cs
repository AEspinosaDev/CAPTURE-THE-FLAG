using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;
using System;

public class Player : NetworkBehaviour
{
    #region Variables


    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable

    [SerializeField] public TextMesh m_PlayerNameRenderer;

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
    #endregion


    #endregion


    #region Unity Event Functions

    //public override void OnNetworkSpawn()
    //{
    //    UpdatePlayerNumberAndNamesServerRpc(m_PlayerName.Value.ToString());
    //}


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

            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();

            UpdatePlayerCountServerRpc();

        }
        if (IsServer)
        {
            m_Life.Value = MAX_HP;
            
        }


    }


    private void OnEnable()
    {

        m_State.OnValueChanged += OnPlayerStateValueChanged;
        m_PlayerName.OnValueChanged += OnPlayerNameChanged;

        if (IsLocalPlayer)
        {
            m_Life.OnValueChanged += UpdateGUILife;
            m_Kills.OnValueChanged += UpdateGUIKills;
            m_Deaths.OnValueChanged += UpdateGUIDeaths;


            m_Points.OnValueChanged += UpdateGUIPoints;

            m_TimeLeft.OnValueChanged += UpdateGUITime;
            m_Foes.OnValueChanged += UpdateGUIPlayers;
        }

    }


    private void OnDisable()
    {
        m_State.OnValueChanged -= OnPlayerStateValueChanged;
        m_PlayerName.OnValueChanged -= OnPlayerNameChanged;

        if (IsLocalPlayer)
        {
            m_Life.OnValueChanged -= UpdateGUILife;
            m_Kills.OnValueChanged -= UpdateGUIKills;
            m_Deaths.OnValueChanged -= UpdateGUIDeaths;

            m_Points.OnValueChanged -= UpdateGUIPoints;

            m_TimeLeft.OnValueChanged -= UpdateGUITime;
            m_Foes.OnValueChanged -= UpdateGUIPlayers;
        }

    }
    private void Update()
    {
        m_PlayerNameRenderer.text = m_PlayerName.Value.ToString();
    }

    #endregion

    #region Config Methods

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        UpdatePlayerNameServerRpc(m_UIManager.m_InputFieldName.text, NetworkObjectId);
        m_GameManager.m_LocalPlayer = this;
    }

    void ConfigureCamera()
    {
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }
    /// <summary>
    /// Server only. Called when a bullet hits a player on server.
    /// </summary>
    public void ComputeDamage(ulong enemyClientId)
    {
        m_Life.Value--;
        if (m_Life.Value == 0)
        {
            KillPlayer(enemyClientId);
        }
    }
    /// <summary>
    /// Server only.
    /// </summary>
    /// <param name="killerName"></param>
    public void KillPlayer(ulong enemyClientId)
    {
        Player killer;
        m_GameManager.m_Players.TryGetValue(enemyClientId, out killer);
        killer.m_Kills.Value++;
        killer.m_Points.Value += POINTS_PER_KILL;
        KillPlayerClientRpc(killer.m_PlayerName.Value.ToString());
        m_GameManager.RespawnPlayer(gameObject);
        gameObject.SetActive(false);

    }
    /// <summary>
    /// Server only.
    /// </summary>
    public void RespawnPlayer()
    {
        RespawnPlayerClientRpc();
        m_Deaths.Value++;
        if (m_Points.Value - POINTS_PER_DEATH < 0) m_Points.Value = 0; else m_Points.Value -= POINTS_PER_DEATH;
        m_Life.Value = MAX_HP;
    }
    #endregion

    #region RPC

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
    [ServerRpc]
    public void UpdatePlayerCountServerRpc()
    {
        m_GameManager.UpdatePlayersCount();
    }

    [ServerRpc]
    public void DisconnectPlayerServerRpc(ulong id)
    {
        m_GameManager.DisconnectPlayer(id);
    }

    [ClientRpc]
    public void KillPlayerClientRpc(string killerName)
    {
        m_UIManager.ActivateAndUpdateKillNotification(killerName + " KILLED " + m_PlayerName.Value + "!");
        if (IsLocalPlayer)
        {
            m_UIManager.ActivateDeathCanvas();
        }
        gameObject.SetActive(false);
    }

    [ClientRpc]
    public void RespawnPlayerClientRpc()
    {
        gameObject.SetActive(true);

    }
    [ClientRpc]
    public void InitializeEndGameActionsClientRpc()
    {
        if (IsLocalPlayer)
        {
            GetComponent<InputHandler>().enabled = false;
            GetComponent<PlayerController>().m_Body.velocity = new Vector2(0,GetComponent<PlayerController>().m_Body.velocity.y); 
            m_UIManager.ActivateEndGameCanvas();

        }
    }

    #endregion

    #region Netcode Related Methods

    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        //???????????????????????????????????
        print(m_State.Value);
    }
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
        int min = NewTimeLeft / 60;
        int seconds = NewTimeLeft % 60;
        string secondsText;
        if (seconds >= 10) secondsText = seconds.ToString(); else secondsText = "0" + seconds.ToString();
        m_UIManager.m_TimeLeft.text = min.ToString() + ":" + secondsText;
    }
    private void UpdateGUIPlayers(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerNumber(newValue);
    }
    private void UpdateGUIPoints(int previousValue, int newValue)
    {
        m_UIManager.UpdatePlayerPoints(newValue);
    }

    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
