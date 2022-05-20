using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{

    [HideInInspector] private UIManager m_UIManager;

    [HideInInspector] public Dictionary<ulong, string> m_PlayerNames;

    [HideInInspector] public int m_NumPlayers;

    private void Start()
    {
        m_UIManager = FindObjectOfType<UIManager>();
    }
    #region RPC
    [ClientRpc]
    public void UpdatePlayerNumberClientRPC(int num)
    {
        m_UIManager.UpdatePlayerNumber(num);
    }
    [ClientRpc]
    public void ShowDisconnectedClientRPC(ulong id)
    {
        string name;
        m_PlayerNames.TryGetValue(id, out name);
        print(name + " SE HA IDO");
    }
    #endregion
}
