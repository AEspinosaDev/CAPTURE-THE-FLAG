using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;



/// <summary>
/// Central class that controls the flow of the game
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager m_NetWorkManager;

    [SerializeField] GameObject m_PlayerPrefab;

    private void Start()
    {
        print("started");
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

        //print("Hola/Adios soy " + clientId);
        //Primero se instancia en unity y luegose le dice a su network object que lo instancie a todos
        if (m_NetWorkManager.IsServer)
        {
            var player = Instantiate(m_PlayerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        }

    }
}
