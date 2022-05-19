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
    [SerializeField]public NetworkVariable<PlayerState> m_State;

    [SerializeField]public TextMesh m_PlayerName;

    //private GameManager m_GameManager;

    public NetworkVariable<FixedString64Bytes> m_Text;

    
    #endregion

   
    #region Unity Event Functions



    private void Awake()
    {
        //NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        m_PlayerName = GetComponentInChildren<TextMesh>();
        m_State = new NetworkVariable<PlayerState>();
        m_Text = new NetworkVariable<FixedString64Bytes>();
    }
    private void Start()
    {
        //print(IsOwner);
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
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        m_State.OnValueChanged -= OnPlayerStateValueChanged;
    }
   

    #endregion

    #region Config Methods

    public void ConfigurePlayer(ulong clientID)
    {
        print("CAMARA READY");

        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();
        }
    }

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);

        if (IsLocalPlayer) UpdateMyNameServerRpc(FindObjectOfType<GameManager>().m_LocalPlayerNickName, NetworkObjectId);
    }

    void ConfigureCamera()
    {
      
        
            // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
            var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

            virtualCam.LookAt = transform;
            virtualCam.Follow = transform;
        

    }

    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    #endregion

    #region RPC

   

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        m_State.Value = state;
    }
    [ServerRpc]
    public void UpdateMyNameServerRpc(string name, ulong clientId)
    {
        print("pETICION A SERVER");
        if (NetworkObjectId == clientId) m_PlayerName.text = name;
        UpdateSpecificPlayerNameClientRpc(name,clientId);

    }
    [ClientRpc]
    public void UpdateSpecificPlayerNameClientRpc(string name, ulong clientId)
    {
        print("pETICION A CLIENTES "+clientId+name);
        if (NetworkObjectId == clientId) m_PlayerName.text = name;

    }


    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        m_State.Value = current;
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
