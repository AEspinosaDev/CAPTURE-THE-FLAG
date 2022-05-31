using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Cinemachine;

public class UIManager : MonoBehaviour
{

    #region Variables

    [SerializeField] NetworkManager m_NetworkManager;
    [SerializeField] GameManager m_GameManager;
    UnityTransport m_Transport;

    readonly ushort m_Port = 7777;

    [SerializeField] Sprite[] m_Hearts = new Sprite[3];
    [SerializeField] Transform m_CameraStartingPosition;


    [Header("Main Menu")]

    [SerializeField] private GameObject m_LoginMenu;
    [SerializeField] private GameObject m_MainMenu;
    [SerializeField] private GameObject m_MainMenuCanvas;
    [SerializeField] private Button m_ButtonSubmit;
    [SerializeField] private Button m_ButtonHost;
    [SerializeField] private Button m_ButtonClient;
    [SerializeField] private Button m_ButtonServer;
    [SerializeField] private Button m_ButtonQuit;
    [SerializeField] private InputField m_InputFieldIP;
    [SerializeField] public InputField m_InputFieldName;

    [SerializeField] public Text m_PlayerNickName;

    [Header("Lobby Canvas")]
    [SerializeField] private GameObject m_LobbyCanvas;
    [SerializeField] public Text m_NumPlayersLobby;
    [SerializeField] public Text m_CountdownText;
    [SerializeField] public Text m_PlayersReady;
    [SerializeField] private Button m_ButtonReady;


    [Header("In-Game HUD")]

    [SerializeField] private GameObject m_InGameHUD;
    [SerializeField] RawImage[] m_HeartsUI = new RawImage[3];

    [SerializeField] public Text m_NumPlayers;
    [SerializeField] public Text m_Kills;
    [SerializeField] public Text m_Deaths;
    [SerializeField] public Text m_Points;

    [SerializeField] public GameObject m_DeathCanvas;
    [SerializeField] public Text m_KillCanvas;
    [SerializeField] public Text m_KillNotification;
    [SerializeField] public Text m_TimeLeft;

    [SerializeField] public Text m_FightText;


    [SerializeField] private Button m_ButtonQuitGame;

    [Header("End Game Canvas")]

    [SerializeField] private GameObject m_EndGameCanvas;

    [SerializeField] public Text[] m_PlayerResults;

    [SerializeField] private Button m_ButtonBack;





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
        m_ButtonQuit.onClick.AddListener(() => Application.Quit());
        m_ButtonBack.onClick.AddListener(QuitAndReturnToMainMenu);

