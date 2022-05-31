using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Cinemachine;

/// <summary>
/// Class containing all the logic behind the UI and graphical aspects of the game.
/// </summary>
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
    [SerializeField] private Button m_ButtonCustomize;

    //Es un unity assets muy util y chulo para elegir el color
    [SerializeField] private FlexibleColorPicker m_ColorPicker;
    private bool m_IsCustomizing = false;


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

        m_ButtonReady.onClick.AddListener(() =>
        {
            m_GameManager.m_LocalPlayer.SetPlayerReadyServerRpc(1);
            m_ButtonReady.gameObject.SetActive(false);
        });

        m_ButtonCustomize.onClick.AddListener(() =>
        {
            if (!m_IsCustomizing)
            {
                m_IsCustomizing = true;
                m_ButtonCustomize.GetComponentInChildren<Text>().text = "ACCEPT";
                m_ColorPicker.gameObject.SetActive(true);
            }
            else
            {
                m_IsCustomizing = false;
                m_ButtonCustomize.GetComponentInChildren<Text>().text = "CUSTOMIZE";
                //Cambia el color al jugador con server RPC
                m_GameManager.m_LocalPlayer.ChangePlayerColorServerRpc(m_ColorPicker.color);
                m_ColorPicker.gameObject.SetActive(false);

            }
        });
    }

    #endregion

    #region UI Related Methods
    //Update text methods
    public void UpdatePlayerNumber(int num)
    {
        m_NumPlayers.text = num.ToString() + "/" + m_GameManager.TOTAL_PLAYERS + " PLAYERS";
        m_NumPlayersLobby.text = num.ToString() + "/" + m_GameManager.TOTAL_PLAYERS + " PLAYERS";
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
        //Se transforma el tiempo a segundos y minutos
        //para hacerlo más agradable y entendible
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
        else if (currentTime == -1)
            m_CountdownText.enabled = false;
        m_CountdownText.text = "STARTING IN " + currentTime;
    }
    //Menu activation functions
    public void ActivateDeathCanvas()
    {
        m_DeathCanvas.SetActive(true);
        foreach (var h in m_HeartsUI)
        {
            h.enabled = false;
        }
        StartCoroutine(DeactivateDeathCanvas(2.5f));
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
    public void ActivateMainMenu()
    {

        if (m_InputFieldName.text != "")//Si se ha introducido el nombre te deja pasar
        {
            m_LoginMenu.SetActive(false);
            m_MainMenu.SetActive(true);
            m_InGameHUD.SetActive(false);
            m_PlayerNickName.text = "Welcome " + m_InputFieldName.text;
            m_InputFieldIP.placeholder.GetComponent<Text>().text = m_Transport.ConnectionData.Address;
        }
        else //Si no, te avisa de que debes introducir un nombre
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
        //Antes de activarlo prepara el ranking de jugadores
        SetupRanking();
        m_InGameHUD.SetActive(false);
        m_EndGameCanvas.SetActive(true);
    }
    /// <summary>
    /// Corountine method for changing the input name text color when on error.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <returns></returns>
    private IEnumerator ChangeTextColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        m_InputFieldName.placeholder.GetComponent<Text>().color = Color.black;
    }
    /// <summary>
    /// Coroutine method that deactivates the death canvas that appears when the player dies after a time
    /// </summary>
    /// <param name="waitTime"></param>
    /// <returns></returns>
    private IEnumerator DeactivateDeathCanvas(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        foreach (var h in m_HeartsUI)
        {
            h.enabled = true;
        }
        m_DeathCanvas.SetActive(false);

    }
    /// <summary>
    /// Generic conroutine method for disabling texts after a specific time has passed.
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="canvas"></param>
    /// <returns></returns>
    private IEnumerator DeactivateText(float waitTime, Text canvas)
    {
        yield return new WaitForSeconds(waitTime);
        canvas.enabled = false;
    }

    /// <summary>
    /// Setups the ranking in the end game canvas.
    /// </summary>
    private void SetupRanking()
    {
        List<Player> players = new List<Player>();
        Player[] playerArr = FindObjectsOfType<Player>();
        foreach (var p in playerArr)
        {
            if (p)
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
            m_PlayerResults[i].enabled = true;
            m_PlayerResults[i].text = players[i].m_PlayerName.Value.ToString() + ": " + players[i].m_Kills.Value + " K/" + players[i].m_Deaths.Value + " D " + players[i].m_Points.Value + " PTS";
            i++;
        }



    }
    /// <summary>
    /// Returns the player to the main menu after the match has ended and disconnects him from the network
    /// </summary>
    private void QuitAndReturnToMainMenu()
    {

        if (m_NetworkManager.IsHost || m_NetworkManager.IsServer)
          m_NetworkManager.Shutdown();

        RestartMainMenu();


        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;
        virtualCam.LookAt = m_CameraStartingPosition;
        virtualCam.Follow = m_CameraStartingPosition;

        m_GameManager.m_LocalPlayer.DisconnectPlayerServerRpc(m_GameManager.m_LocalPlayer.GetComponent<NetworkObject>().OwnerClientId);

    }
    public void RestartMainMenu()
    {
        m_LobbyCanvas.SetActive(false);
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
        //NetworkManager.Singleton.ConnectionApprovalCallback += m_GameManager.ApprovalCheck;
        NetworkManager.Singleton.StartHost();
        ActivateLobbyCanvas();
    }

    private void StartClient()
    {
        var ip = m_InputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            m_Transport.SetConnectionData(ip, m_Port);
        }
        NetworkManager.Singleton.StartClient();
        ActivateLobbyCanvas();
    }

    private void StartServer()
    {
        //NetworkManager.Singleton.ConnectionApprovalCallback += m_GameManager.ApprovalCheck;
        NetworkManager.Singleton.StartServer();
        ActivateLobbyCanvas();
    }

    #endregion

}

