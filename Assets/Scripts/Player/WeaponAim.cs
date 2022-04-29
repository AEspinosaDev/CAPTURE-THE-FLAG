using UnityEngine;
using Unity.Netcode;

public class WeaponAim : NetworkBehaviour
{

    #region Variables

    [SerializeField] Transform m_CrossHair;
    [SerializeField] Transform m_Weapon;

    SpriteRenderer m_WeaponRenderer;

    InputHandler m_Handler;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        m_Handler = GetComponent<InputHandler>();
        m_WeaponRenderer = m_Weapon.gameObject.GetComponent<SpriteRenderer>();


    }
    private void Start()
    {
        //Check if player is not local in order to hide enemies crosshairs
        if (!IsLocalPlayer)
        {
            m_CrossHair.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnEnable()
    {
        m_Handler.OnMousePosition.AddListener(UpdateCrosshairPosition);
    }

    private void OnDisable()
    {
        m_Handler.OnMousePosition.RemoveListener(UpdateCrosshairPosition);
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

        UpdateWeaponOrientation();

    }

    void UpdateWeaponOrientation()
    {
        m_Weapon.right = m_CrossHair.position - m_Weapon.position;

        if (m_CrossHair.localPosition.x > 0)
        {
            m_WeaponRenderer.flipY = false;
        }
        else
        {
            m_WeaponRenderer.flipY = true;
        }
    }

    void SetCrossHairPosition(float aimAngle)
    {
        var x = transform.position.x + .5f * Mathf.Cos(aimAngle);
        var y = transform.position.y + .5f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        m_CrossHair.transform.position = crossHairPosition;
    }

    #endregion

}
