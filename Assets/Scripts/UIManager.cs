using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    [SerializeField] NetworkManager m_NetworkManager;
    [SerializeField] GameManager m_GameManager;
    UnityTransport m_Transport;

    readonly ushort m_Port = 7777;

    [SerializeField] Sprite[] m_Hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject m_LoginMenu;
    [SerializeField] private GameObject m_MainMenu;
    [SerializeField] private GameObject m_MainMenuCanvas;
    [SerializeField] private Button m_ButtonSubmit;
    [SerializeField] private Button m_ButtonHost;
    [SerializeField] private Button m_ButtonClient;
    [SerializeField] private Button m_ButtonServer;
    [SerializeField] private InputField m_InputFieldIP;
    [SerializeField] public InputField m_InputFieldName;

    [SerializeField] public Text m_PlayerNickName;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject m_InGameHUD;
    [SerializeField] RawImage[] m_HeartsUI = new RawImage[3];
    [SerializeField] public Text m_NumPlayers;
    [HideInInspector] public int m_HitPoints=0; 

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        m_Transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
        m_MainMenu.SetActive(false);
        m_InGameHUD.SetActive(false);

    }

    private void Start()
    {
        m_ButtonSubmit.onClick.AddListener(ActivateMainMenu);
        m_ButtonHost.onClick.AddListener(() => StartHost());
        m_ButtonClient.onClick.AddListener(() => StartClient());
        m_ButtonServer.onClick.AddListener(() => StartServer());

        //ActivateMainMenu();


    }

    #endregion

    #region UI Related Methods

    public void UpdatePlayerNumber(int num)
    {
        m_NumPlayers.text = num.ToString() + "/6 PLAYERS";
          
    }
    private void ActivateMainMenu()
    {

        if (m_InputFieldName.text != "")
        {
            m_LoginMenu.SetActive(false);
            m_MainMenu.SetActive(true);
            m_InGameHUD.SetActive(false);
            m_PlayerNickName.text = "Welcome "+m_InputFieldName.text;
            m_InputFieldIP.placeholder.GetComponent<Text>().text = m_Transport.ConnectionData.Address;
        }
        else
        {
            m_InputFieldName.placeholder.GetComponent<Text>().color = Color.red;
            StartCoroutine(ChangeTextColor(1.5f));

        }
    }
    private IEnumerator ChangeTextColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        m_InputFieldName.placeholder.GetComponent<Text>().color = Color.black;
    }

    private void ActivateInGameHUD()
    {
        m_MainMenuCanvas.SetActive(false);
        m_MainMenu.SetActive(false);
        m_InGameHUD.SetActive(true);

        // for test purposes
        UpdateLifeUI(m_HitPoints);
    }

    public void UpdateLifeUI(int hitpoints)
    {
        switch (hitpoints)
        {
            case 6:
                m_HeartsUI[0].texture = m_Hearts[2].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 5:
                m_HeartsUI[0].texture = m_Hearts[1].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 4:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 3:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[1].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 2:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[0].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 1:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[0].texture;
                m_HeartsUI[2].texture = m_Hearts[1].texture;
                break;
        }
    }

    #endregion




    #region Netcode Related Methods

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        ActivateInGameHUD();
    }

    private void StartClient()
    {
        var ip = m_InputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            m_Transport.SetConnectionData(ip, m_Port);
        }
        NetworkManager.Singleton.StartClient();
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

    #endregion

}

