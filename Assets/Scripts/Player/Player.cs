using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    #region Variables


    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    [SerializeField] public NetworkVariable<PlayerState> m_State;

    [SerializeField] public TextMesh m_PlayerName;

    private UIManager m_UIManager;

    public NetworkVariable<FixedString64Bytes> m_Text;


    #endregion


    #region Unity Event Functions

    //public override void OnNetworkSpawn()
    //{
    //    print("pn network spawn!");
    //}


    private void Awake()
    {
        m_PlayerName = GetComponentInChildren<TextMesh>();
        m_State = new NetworkVariable<PlayerState>();
        m_Text = new NetworkVariable<FixedString64Bytes>();
    }
    private void Start()
    {
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();

        }

    }

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        m_State.OnValueChanged += OnPlayerStateValueChanged;
        m_Text.OnValueChanged += OnPlayerNameChanged;

    }

    private void OnDisable()
    {
        m_State.OnValueChanged -= OnPlayerStateValueChanged;
    }


    #endregion

    #region Config Methods

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);

        if (IsLocalPlayer)
        {
            GameManager manager = FindObjectOfType<GameManager>();
            m_UIManager = FindObjectOfType<UIManager>();
            UpdateMyNameServerRpc(m_UIManager.m_InputFieldName.text, NetworkObjectId);
            manager.m_LocalPlayer = this;
        }
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


    #endregion

    public void ComputeDamage()
    {
        m_UIManager.m_HitPoints++;
        m_UIManager.UpdateLifeUI(m_UIManager.m_HitPoints);
    }
    #region RPC



    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        m_State.Value = state;
    }

    [ServerRpc]
    public void UpdateMyNameServerRpc(string name, ulong clientId)
    {
        if (NetworkObjectId == clientId) m_PlayerName.text = name;
        //FindObjectOfType<PlayerManager>().m_PlayerNames.Add(clientId, name);
        UpdateSpecificPlayerNameClientRpc(name, clientId);

    }
    [ClientRpc]
    public void UpdateSpecificPlayerNameClientRpc(string name, ulong clientId)
    {
        if (NetworkObjectId == clientId) m_PlayerName.text = name;
        //FindObjectOfType<PlayerManager>().m_PlayerNames.Add(clientId, name);
        print(name + " HA LLEGADO");

    }


    #endregion

    #region Netcode Related Methods

    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        //m_State.Value = current;
        print(m_State.Value);
    }
    void OnPlayerNameChanged(FixedString64Bytes old, FixedString64Bytes current)
    {
        m_Text.Value = current;
    }

    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
