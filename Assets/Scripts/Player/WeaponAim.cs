using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Class that controls the aiming, shooting and rendering of the weapon and crosshair of the player.
/// </summary>
public class WeaponAim : NetworkBehaviour
{

    #region Variables

    [SerializeField] Transform m_CrossHair;
    [SerializeField] Transform m_Weapon;

    SpriteRenderer m_WeaponRenderer;

    InputHandler m_Handler;
    Player m_Player;
    [SerializeField] GameObject m_BulletPrefab;

    NetworkVariable<Vector3> m_WeaponRight;
    NetworkVariable<bool> m_FlipSprite;

    const float BULLET_SPEED = 7.5f;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        m_Handler = GetComponent<InputHandler>();
        m_Player = GetComponent<Player>();
        m_WeaponRenderer = m_Weapon.gameObject.GetComponent<SpriteRenderer>();

        m_WeaponRight = new NetworkVariable<Vector3>();
        m_FlipSprite = new NetworkVariable<bool>();

    }
    private void Start()
    {

        if (IsOwner)
        {
            m_Handler.OnMousePosition.AddListener(UpdateCrosshairPosition);
        }

    }

    private void OnEnable()
    {
        if (IsOwner)
        {
            m_Handler.OnMousePosition.AddListener(UpdateCrosshairPosition);
            m_Handler.OnFire.AddListener(PerformShotServerRpc);
        }

        m_WeaponRight.OnValueChanged += OnChangeWeaponRight;
        m_FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;

    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            m_Handler.OnMousePosition.RemoveListener(UpdateCrosshairPosition);
            m_Handler.OnFire.RemoveListener(PerformShotServerRpc);
        }

        m_FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
    }

    #endregion

    #region Methods
    /// <summary>
    /// Client side. Enables weaponry.
    /// </summary>
    public void EnableWeapons()
    {
        m_WeaponRenderer.enabled = true;
        if (IsLocalPlayer)
            //Para que solo se vea en el jugador local
            m_CrossHair.gameObject.GetComponent<SpriteRenderer>().enabled = true;
        if (IsOwner)
            //Se suscribe la funcion de disparo para que pueda disparar
            m_Handler.OnFire.AddListener(PerformShotServerRpc);
    }
    /// <summary>
    /// Client side. Updates the crosshair position
    /// </summary>
    /// <param name="input"></param>
    void UpdateCrosshairPosition(Vector2 input)
    {
        var worldMousePosition = Camera.main.ScreenToWorldPoint(input);

        var facingDirection = worldMousePosition - transform.position;


        var aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        SetCrossHairPosition(aimAngle);

        UpdateWeaponOrientationServerRPC(m_CrossHair.localPosition.x, m_CrossHair.position - m_Weapon.position);

    }

    /// <summary>
    /// Client side. Stores the crosshair position.
    /// </summary>
    /// <param name="aimAngle"></param>
    void SetCrossHairPosition(float aimAngle)
    {
        var x = transform.position.x + .5f * Mathf.Cos(aimAngle);
        var y = transform.position.y + .5f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        m_CrossHair.transform.position = crossHairPosition;
    }
    /// <summary>
    /// Server side. Instatiantes a bullet with a velocity and returns the game object for further actions.
    /// </summary>
    /// <param name="weaponPos"></param>
    /// <param name="velocity"></param>
    /// <returns></returns>
    GameObject ShootBullet(Vector3 weaponPos, Vector2 velocity)
    {

        var bullet = Instantiate(m_BulletPrefab, weaponPos, m_Player.transform.rotation);
        bullet.GetComponent<Rigidbody2D>().velocity = velocity;
        return bullet;

    }

    #endregion
    #region RPCs
    /// <summary>
    /// Tells the server to update the crosshair position updating all network the necessary network variables and
    /// thus, propagating across the network.
    /// </summary>
    /// <param name="crossHairX"></param>
    /// <param name="newOrientation"></param>
    [ServerRpc]
    void UpdateWeaponOrientationServerRPC(float crossHairX, Vector3 newOrientation)
    {
        m_WeaponRight.Value = newOrientation;
        m_Weapon.right = m_WeaponRight.Value;

        if (crossHairX > 0)
        {
            m_FlipSprite.Value = false;
        }
        else
        {
            m_FlipSprite.Value = true;
        }
    }
    /// <summary>
    /// Tells the server to instantiate and spawn a bullet across the network in the target direction.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="id"></param>
    [ServerRpc]
    private void PerformShotServerRpc(Vector2 target, ulong id)
    {
        if ((m_Player.m_State.Value != PlayerState.Hooked))
        {
            //Calculos de la direccion y velocidad
            Vector2 velocity = (target - (Vector2)m_Player.transform.position).normalized * BULLET_SPEED;
            Vector2 offset = Vector2.ClampMagnitude(velocity, velocity.magnitude * 0.08f);
            //Spawn con ownership del cliente que dispara
            ShootBullet(m_Weapon.transform.position + new Vector3(offset.x, offset.y, 0), velocity).GetComponent<NetworkObject>().SpawnWithOwnership(id);
        }
    }
    #endregion
    #region Network
    //Simple methods
    private void OnChangeWeaponRight(Vector3 previousValue, Vector3 newValue)
    {
        m_Weapon.right = newValue;
    }
    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        m_WeaponRenderer.flipY = current;
    }
    #endregion

}
