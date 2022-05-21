using UnityEngine;
using Unity.Netcode;

public class WeaponAim : NetworkBehaviour
{

    #region Variables

    [SerializeField] Transform m_CrossHair;
    [SerializeField] Transform m_Weapon;

    SpriteRenderer m_WeaponRenderer;

    InputHandler m_Handler;
    Player m_Player;
    [SerializeField]GameObject m_BulletPrefab;

    NetworkVariable<Vector3> m_WeaponRight;
    NetworkVariable<bool> m_FlipSprite;

    readonly float m_BulletSpeed=3f;

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
        //Check if player is not local in order to hide enemies crosshairs
        if (!IsLocalPlayer)
        {
            m_CrossHair.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        }

        if (IsOwner)
        {
            m_Handler.OnMousePosition.AddListener(UpdateCrosshairPosition);
            m_Handler.OnFire.AddListener(PerformShotServerRpc);
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

    void UpdateCrosshairPosition(Vector2 input)
    {
        // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
        var worldMousePosition = Camera.main.ScreenToWorldPoint(input);

        var facingDirection = worldMousePosition - transform.position;


        var aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        SetCrossHairPosition(aimAngle);

        UpdateWeaponOrientationServerRPC(m_CrossHair.localPosition.x, m_CrossHair.position - m_Weapon.position);
        //UpdateWeaponOrientation();

    }


    void SetCrossHairPosition(float aimAngle)
    {
        var x = transform.position.x + .5f * Mathf.Cos(aimAngle);
        var y = transform.position.y + .5f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        m_CrossHair.transform.position = crossHairPosition;
    }

    GameObject ShootBullet(Vector3 weaponPos,Vector2 velocity)
    {

        var bullet = Instantiate(m_BulletPrefab, weaponPos, m_Player.transform.rotation);
        bullet.GetComponent<Rigidbody2D>().velocity = velocity;
        return bullet;

    }

    #endregion
    #region Network
    private void OnChangeWeaponRight(Vector3 previousValue, Vector3 newValue)
    {
        m_Weapon.right = newValue;
    }
    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        m_WeaponRenderer.flipY = current;
    }
    #endregion
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
    [ServerRpc]
    private void PerformShotServerRpc(Vector2 target, ulong id)
    {
        if ((m_Player.m_State.Value != PlayerState.Hooked))
        {

            Vector2 velocity = (target - (Vector2)m_Player.transform.position).normalized * m_BulletSpeed;
            Vector2 offset = Vector2.ClampMagnitude(velocity,velocity.magnitude*0.08f);
            ShootBullet(m_Weapon.transform.position+new Vector3(offset.x,offset.y,0), velocity).GetComponent<NetworkObject>().SpawnWithOwnership(id);
        }
    }
}
