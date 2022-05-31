using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Class that represents bullet and logic behind the collisions
/// </summary>
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
            //La logica la llea el servidor. El servido manda
        {
            if (collision.gameObject.layer == 6)
            {
                //Si choca contra el escenario desparece a traves de la red y se acabo
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            ulong targetClientId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            if (targetClientId == OwnerClientId) return; //Si choca contra el mismo jugador que dispara se ignora

            Player targetPlayer = collision.gameObject.GetComponent<Player>();

            //Se computa el daño cuando colisiona conra otro jugador
            targetPlayer.ComputeDamage(OwnerClientId);

            GetComponent<NetworkObject>().Despawn();
        }
    }

}