        m_ButtonReady.onClick.AddListener(()=>
        {
            m_GameManager.m_LocalPlayer.SetPlayerReadyServerRpc(1);
            m_ButtonReady.gameObject.SetActive(false);
        });

    }

    #endregion

    #region UI Related Methods

    public void UpdatePlayerNumber(int num)
    {
        m_NumPlayers.text = num.ToString() + "/6 PLAYERS";
        m_NumPlayersLobby.text = num.ToString() + "/6 PLAYERS";
    }
    public void UpdatePlayersReadyNumber(int num)
    {
        m_PlayersReady.text = num.ToString() + " PLAYERS READY";
    }
    public void UpdatePlayerKills(int num)
    {
        m_Kills.text = num.ToString() + " KILLS";

    }
    public void UpdatePlayerDeaths(int num)
    {
        m_Deaths.text = num.ToString() + " DEATHS";

    }
    public void UpdatePlayerPoints(int num)
    {
        m_Points.text = num.ToString() + " POINTS";
    }
    public void UpdateTimeLeft(int currentTime)
    {
        int min = currentTime / 60;
        int seconds = currentTime % 60;
        string secondsText;
        if (seconds >= 10) secondsText = seconds.ToString(); else secondsText = "0" + seconds.ToString();
        m_TimeLeft.text = min.ToString() + ":" + secondsText;
    }
    public void UpdateCountDownTime(int currentTime)
    {
        if (currentTime == m_GameManager.START_MATCH_TIME)
            m_CountdownText.enabled = true;
        else if(currentTime==-1)
            m_CountdownText.enabled = false;
        m_CountdownText.text = "STARTING IN " + currentTime;
    }
    public void ActivateDeathCanvas()
    {
        m_DeathCanvas.SetActive(true);
        foreach (var h in m_HeartsUI)
        {
            h.enabled = false;
        }
        StartCoroutine(DeactivateDeathCanvas(4.8f));
    }
    public void ActivateKillCanvas()
    {
        m_KillCanvas.enabled = true;
        //StartCoroutine(DeactivateKillCanvas(1.8f));
        StartCoroutine(DeactivateText(1.8f, m_KillCanvas));
    }
    public void ActivateAndUpdateKillNotification(string news)
    {
        m_KillNotification.enabled = true;
        m_KillNotification.text = news;
        //StartCoroutine(DeactivateKillNotification(5f));
        StartCoroutine(DeactivateText(5f, m_KillNotification));

    }
    private void ActivateMainMenu()
    {

        if (m_InputFieldName.text != "")
        {
            m_LoginMenu.SetActive(false);
            m_MainMenu.SetActive(true);
            m_InGameHUD.SetActive(false);
            m_PlayerNickName.text = "Welcome " + m_InputFieldName.text;
            m_InputFieldIP.placeholder.GetComponent<Text>().text = m_Transport.ConnectionData.Address;
        }
        else
        {
            m_InputFieldName.placeholder.GetComponent<Text>().color = Color.red;
            StartCoroutine(ChangeTextColor(1.5f));

        }
    }
    public void ActivateLobbyCanvas()
    {
        m_MainMenuCanvas.SetActive(false);
        m_MainMenu.SetActive(false);
        m_LobbyCanvas.SetActive(true);
    }

    public void ActivateInGameHUD()
    {
        m_LobbyCanvas.SetActive(false);
        m_CountdownText.enabled = false;
        m_ButtonReady.gameObject.SetActive(true);
        m_InGameHUD.SetActive(true);
        StartCoroutine(DeactivateText(1.8f, m_FightText));

        UpdateLifeUI(6);
    }
    public void ActivateEndGameCanvas()
    {
        SetupRanking();
        m_InGameHUD.SetActive(false);
        m_EndGameCanvas.SetActive(true);
    }
    private IEnumerator ChangeTextColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        m_InputFieldName.placeholder.GetComponent<Text>().color = Color.black;
    }
    private IEnumerator DeactivateDeathCanvas(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        foreach (var h in m_HeartsUI)
        {
            h.enabled = true;
        }
        m_DeathCanvas.SetActive(false);

    }
    private IEnumerator DeactivateText(float waitTime, Text canvas)
    {
        yield return new WaitForSeconds(waitTime);
        canvas.enabled = false;
    }



    private void SetupRanking()
    {
        List<Player> players = new List<Player>();
        Player[] playersArr = FindObjectsOfType<Player>();
        foreach (var p in playersArr)
        {
            players.Add(p);
        }
        players.Sort(delegate (Player p1, Player p2)
        {
            if (p1.m_Points.Value < p2.m_Points.Value) return 1;
            if (p1.m_Points.Value == p2.m_Points.Value) return 0;
            else return -1;

        });
        int i = 0;
        while (i < players.Count)
        {
            m_PlayerResults[i].text = players[i].m_PlayerName.Value.ToString() + ": " + players[i].m_Kills.Value + " K/" + players[i].m_Deaths.Value + " D " + players[i].m_Points.Value + " PTS";
            i++;
        }
        while (i < m_GameManager.TOTAL_PLAYERS)
        {
            m_PlayerResults[i].enabled = false;
            i++;
        }


    }
    private void QuitAndReturnToMainMenu()
    {
        m_GameManager.m_LocalPlayer.DisconnectPlayerServerRpc(m_GameManager.m_LocalPlayer.GetComponent<NetworkObject>().OwnerClientId);

        if (m_NetworkManager.IsHost || m_NetworkManager.IsServer)
            m_NetworkManager.Shutdown();

        m_GameManager.ResetGame();

        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;
        virtualCam.LookAt = m_CameraStartingPosition;
        virtualCam.Follow = m_CameraStartingPosition;

        m_EndGameCanvas.SetActive(false);
        m_MainMenuCanvas.SetActive(true);
        m_MainMenu.SetActive(true);

    }

    public void UpdateLifeUI(int hitpoints)
    {
        switch (hitpoints)
        {
            case 0:
                m_HeartsUI[0].texture = m_Hearts[2].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 1:
                m_HeartsUI[0].texture = m_Hearts[1].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 2:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[2].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 3:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[1].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 4:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[0].texture;
                m_HeartsUI[2].texture = m_Hearts[2].texture;
                break;
            case 5:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[0].texture;
                m_HeartsUI[2].texture = m_Hearts[1].texture;
                break;
            case 6:
                m_HeartsUI[0].texture = m_Hearts[0].texture;
                m_HeartsUI[1].texture = m_Hearts[0].texture;
                m_HeartsUI[2].texture = m_Hearts[0].texture;
                break;

        }
    }

    #endregion




    #region Netcode Related Methods

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        ActivateLobbyCanvas();
        //ActivateInGameHUD();
    }

    private void StartClient()
    {
        var ip = m_InputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            m_Transport.SetConnectionData(ip, m_Port);
        }
        NetworkManager.Singleton.StartClient();
        //ActivateInGameHUD();
        ActivateLobbyCanvas();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateLobbyCanvas();
        //ActivateInGameHUD();
    }

    #endregion

}

