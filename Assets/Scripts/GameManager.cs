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

    [SerializeField] GameObject m_PlayerPrefab;

    [HideInInspector]public string m_PlayerNickName;

    //UnityEvent OnPlayerCreated;

    private void Start()
    {
        //print("started");
    }
    private void OnEnable()
    {
        m_NetWorkManager.OnServerStarted += OnServerReady;
        m_NetWorkManager.OnClientConnectedCallback += OnClientConnected;
    }


    private void OnDisable()
    {

        //networkManager.OnServerStarted -= OnServerReady;
        m_NetWorkManager.OnClientConnectedCallback -= OnClientConnected;
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
        //if(m_NetWorkManager.)
        if (m_NetWorkManager.IsServer)
        {

            ulong[] target = { clientId };
            ClientRpcParams rpcParams = default;
            rpcParams.Send.TargetClientIds = target;

            GetNameClientRPC(clientId,rpcParams);


            //Primero se instancia en unity y luegose le dice a su network object que lo instancie a todos

            //var player = Instantiate(m_PlayerPrefab);

            //player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);


        }

    }
    #region RPCs

    [ClientRpc]
    private void GetNameClientRPC(ulong clientId, ClientRpcParams rpcParams = default)
    {
        BroadcastClientInstanceServerRPC(m_PlayerNickName, clientId);
    }
    [ServerRpc]
    private void BroadcastClientInstanceServerRPC(string playerName, ulong clientId)
    {
        var player = Instantiate(m_PlayerPrefab);
        print(playerName);
        player.GetComponent<Player>().m_NickNameString=playerName;
        
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
    #endregion


}
