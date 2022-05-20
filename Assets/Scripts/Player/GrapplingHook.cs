using UnityEngine;
using Unity.Netcode;

public class GrapplingHook : NetworkBehaviour
{
    #region Variables

    InputHandler m_Handler;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/DistanceJoint2D.html
    DistanceJoint2D m_Rope;
    // // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LineRenderer.html
    LineRenderer m_RopeRenderer;
    Transform m_PlayerTransform;
    [SerializeField] Material m_Mat;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LayerMask.html
    LayerMask m_Layer;
    Player m_Player;

    readonly float m_ClimbSpeed = 2f;
    readonly float m_SwingForce = 80f;

    Rigidbody2D m_RBody;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
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

        // Configure Rope
        m_Rope = gameObject.AddComponent<DistanceJoint2D>();
        m_Rope.enableCollision = true;
        m_Rope.enabled = false;

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
        m_RopeDistance.OnValueChanged += OnRopeDistanceValueChanged;
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
        m_RopeDistance.OnValueChanged -= OnRopeDistanceValueChanged;
    }
    private void Start()
    {
        if (IsOwner)
        {
            m_Handler.OnHookRender.RemoveListener(UpdateHookServerRpc);
            m_Handler.OnMoveFixedUpdate.RemoveListener(SwingRopeServerRpc);
            m_Handler.OnJump.RemoveListener(JumpPerformedServerRpc);
            m_Handler.OnHook.RemoveListener(LaunchHookServerRpc);
        }
    }
    #endregion

    #region Netcode RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
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
            RemoveRopeClientRpc();
            m_Rope.enabled = false;
            m_RopeRenderer.enabled = false;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void JumpPerformedServerRpc()
    {
        RemoveRopeClientRpc();
        m_Rope.enabled = false;
        m_RopeRenderer.enabled = false;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void LaunchHookServerRpc(Vector2 input)
    {
        var hit = Physics2D.Raycast(m_PlayerTransform.position, input - (Vector2)m_PlayerTransform.position, Mathf.Infinity, m_Layer);

        if (hit.collider)
        {
            var anchor = hit.centroid;
            m_Rope.connectedAnchor = anchor;
            m_RopeRenderer.SetPosition(1, anchor);
            UpdateAnchorClientRpc(hit.centroid);
            m_Player.m_State.Value = PlayerState.Hooked;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void SwingRopeServerRpc(Vector2 input)
    {

        if (m_Player.m_State.Value == PlayerState.Hooked)
        {
            // Player 2 hook direction
            var direction = (m_Rope.connectedAnchor - (Vector2)m_PlayerTransform.position).normalized;

            // Perpendicular direction
            var forceDirection = new Vector2(input.x * direction.y, direction.x);

            var force = forceDirection * m_SwingForce;

            m_RBody.AddForce(force, ForceMode2D.Force);
        }

    }

    #endregion

    #region ClientRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateAnchorClientRpc(Vector2 anchor)
    {
        m_Rope.connectedAnchor = anchor;
        ShowRopeClientRpc();
        m_RopeRenderer.SetPosition(1, anchor);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateRopeClientRpc()
    {
        m_RopeRenderer.SetPosition(0, m_PlayerTransform.position);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void ShowRopeClientRpc()
    {
        m_Rope.enabled = true;
        m_RopeRenderer.enabled = true;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void RemoveRopeClientRpc()
    {
        m_Rope.enabled = false;
        m_RopeRenderer.enabled = false;
    }

    #endregion

    #endregion

    #region Methods

    void ClimbRope(float input)
    {
        m_RopeDistance.Value = (input) * m_ClimbSpeed * Time.deltaTime;
    }

    void OnRopeDistanceValueChanged(float previous, float current)
    {
        m_Rope.distance -= current;
    }

    #endregion
}
