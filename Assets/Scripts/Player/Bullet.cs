using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{

    Rigidbody2D m_RBody;
    CircleCollider2D m_Collider;



    //[HideInInspector]public Vector3 m_Direction;

    private void Start()
    {
        m_RBody = GetComponent<Rigidbody2D>();
        m_Collider = GetComponent<CircleCollider2D>();
        m_RBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.layer == 6)
            {
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            ulong targetClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            if (targetClientId == OwnerClientId) return;
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { targetClientId }
                }
            };
            DamagePlayerClientRPC(clientRpcParams);

            GetComponent<NetworkObject>().Despawn();
        }
    }
    [ClientRpc]
    void DamagePlayerClientRPC(ClientRpcParams clientRpcParams = default)
    {

        //UIManager gui = FindObjectOfType<UIManager>();
        //gui.m_HitPoints++;
        //gui.UpdateLifeUI(gui.m_HitPoints);
        FindObjectOfType<GameManager>().m_LocalPlayer.ComputeDamage();

    }


}
