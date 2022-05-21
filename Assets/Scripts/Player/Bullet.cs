using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{

    Rigidbody2D m_RBody;



    private void Start()
    {
        m_RBody = GetComponent<Rigidbody2D>();
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

            Player targetPlayer = collision.gameObject.GetComponent<Player>();

            targetPlayer.ComputeDamage(OwnerClientId);

            GetComponent<NetworkObject>().Despawn();
        }
    }

}
