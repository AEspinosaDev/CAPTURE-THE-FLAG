using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;


/// <summary>
/// Class controlling the movement of the player and the changes to the animator
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : NetworkBehaviour
{

    #region Variables

    readonly float m_Speed = 3.4f;
    readonly float m_JumpHeigth = 6.5f;
    readonly float m_Gravity = 1.5f;
    readonly int m_MaxJumps = 2;

    LayerMask m_Layer;
    int m_JumpsLeft;

    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ContactFilter2D.html
    ContactFilter2D m_Filter;
    InputHandler m_Handler;
    Player m_Player;
    public Rigidbody2D m_Body;
    CapsuleCollider2D m_Collider;
    Animator m_Animator;
    SpriteRenderer m_SpriteRenderer;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> m_FlipSprite;

    bool IsGrounded => m_Collider.IsTouching(m_Filter) && m_Body.velocity.y==0;
    //bool IsGrounded;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        m_Body = GetComponent<Rigidbody2D>();
        m_Collider = GetComponent<CapsuleCollider2D>();
        m_Handler = GetComponent<InputHandler>();
        m_Player = GetComponent<Player>();
        m_Animator = GetComponent<Animator>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();

        m_FlipSprite = new NetworkVariable<bool>();
        m_JumpsLeft = m_MaxJumps;

    }

    private void OnEnable()
    {
        if (IsOwner)
        {
            m_Handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);
            m_Handler.OnJump.AddListener(PerformJumpServerRpc);
            m_Handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);


        }

        m_FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;

        //m_NetworkVel.OnValueChanged += UpdateClientVelocity;
    }

    //private void Update()
    //{
    //    if (IsServer)
    //        print(IsGrounded);

    //}
    private void OnDisable()
    {
        if (IsOwner)
        {
            m_Handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
            m_Handler.OnJump.RemoveListener(PerformJumpServerRpc);
            m_Handler.OnMoveFixedUpdate.RemoveListener(UpdatePlayerPositionServerRpc);

        }
        m_FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
    }

    void Start()
    {
        if (IsOwner)
        {
            m_Handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);
            m_Handler.OnJump.AddListener(PerformJumpServerRpc);
            m_Handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);
        }

        // Configure Rigidbody2D
        m_Body.freezeRotation = true;
        m_Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        m_Body.gravityScale = m_Gravity;

        // Configure LayerMask
        m_Layer = LayerMask.GetMask("Obstacles");

        // Configure ContactFilter2D
        m_Filter.minNormalAngle = 45;
        m_Filter.maxNormalAngle = 135;
        m_Filter.useNormalAngle = true;
        m_Filter.layerMask = m_Layer;
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        UpdateAnimatorStateServerRpc(input);
        UpdateSpriteOrientation(input);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateAnimatorStateServerRpc(Vector2 input)
    {
        if (IsGrounded)
        {
            if (input == Vector2.zero)
            {
                m_Animator.SetBool("isWalking", false);
            }
            else m_Animator.SetBool("isWalking", true);
            m_Animator.SetBool("isGrounded", true);
            m_Animator.SetBool("isJumping", false);
        }
        else
        {
            m_Animator.SetBool("isGrounded", false);
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void PerformJumpServerRpc()
    {
        if (m_Player.m_State.Value == PlayerState.Grounded)
        {
            m_JumpsLeft = m_MaxJumps;
        }
        else if (m_JumpsLeft == 0)
        {
            return;
        }

        m_Player.m_State.Value = PlayerState.Jumping;
        m_Animator.SetBool("isJumping", true);
        m_Body.velocity = new Vector2(m_Body.velocity.x, m_JumpHeigth);
        m_JumpsLeft--;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        if (IsGrounded)
        {
            //if (m_JumpsLeft <= 1)
            //{
            //    m_JumpsLeft = m_MaxJumps;
            //}
            m_Player.m_State.Value = PlayerState.Grounded;
        }
        //else if (m_Player.m_State.Value != PlayerState.Hooked)
        //{
        //    m_Player.m_State.Value = PlayerState.Jumping;
        //}

        if ((m_Player.m_State.Value != PlayerState.Hooked))
        {
            m_Body.velocity = new Vector2(input.x * m_Speed, m_Body.velocity.y);
        }
    }



    #endregion
    #region ClientRCP


    #endregion

    #endregion

    #region Methods

    void UpdateSpriteOrientation(Vector2 input)
    {
        if (input.x < 0)
        {
            m_FlipSprite.Value = false;
        }
        else if (input.x > 0)
        {
            m_FlipSprite.Value = true;
        }
    }

    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        m_SpriteRenderer.flipX = current;
    }



    #endregion

}
