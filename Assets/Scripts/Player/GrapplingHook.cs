using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Class that controls the logic behind the player´s grappling hook
/// </summary>
public class GrapplingHook : NetworkBehaviour
{
    #region Variables

    InputHandler m_Handler;
    DistanceJoint2D m_Rope;
    LineRenderer m_RopeRenderer;
    Transform m_PlayerTransform;
    [SerializeField] Material m_Mat;
    LayerMask m_Layer;
    Player m_Player;

    readonly float m_ClimbSpeed = 2f;
    readonly float m_SwingForce = 80f;

    Rigidbody2D m_RBody;

    NetworkVariable<float> m_RopeDistance;

    #endregion

    #region Unity Event Functions

    void Awake()
    {
        m_Handler = GetComponent<InputHandler>();
        m_Player = GetComponent<Player>();

        //Configure Rope Renderer
        m_RopeRenderer = gameObject.AddComponent<LineRenderer>();
        m_RopeRenderer.startWidth = .05f;
        m_RopeRenderer.endWidth = .05f;
        m_RopeRenderer.material = m_Mat;
        m_RopeRenderer.sortingOrder = 3;
        m_RopeRenderer.enabled = false;

        m_PlayerTransform = transform;
        m_Layer = LayerMask.GetMask("Obstacles");

        m_RBody = GetComponent<Rigidbody2D>();
        m_Player = GetComponent<Player>();

        m_RopeDistance = new NetworkVariable<float>();
    }

    private void OnEnable()
    {
        if (IsOwner)
        {
            m_Handler.OnHookRender.AddListener(UpdateHookServerRpc);
            m_Handler.OnMoveFixedUpdate.AddListener(SwingRopeServerRpc);
            m_Handler.OnJump.AddListener(JumpPerformedServerRpc);
            m_Handler.OnHook.AddListener(LaunchHookServerRpc);
        }

    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            m_Handler.OnHookRender.RemoveListener(UpdateHookServerRpc);
            m_Handler.OnMoveFixedUpdate.RemoveListener(SwingRopeServerRpc);
            m_Handler.OnJump.RemoveListener(JumpPerformedServerRpc);
            m_Handler.OnHook.RemoveListener(LaunchHookServerRpc);
        }
    }
    private void Start()
    {
        if (IsOwner)
        {
            m_Handler.OnHookRender.AddListener(UpdateHookServerRpc);
            m_Handler.OnMoveFixedUpdate.AddListener(SwingRopeServerRpc);
            m_Handler.OnJump.AddListener(JumpPerformedServerRpc);
            m_Handler.OnHook.AddListener(LaunchHookServerRpc);
        }
        if (IsServer)
        {
            // Configura el Rope solo si es servidor, los clientes no hace falta que tengan este componente
            //ya que con el network transform y el renderer de la cuerda es mas que suficiente
            m_Rope = gameObject.AddComponent<DistanceJoint2D>();
            m_Rope.enableCollision = true;
            m_Rope.enabled = false;
        }
    }
    #endregion
    /// <summary>
    /// Server side. Disables the hook and tells the clients to do so.
    /// </summary>
    public void DisableHook()
    {
        m_Rope.enabled = false;
        m_RopeRenderer.enabled = false;
        RemoveRopeClientRpc();
    }

    #region Netcode RPC

    #region ServerRPC
    /// <summary>
    /// Updates the state of the hook
    /// </summary>
    /// <param name="input"></param>
    [ServerRpc]
   
    void UpdateHookServerRpc(Vector2 input)
    {
        if (m_Player.m_State.Value == PlayerState.Hooked)
        {
            ClimbRope(input.y);
            UpdateRopeClientRpc();

        }
        else if (m_Player.m_State.Value == PlayerState.Grounded)
        {
            DisableHook();
        }
    }
    /// <summary>
    /// Tells the server to disable the hook if player jumps being hooked.
    /// </summary>
    [ServerRpc]
    void JumpPerformedServerRpc()
    {
        DisableHook();
    }
    /// <summary>
    /// Tells the server to activate the hook and propagate it across the netwok.
    /// </summary>
    /// <param name="input"></param>
    [ServerRpc]
    void LaunchHookServerRpc(Vector2 input)
    {
        if (m_Player.m_State.Value != PlayerState.Grounded)
        {
            var hit = Physics2D.Raycast(m_PlayerTransform.position, input - (Vector2)m_PlayerTransform.position, Mathf.Infinity, m_Layer);

            if (hit.collider)
            {
                //Importante activarlo ahora en el servidor, si no no funciona
                m_Rope.enabled = true;
                m_RopeRenderer.enabled = true;

                var anchor = hit.centroid;
                m_Rope.connectedAnchor = anchor;
                m_RopeRenderer.SetPosition(1, anchor);
                UpdateAnchorClientRpc(hit.centroid);
                m_Player.m_State.Value = PlayerState.Hooked;
            }
        }
    }
    /// <summary>
    /// Tells the server to apply a swinging force resulting of a keyboard input.
    /// </summary>
    /// <param name="input"></param>
    [ServerRpc]
    void SwingRopeServerRpc(Vector2 input)
    {

        if (m_Player.m_State.Value == PlayerState.Hooked)
        {
            var direction = (m_Rope.connectedAnchor - (Vector2)m_PlayerTransform.position).normalized;

            var forceDirection = new Vector2(input.x * direction.y, direction.x);

            var force = forceDirection * m_SwingForce;
            m_RBody.AddForce(force, ForceMode2D.Force);
        }

    }

    #endregion

    #region ClientRPC
    //Simple methods
    [ClientRpc]
    void UpdateAnchorClientRpc(Vector2 anchor)
    {
        m_RopeRenderer.enabled = true;
        m_RopeRenderer.SetPosition(1, anchor);
    }

    [ClientRpc]
    void UpdateRopeClientRpc()
    {
        
        m_RopeRenderer.SetPosition(0, m_PlayerTransform.position);
    }

    [ClientRpc]
    void RemoveRopeClientRpc()
    {
        m_RopeRenderer.enabled = false;
    }

    #endregion

    #endregion

    #region Methods
    /// <summary>
    /// Server  side
    /// </summary>
    /// <param name="input"></param>
    void ClimbRope(float input)
    {
        //m_RopeDistance.Value = (input) * m_ClimbSpeed * Time.deltaTime;
        m_Rope.distance -= (input) * m_ClimbSpeed * Time.deltaTime;
    }



    #endregion
}
