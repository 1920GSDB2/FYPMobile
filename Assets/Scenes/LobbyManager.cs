﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviour
{
    #region Enum
    public enum FunctionPanelType
    {
        StartPanel,
        LobbyPanel,
        InformationPanel,
        GameRoomPanel
    }
    public enum UsingPanelType
    {
        MatchModePanel,
        CreateCustomPanel,
        JoinCustomPanel
    }
    #endregion

    #region Const Variable
    private const string Match = "MATCH";
    private const string Custom = "CUSTOM";
    #endregion

    #region Variable
    public static LobbyManager instance;
    public static PhotonView PhotonView;
    public static UsingPanelType currentModeType = UsingPanelType.MatchModePanel;
    public static List<LobbyRoom> LobbyRooms { get; private set; } = new List<LobbyRoom>();
    public static List<LobbyPlayer> RoomPlayersList { get; private set; } = new List<LobbyPlayer>();
    string joinRoomId;
    Coroutine matchTimerCoroutine;
    #endregion

    #region Buttons
    [Header("Function Buttons")]
    public Button StartButton;
    public Button LobbyButton;
    public Button InformationButton;

    [Header("Game Mode Buttons")]
    public Button MatchButton;
    public Button CreateCustomButton;
    public Button JoinCustomButton;

    [Header("Game Buttons")]
    public Button OkayButton;
    public Button BackLobbyButton;
    public Button RoomStartButton;
    public Button RoomBackLoobyButton;

    [Header("Room Password Buttons")]
    public Button JoinButton;
    public Button CancelJoinButton;

    [Header("Useful Buttons")]
    public Button CancelMatchButton;
    #endregion

    #region Panels
    [Header("Function Panels")]
    public GameObject StartPanel;
    public GameObject LobbyPanel;
    public GameObject InformationPanel;
    public GameObject GameRoomPanel;
    static GameObject[] FunctionPanelsArr = new GameObject[4];

    [Header("Using Panels")]
    public GameObject MatchModePanel;
    public GameObject CreateCustomPanel;
    public GameObject JoinCustomPanel;
    static GameObject[] UsingPanelsArr = new GameObject[3];

    [Header("Room Panels")]
    public GameObject RoomListPanel;
    public GameObject RoomInfoPanel;
    public GameObject PlayerListPanel;

    [Header("Useful Panels")]
    public GameObject RoomPasswordPanel;
    public GameObject MatchStatusPanel;
    public MatchAcceptance MatchAcceptPanel;
    #endregion

    #region Text
    [Header("Text")]
    public TMP_InputField CreateRoomName;
    public TMP_InputField CreateRoomPassord;
    public TMP_InputField PassordInputField;
    public TextMeshProUGUI CurrentRoomName;
    public TextMeshProUGUI MatchTimer;
    #endregion

    #region Instantiate Object
    [Header("Instantiate Object")]
    public GameObject LobbyPlayer;
    public GameObject LobbyRoomPrefab;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Photon Connect
        //connect to server
        if (!PhotonNetwork.connected)
        {
            print("Connecting to server..");
            PhotonNetwork.ConnectUsingSettings("0.0.0");
        }
        #endregion

        #region Variable Initialize
        if (instance == null)
        {
            instance = this;
        }
        PhotonView = GetComponent<PhotonView>();

        //Function Panels Manage
        FunctionPanelsArr[0] = StartPanel;
        FunctionPanelsArr[1] = LobbyPanel;
        FunctionPanelsArr[2] = InformationPanel;
        FunctionPanelsArr[3] = GameRoomPanel;

        //Using Panels Manage
        UsingPanelsArr[0] = MatchModePanel;
        UsingPanelsArr[1] = CreateCustomPanel;
        UsingPanelsArr[2] = JoinCustomPanel;

        MatchStatusPanel.SetActive(false);
        #endregion

        #region AddListener
        //Add Listener of Buttons
        StartButton.onClick.AddListener(delegate { SwitchFunctionPanel(FunctionPanelType.StartPanel); SwitchUsingPanel(UsingPanelType.MatchModePanel); });
        LobbyButton.onClick.AddListener(delegate { SwitchFunctionPanel(FunctionPanelType.LobbyPanel); });
        InformationButton.onClick.AddListener(delegate { SwitchFunctionPanel(FunctionPanelType.InformationPanel); });
        MatchButton.onClick.AddListener(delegate { SwitchUsingPanel(UsingPanelType.MatchModePanel); });
        CreateCustomButton.onClick.AddListener(delegate { SwitchUsingPanel(UsingPanelType.CreateCustomPanel); });
        JoinCustomButton.onClick.AddListener(delegate { SwitchUsingPanel(UsingPanelType.JoinCustomPanel); });
        OkayButton.onClick.AddListener(delegate { EnterRoom(); });
        BackLobbyButton.onClick.AddListener(delegate { SwitchFunctionPanel(FunctionPanelType.LobbyPanel); });
        RoomStartButton.onClick.AddListener(delegate { StartGame(); });
        RoomBackLoobyButton.onClick.AddListener(delegate { OnClickLeftRoom(); });
        JoinButton.onClick.AddListener(delegate { JoinPrivateRoom(); });
        CancelJoinButton.onClick.AddListener(delegate { CancelJoinRoom(); });
        CancelMatchButton.onClick.AddListener(delegate { OnClickLeftRoom(); });
        #endregion

        SwitchFunctionPanel(FunctionPanelType.LobbyPanel);
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.isMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PhotonView.RPC("test", PhotonTargets.Others, "others");
                PhotonView.RPC("test", PhotonTargets.All, "All");
                PhotonView.RPC("test", PhotonTargets.OthersBuffered, "OthersBuffered");
            }
        }
    }

    #region Switch Panels
    public void SwitchFunctionPanel(FunctionPanelType _type)
    {
        
        foreach (GameObject panel in FunctionPanelsArr)
        {
            panel.SetActive(false);
        }
        switch (_type)
        {
            case FunctionPanelType.StartPanel:
                if (PhotonNetwork.inRoom)
                    FunctionPanelsArr[3].SetActive(true);
                else
                    FunctionPanelsArr[0].SetActive(true);
                break;
            case FunctionPanelType.LobbyPanel:
                FunctionPanelsArr[1].SetActive(true);
                break;
            case FunctionPanelType.InformationPanel:
                FunctionPanelsArr[2].SetActive(true);
                break;
            case FunctionPanelType.GameRoomPanel:
                FunctionPanelsArr[3].SetActive(true);
                break;
        }
    }

    public void SwitchUsingPanel(UsingPanelType _type)
    {
        if (PhotonNetwork.isMasterClient) return;
        RoomPasswordPanel.SetActive(false);
        currentModeType = _type;
        foreach(GameObject panel in UsingPanelsArr)
        {
            panel.SetActive(false);
        }
        switch (_type)
        {
            case UsingPanelType.MatchModePanel:
                UsingPanelsArr[0].SetActive(true);
                break;
            case UsingPanelType.CreateCustomPanel:
                UsingPanelsArr[1].SetActive(true);                
                break;
            case UsingPanelType.JoinCustomPanel:
                UsingPanelsArr[2].SetActive(true);
                break;

        }
    }
    #endregion

    #region Connect Lobby
    //When Player connect to Master called by photon
    private void OnConnectedToMaster()
    {
        print("Connected to master.");
        PhotonNetwork.automaticallySyncScene = false;
        PhotonNetwork.playerName = "player#" + Random.Range(1000, 9999); ;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }
    //When Player join Lobby called by photon
    private void OnJoinedLobby()
    {
        print("Joined lobby.");

      //  if (!PhotonNetwork.inRoom)
          //  MainCanvasManger.Instance.LobbyCanvas.transform.SetAsLastSibling();
    }
    #endregion

    #region Create Room
    //Create A Custom room
    private void CreateCustomRoom() {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        roomOptions.MaxPlayers = Main.GameManager.Instance.MaxRoomPlayer;
        roomOptions.CustomRoomProperties = new Hashtable()
        {
            {"NAME", CreateRoomName.text},
            {"MATCHMODE", Custom }
        };

        roomOptions.CustomRoomPropertiesForLobby = new string[] { "NAME" };
        List<string> CustomRoomPropertiesForLobby = new List<string>();
        CustomRoomPropertiesForLobby.Add("NAME");
        CustomRoomPropertiesForLobby.Add("MATCHMODE");
        if (!CreateRoomPassord.text.Equals(""))
        {
            roomOptions.CustomRoomProperties.Add("PASSWORD", CreateRoomPassord.text);
            CustomRoomPropertiesForLobby.Add("PASSWORD");
        }
        roomOptions.CustomRoomPropertiesForLobby = CustomRoomPropertiesForLobby.ToArray();
        
        if (PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default))
        {
            print("create room successfully");
            CurrentRoomName.text = CreateRoomName.text;
        }
        else
        {
            print("create room failed");
        }
        CreateRoomName.text = "";
        CreateRoomPassord.text = "";
    }

    //Called by photon to check if fail to create room
    private void OnPhotonCreateRoomFailed(object[] codeAndMessage)
    {
        print("create room failed: " + codeAndMessage[1]);
    }
    //Called by photon to check if successfully to create room
    private void OnCreateRoom()
    {
        print("Room Create successfully.");
    }

    //When receive a room that mean someone create a room. called by photon
    private void OnReceivedRoomListUpdate()
    {
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        foreach (RoomInfo room in rooms)
        {
            if (room.CustomProperties["MATCHMODE"].Equals(Custom))
            {
                RoomReceived(room);
            }
        }
        RemoveOldRooms();
    }
    
    private void RoomReceived(RoomInfo room)
    {
        int index = LobbyRooms.FindIndex(x => x.RoomName.text == room.Name);//Find the room name equal to one of element in LobbyRoomButtons List
                                                                                   //if find succesfully will return the index of position in List
        if (index == -1)//cannot find the element
        {
            if (room.IsVisible && room.PlayerCount < room.MaxPlayers)
            {
                GameObject LobbyRoomObj = Instantiate(LobbyRoomPrefab);// if cannnot find the Room,add new Room
                LobbyRoomObj.transform.SetParent(RoomListPanel.transform, false);

                LobbyRoom lobbyRoom = LobbyRoomObj.GetComponent<LobbyRoom>();
                LobbyRooms.Add(lobbyRoom);
                index = (LobbyRooms.Count - 1);
            }
        }
        if (index != -1) 
        {
            LobbyRoom lobbyRoom = LobbyRooms[index];
            lobbyRoom.SetRoom(room.Name, room.CustomProperties["NAME"].ToString(), room.PlayerCount);
            lobbyRoom.Updated = true;
           
        }
    }
    #endregion

    #region Join Room
    //Button Click a Room to Join
    public void EnterRoom()
    {
        switch (currentModeType)
        {
            case UsingPanelType.MatchModePanel:
                //...//
                MatchRoom();
                break;
            case UsingPanelType.CreateCustomPanel:
                SwitchFunctionPanel(FunctionPanelType.GameRoomPanel);
                CreateCustomRoom();
                //Pass Room Data To Create Room//
                break;
            case UsingPanelType.JoinCustomPanel:
                SwitchFunctionPanel(FunctionPanelType.GameRoomPanel);
                //Get Room Data To Join Room//
                break;
        }
    }
    //Enter room
    private void EnterRoom(string roomId)
    {
        if (PhotonNetwork.JoinRoom(roomId))
        {
            print("Join room: " + roomId);
        }
        else
        {
            print("Join room failed.");
        }
        
    }
    //Normal Join Method, It is called by LoobyRoom.cs
    public void JoinRoom(string roomId)
    {
        foreach(RoomInfo room in PhotonNetwork.GetRoomList())
        {
            if (room.Name.Equals(roomId))
            {
                if (!room.CustomProperties.ContainsKey("PASSWORD"))
                {
                    EnterRoom(roomId);
                    joinRoomId = "";
                }
                else
                {
                    RoomPasswordPanel.SetActive(true);
                    joinRoomId = roomId;
                }
                break;
            }
        }
    }

    //Join Private Room and Verify Password, It is called by JoinButton
    public void JoinPrivateRoom()
    {
        print(joinRoomId);
        foreach (RoomInfo room in PhotonNetwork.GetRoomList())
        {
            if (room.Name.Equals(joinRoomId))
            {
                if (room.CustomProperties["PASSWORD"].Equals(PassordInputField.text))
                {
                    EnterRoom();
                    joinRoomId = "";
                    break;
                }
                else
                {
                    print("Wrong Pw");
                }
                PassordInputField.text = "";
                RoomPasswordPanel.SetActive(false);
            }
        }
    }

    //Cancel Join Private Room and Verify Password, It is called by CancelJoinButton
    public void CancelJoinRoom()
    {
        PassordInputField.text = "";
        RoomPasswordPanel.SetActive(false);
        joinRoomId = "";
    }

    //Set List of Room Player 
    private void SetRoomPlayerList(PhotonPlayer photonPlayer)
    {
        if (photonPlayer == null)
            return;

        PlayerLeftRoom(photonPlayer);
        GameObject playerListingObj = Instantiate(LobbyPlayer);
        playerListingObj.transform.SetParent(PlayerListPanel.transform, false);

        LobbyPlayer playerListing = playerListingObj.GetComponent<LobbyPlayer>();
        playerListing.SetPhotonPlayer(photonPlayer);//set information of player

        RoomPlayersList.Add(playerListing);
    }

    private void OnMatchJoinRoom()
    {
        if (PhotonNetwork.playerList.Length >= Main.GameManager.Instance.MaxRoomPlayer)
        {
            MatchAcceptPanel.InitialPanel();
        }
    }

    //Called by photon, When player join room
    private void OnJoinedRoom()
    {

        if (PhotonNetwork.room.CustomProperties["MATCHMODE"].Equals(Custom))
        {
            SwitchFunctionPanel(FunctionPanelType.GameRoomPanel);
            CurrentRoomName.text = PhotonNetwork.room.CustomProperties["NAME"].ToString();
            PhotonPlayer[] photonPlayers = PhotonNetwork.playerList;
            for (int i = 0; i < photonPlayers.Length; i++)
            {
                SetRoomPlayerList(photonPlayers[i]);
            }
        }
        else if (PhotonNetwork.room.CustomProperties["MATCHMODE"].Equals(Match))
        {
            MatchStatusPanel.SetActive(true);
            matchTimerCoroutine = StartCoroutine(FindGame());
            OnMatchJoinRoom();
        }
        //PhotonView.RPC("RPC_PlayerJoinRoom", PhotonTargets.OthersBuffered);
    }

    //Called by photon
    private void OnPhotonPlayerConnected(PhotonPlayer photonPlayer)
    {
        SetRoomPlayerList(photonPlayer);

        if (PhotonNetwork.room.CustomProperties["MATCHMODE"].Equals(Match))
            OnMatchJoinRoom();
    }
    #endregion

    #region Match Room
    private void MatchRoom()
    {
        Hashtable CustomRoomProperties = new Hashtable() { { "MATCHMODE", Match } };
        //JoinMatchingRoom(CustomRoomProperties, GameManager.MaxRoomPlayer);
        PhotonNetwork.JoinRandomRoom(CustomRoomProperties, Main.GameManager.Instance.MaxRoomPlayer);
    }
    void JoinMatchingRoom(Hashtable CustomRoomProperties, byte MaxRoomPlayer)
    {
        foreach (RoomInfo room in PhotonNetwork.GetRoomList())
        {
            Hashtable roomCustomProperties = room.CustomProperties;
            foreach (object key in CustomRoomProperties.Keys)
            {
                Debug.Log("Match Key: " + key);
                Debug.Log("Match Value: '" + CustomRoomProperties[key].ToString() + "'");
                Debug.Log("Room Key Value: '" + roomCustomProperties[key].ToString() + "'");
                Debug.Log("Is Contain Key: " + roomCustomProperties.ContainsKey(key));
                Debug.Log("Is Contain Value: " + roomCustomProperties[key].ToString().Equals(CustomRoomProperties[key].ToString()));

                if (roomCustomProperties.ContainsKey(key) &&
                    roomCustomProperties[key].ToString().Equals(CustomRoomProperties[key].ToString()) &&
                    room.MaxPlayers == MaxRoomPlayer)
                {
                    EnterRoom(room.Name);
                    return;
                }
            }
        }
        //OnMatchingJoinFailed();
    }
    void OnPhotonRandomJoinFailed()
    {
        print("Join Random Fail");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        roomOptions.MaxPlayers = Main.GameManager.Instance.MaxRoomPlayer;
        roomOptions.CustomRoomProperties = new Hashtable() { { "MATCHMODE", Match } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "MATCHMODE" };
        PhotonNetwork.CreateRoom(null, roomOptions, null);
    }
    #endregion

    #region Leave Room
    public void OnClickLeftRoom()
    {
        print("Leave room");
        
        if (PhotonNetwork.isMasterClient)
        {
            //print("i am master player left room");
            if (PhotonNetwork.playerList.Length > 1)
                PhotonNetwork.SetMasterClient(PhotonNetwork.masterClient.GetNext());
            //PhotonView.RPC("RPC_MasterLeftRoom", PhotonTargets.Others);
        }
        if (PhotonNetwork.room.CustomProperties["MATCHMODE"].Equals(Custom))
        {
            while (RoomPlayersList.Count != 0)
            {
                Destroy(RoomPlayersList[0].gameObject);
                RoomPlayersList.RemoveAt(0);
            }
        }
        else if (PhotonNetwork.room.CustomProperties["MATCHMODE"].Equals(Match))
        {
            StopCoroutine(matchTimerCoroutine);
            MatchStatusPanel.SetActive(false);
        }

        if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
        SwitchFunctionPanel(FunctionPanelType.LobbyPanel);
    }
    #endregion

    #region Start Game

    public void StartGame()
    {
        
        
        if (CheckStartGame())
        {
            //Enter the Game Room
            Debug.Log("Start Game");
        }
    }
    private bool CheckStartGame()
    {
        PhotonPlayer[] roomPlayers = PhotonNetwork.playerList;

        if (roomPlayers.Length != Main.GameManager.Instance.MaxRoomPlayer) return false;
        foreach(PhotonPlayer roomPlayer in roomPlayers)
        {
            if (!roomPlayer.CustomProperties["ReadyForStart"].Equals("Ready"))
                return false;
        }
        //bool isAllReady = true;
        //foreach (LobbyPlayer player in RoomPlayersList)
        //{
        //    if (!player.IsReady)
        //    {
        //        isAllReady = false;
        //        break;
        //    }
        //}
        return true;
    }
    #endregion

    #region Remove Outdated Value
    //Remove Player who does not Exist in the Room;
    private void PlayerLeftRoom(PhotonPlayer photonPlayer)
    {
        int index = RoomPlayersList.FindIndex(a => a.PhotonPlayer == photonPlayer);
        if (index != -1)
        {
            Destroy(RoomPlayersList[index].gameObject);
            RoomPlayersList.RemoveAt(index);
        }
    }
    
    //Called by photon, When player disconnected the room
    private void OnPhotonPlayerDisconnected(PhotonPlayer photonPlayer)
    {
        PlayerLeftRoom(photonPlayer);
        foreach(LobbyPlayer player in RoomPlayersList)
        {
            player.SetKickButton();
        }
    }

    //Remove rooms which do not existed in photon server room list
    private void RemoveOldRooms()
    {
        List<LobbyRoom> removeRooms = new List<LobbyRoom>();

        foreach (LobbyRoom roomListing in LobbyRooms)
        {
            if (!roomListing.Updated)
                removeRooms.Add(roomListing);
            else
                roomListing.Updated = false;
        }

        foreach (LobbyRoom roomListing in removeRooms)
        {
            GameObject roomListingObj = roomListing.gameObject;
            LobbyRooms.Remove(roomListing);
            Destroy(roomListingObj);

        }
    }
    #endregion

    #region PunRPC
    [PunRPC]
    void RPC_PlayerJoinRoom()
    {

    }
    [PunRPC]
    //"Was Left" Room (Kicked By Room Master)
    void RPC_OnLeftRoom()
    {
        print("Left Room");
        OnClickLeftRoom();
    }

    [PunRPC]
    //Player Ready Click Ready
    void RPC_Ready(PhotonPlayer photonPlayer)
    {
        print("Ready");
        int index = RoomPlayersList.FindIndex(a => a.PhotonPlayer == photonPlayer);
        if (index != -1)
        {
           RoomPlayersList[index].Ready();
        }
    }
    #endregion

    IEnumerator FindGame()
    {
        int countUpTime = 0;
        while (true)
        {
            if (countUpTime % 60 < 10)
            {
                MatchTimer.text = countUpTime / 60 + " : 0" + (countUpTime % 60).ToString();
            }
            else
            {
                MatchTimer.text = countUpTime / 60 + " : " + (countUpTime % 60).ToString();
            }
            yield return new WaitForSeconds(1);
            countUpTime++;
        }
    }
}
