using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> m_State;

    public string m_NickNameString;
    public TextMesh m_PlayerName;
    #endregion

   
    #region Unity Event Functions



    private void Awake()
    {
        //NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        m_PlayerName = GetComponentInChildren<TextMesh>();
        m_State = new NetworkVariable<PlayerState>();
    }
    private void Start()
    {
        print("CAMARA READY");
        //print(IsOwner);
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();
        }
        m_PlayerName.text = m_NickNameString;
    }

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        m_State.OnValueChanged += OnPlayerStateValueChanged;

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

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        m_State.Value = state;
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        m_State.Value = current;
    }

    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
