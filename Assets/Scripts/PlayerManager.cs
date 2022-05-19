using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{

    [SerializeField] private UIManager m_UIManager;

    [HideInInspector] public Dictionary<uint, string> m_PlayerNames;

    [HideInInspector] public int m_NumPlayers;

    #region RPC
    [ClientRpc]
    public void UpdatePlayerNumberClientRPC(int num)
    {
        m_UIManager.UpdatePlayerNumber(num);
    }
    //[ClientRpc]
    //public void UpdatePlayerNamesClientRPC(Dictionary<uint, string> playerNames)
    //{

    //}
    #endregion
}
