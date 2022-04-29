using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    UnityTransport m_Transport;
    [SerializeField] Sprite[] m_Hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject m_MainMenu;
    [SerializeField] private Button m_ButtonHost;
    [SerializeField] private Button m_ButtonClient;
    [SerializeField] private Button m_ButtonServer;
    [SerializeField] private InputField m_InputFieldIP;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject m_InGameHUD;
    [SerializeField] RawImage[] m_HeartsUI = new RawImage[3];

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        m_Transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        NetworkManager.Singleton.OnClientConnectedCallback += test;
    }

    private void Start()
    {
        m_ButtonHost.onClick.AddListener(() => StartHost());
        m_ButtonClient.onClick.AddListener(() => StartClient());
        m_ButtonServer.onClick.AddListener(() => StartServer());
        ActivateMainMenu();


    }

    #endregion

    #region UI Related Methods

    private void ActivateMainMenu()
    {
        m_MainMenu.SetActive(true);
        m_InGameHUD.SetActive(false);
    }

    private void ActivateInGameHUD()
    {
        m_MainMenu.SetActive(false);
        m_InGameHUD.SetActive(true);

        // for test purposes
        UpdateLifeUI(Random.Range(1, 6));
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
            m_Transport.ConnectionData.Address = ip;
        }
       
        NetworkManager.Singleton.StartClient();
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }
    private void test(ulong id)
    {
        print("noo");
    }
    #endregion

}
