﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace TFT
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        public static PhotonView PhotonView;
        public int playerId, posId, battlePosId;                 //Id for Networking
        private int focusPlayerId;
        public int FocusPlayerId
        {
            get { return focusPlayerId; }
            set
            {
                focusPlayerId = value;
                BuffList.Instance.ClearBuff();
                BuffList.Instance.HeroBuffList = PlayerHeroes[FocusPlayerId].BuffList;
            }
        }
        [SerializeField]
        public Opponent opponent;                   //Player's Opponent

        public int[] PlayerPosition;                //Player's Position Represent to PlayerArenas
        public RankManager RankManager;
        public PlayerHero[] PlayerHeroes;           //Players' Heroes List
        public GameObject[] PlayerArenas;           //PlayerArenas Location (The Data are hard coded in scene)
        public OpponentManager[] OpponentManagers;  //Player's Opponent Data
        public Camera[] Cameras;                    //Cameras of Focusing Game Arena (The Data are hard coded in scene)
        public GridMap map;
        public List<Character> selfGameBoardHero = new List<Character>();
        public List<Character> battleGameBoardHero;
        TFTPlayerCharacter playerCharacter;
        public bool isHomeTeam { get; private set; }
        int waveFinishResponse;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            RankManager = RankManager.Instance;
            PhotonNetworkSetup();

            FocusPlayerId = playerId;
        }

        void Update()
        {

            #region PhotonNetwork Debugging
            if (Input.GetKeyDown(KeyCode.L))
            {
                PhotonView.RPC("RPC_resetResponse", PhotonTargets.All);
                PhotonView.RPC("MonsterBattle", PhotonTargets.All);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {

                PhotonView.RPC("RPC_resetResponse", PhotonTargets.All);
                PhotonView.RPC("RPC_Battle", PhotonTargets.All, 0, 1);
                //   PhotonView.RPC("RPC_Test", PhotonTargets.All);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //  Hero monster = (PhotonNetwork.Instantiate(Path.Combine("Prefabs", "God of Wizard"), Vector3.zero, Quaternion.identity, 0)).GetComponent<Hero>();
                PhotonView.RPC("RPC_Test", PhotonTargets.Others);
            }
            #endregion

        }

        #region Photon Network Setup
        /// <summary>
        /// Photon Connection Setup, It will be called by Start
        /// </summary>
        private void PhotonNetworkSetup()
        {
            PhotonView = GetComponent<PhotonView>();

            PlayerHeroes = new PlayerHero[PhotonNetwork.playerList.Length];

            if (PhotonNetwork.isMasterClient)
                SetupNetworkPlayer();
        }

        /// <summary>
        /// Rearrange player gameboard position, it is implemented by master client
        /// </summary>
        /// <returns></returns>
        private void SetupNetworkPlayer()
        {
            #region Setup All Player ID (Follow master client arrangement)
            for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
            {
                PhotonView.RPC("RPC_SetPlayerId", PhotonNetwork.playerList[i], i);
            }
            #endregion

            //Set Player Random Position
            PhotonView.RPC("RPC_SetupPlayerPosition", PhotonTargets.All, GetRearrangeData(PhotonNetwork.playerList.Length));
        }

        #endregion

        #region Sync Hero Method
        /// <summary>
        /// Sync Player Hero to Other Players
        /// </summary>
        /// <param name="_syncHero"></param>
        /// <param name="_syncHeroMethod"></param>
        public void SyncPlayerHero(NetworkHero _syncHero, SyncHeroMethod _syncHeroMethod)
        {
            PhotonView.RPC("RPC_SyncPlayerHeroes", PhotonTargets.All, posId,
                playerId, _syncHero.name, _syncHero.position, _syncHero.HeroLevel, _syncHeroMethod);
        }

        /// <summary>
        /// Sync Player Hero Position to Other Players
        /// </summary>
        /// <param name="_hero"></param>
        /// <param name="_syncMoveHero"></param>
        public void SyncPlayerHeroPlace(Hero _hero, SyncMoveHero _syncMoveHero)
        {
            switch (_syncMoveHero)
            {
                case SyncMoveHero.AddGameboard:
                    selfGameBoardHero.Add(_hero);
                    break;
                case SyncMoveHero.RemoveGameboard:
                    selfGameBoardHero.Remove(_hero);
                    break;
            }
            _hero.networkPlaceId = _hero.HeroPlace.PlaceId;
            _hero.GetComponent<PhotonView>().RPC("syncNetworkPlaceId", PhotonTargets.All, _hero.networkPlaceId);
            PhotonView.RPC("RPC_SyncPlayerHeroPlace", PhotonTargets.All, posId,
                playerId, _hero.name, _hero.LastHeroPlace.PlaceId, _hero.HeroLevel, _hero.HeroPlace.PlaceId, _syncMoveHero);
        }
        #endregion

        #region Get Hero Place
        /// <summary>
        /// Get Gameboard Hero place by Id
        /// </summary>
        /// <param name="_posId"></param>
        /// <param name="_placeId"></param>
        /// <returns></returns>
        public HeroPlace GetGameboardHeroPlace(int _posId, int _placeId)
        {
            if (posId == _posId)
            {
                return GameManager.Instance.SelfPlayerArena.SelfArena.GameBoard.GetChild(_placeId).GetComponent<HeroPlace>();
            }
            else
            {
                return PlayerArenas[_posId].GetComponent<PlayerArena>().SelfArena.GameBoard.GetChild(_placeId).GetComponent<HeroPlace>();
            }
        }

        /// <summary>
        /// Get HeroList Hero Place by Id
        /// </summary>
        /// <param name="posId"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public HeroPlace GetHeroListHeroPlace(int posId, int placeId)
        {
            if (this.posId == posId)
                return GameManager.Instance.SelfPlayerArena.SelfArena.HeroList.GetChild(placeId).GetComponent<HeroPlace>();
            else
               return PlayerArenas[posId].GetComponent<PlayerArena>().SelfArena.HeroList.GetChild(placeId).GetComponent<HeroPlace>();
            //   return PlayerArenas[posId].GetComponent<PlayerArena>().EnemyArena.HeroList.GetChild(placeId).GetComponent<HeroPlace>();
        }

        /// <summary>
        /// Get Player's Hero Place by Id
        /// </summary>
        /// <param name="_posId"></param>
        /// <param name="_placeId"></param>
        /// <param name="_isEnemy"></param>
        /// <returns></returns>
        public HeroPlace GetMyGameBoardEnemyHeroPlace(int _posId, int _placeId)
        {
          //  if (this.posId == _posId)
                return PlayerArenas[_posId].GetComponent<PlayerArena>().EnemyArena.GameBoard.GetChild(_placeId).GetComponent<HeroPlace>();
           // else
               // return PlayerArenas[_posId].GetComponent<PlayerArena>().SelfArena.GameBoard.GetChild(_placeId).GetComponent<HeroPlace>();
        }
        public HeroPlace GetOpponentHeroPlace(int placeId, bool isEnemyPlace) {
            if (isEnemyPlace)
                return PlayerArenas[PlayerHeroes[opponent.opponentId].posId].GetComponent<PlayerArena>().EnemyArena.GameBoard.GetChild(placeId).GetComponent<HeroPlace>();
            else
                return PlayerArenas[PlayerHeroes[opponent.opponentId].posId].GetComponent<PlayerArena>().SelfArena.GameBoard.GetChild(placeId).GetComponent<HeroPlace>();
        }
        public HeroPlace GetBattleHeroPlace(int posId,int placeId, bool isEnemyPlace)
        {
            if (isEnemyPlace)
                return PlayerArenas[posId].GetComponent<PlayerArena>().EnemyArena.GameBoard.GetChild(placeId).GetComponent<HeroPlace>();
            else
                return PlayerArenas[posId].GetComponent<PlayerArena>().SelfArena.GameBoard.GetChild(placeId).GetComponent<HeroPlace>();
        }
        public Character GetBattleBoardHero(int posId, int placeId, bool isEnemyPlace)
        {
            if (isEnemyPlace)
                return PlayerArenas[posId].GetComponent<PlayerArena>().EnemyArena.GameBoard.GetChild(placeId).GetChild(0).GetComponent<Hero>();
            else
                return PlayerArenas[posId].GetComponent<PlayerArena>().SelfArena.GameBoard.GetChild(placeId).GetChild(0).GetComponent<Hero>();
        }
        public Character getRandomCharacter(bool isEnemy) {

            if (isEnemy)
            {
                int random = Random.Range(0, opponent.hero.Count);
                return opponent.hero[random];
            }
            else {
                int random = Random.Range(0, battleGameBoardHero.Count);
                return battleGameBoardHero[random];
            }

        }
        #endregion

        public Character getCloestEnemyTarget(bool isEnemy, Transform heroPos) {

            if (isEnemy)
            {
             //   Debug.Log("Get battleGame " + isEnemy);
                return calculateClosestDistance(opponent.hero, heroPos);
            }
            else
            {
                //Debug.Log("Get opponent " + isEnemy);            
                return calculateClosestDistance(battleGameBoardHero, heroPos);
            }
        }
        public Character getFurthestEnemyTarget(bool isEnemy, Transform heroPos)
        {

            if (isEnemy)
            {
                Debug.Log("Get battleGame " + isEnemy);
                return calculateClosestDistance(opponent.hero, heroPos);
            }
            else
            {
                Debug.Log("Get opponent " + isEnemy);
                return calculateClosestDistance(battleGameBoardHero, heroPos);
            }
        }
        public Character getEnemyIndexById(int placeId,bool isEnemy) {
            if (isEnemy){
                int index = selfGameBoardHero.FindIndex(x => x.networkPlaceId == placeId);
                if(index!=-1)
                return selfGameBoardHero[index];
            }
            else{
               int index = NetworkManager.Instance.opponent.hero.FindIndex(x => x.networkPlaceId == placeId);
                if (index != -1)
                  return opponent.hero[index];
            }
            return null;
        }
        Character calculateClosestDistance(List<Character> targetHeros, Transform heroPos) {
            Character[] hero = targetHeros.ToArray<Character>();

            if (hero.Length != 0) {
                float closestDis = Vector3.Distance(hero[0].transform.position, heroPos.position);
                Character closestHero = hero[0];
                for (int i = 1; i < hero.Length; i++) {
                    float dis = Vector3.Distance(hero[i].transform.position, heroPos.position);
                    if (dis < closestDis)
                        closestHero = hero[i];
                }
                return closestHero;
            }
            return null;
        }
        Character calculateFurthestDistance(List<Character> targetHeros, Transform heroPos)
        {
            Character[] hero = targetHeros.ToArray<Character>();

            if (hero.Length != 0)
            {
                float furthestDis = Vector3.Distance(hero[0].transform.position, heroPos.position);
                Character furthestHero = hero[0];
                for (int i = 1; i < hero.Length; i++)
                {
                    float dis = Vector3.Distance(hero[i].transform.position, heroPos.position);
                    if (dis > furthestDis)
                        furthestHero = hero[i];
                }
                return furthestHero;
            }
            return null;
        }
        public GameObject getCamera(int id) {
            if(isHomeTeam)
                return PlayerArenas[id].GetComponent<PlayerArena>().Camera;
            else
                return PlayerArenas[id].GetComponent<PlayerArena>().enemyCamera;
        }
        #region Set Player Opponent
        /// <summary>
        /// Match Player Opponent, Called by Master Client
        /// </summary>
        public void MatchPlayerOpponent()
        {
            //Get the surival players list
            int[] opponentResult = GetRearrangeData(PhotonNetwork.playerList.Length);
            PhotonView.RPC("RPC_SyncPlayersOpponent", PhotonTargets.All, opponentResult);
        }

        /// <summary>
        /// Setting Player Opponent, Called by PunRPC
        /// </summary>
        /// <param name="matchResult"></param>
        private void SetPlayerOpponent(int[] matchResult)
        {
            if (matchResult.Length % 2 != 0)
            {
                OpponentManagers = new OpponentManager[matchResult.Length / 2 + 1];
                OpponentManagers[OpponentManagers.Length - 1] = new OpponentManager(matchResult[matchResult.Length / 2], matchResult[0], true);
                if (matchResult[matchResult.Length / 2] == playerId)
                {
                    opponent = new Opponent(matchResult[0], true);
                }
            }
            else
            {
                OpponentManagers = new OpponentManager[matchResult.Length / 2];
            }

            for (int i = 0; i < matchResult.Length / 2; i++)
            {
                OpponentManagers[i] = new OpponentManager(matchResult[i], matchResult[matchResult.Length - 1 - i]);
                Debug.Log("Player " + matchResult[i] + " vs  Player" + matchResult[matchResult.Length - 1 - i]);
                if (matchResult[i] == playerId)
                {
                    opponent = new Opponent(matchResult[matchResult.Length - 1 - i]);
                }
                else if (matchResult[matchResult.Length - 1 - i] == playerId)
                {
                    opponent = new Opponent(matchResult[i]);
                }
            }
        }

        #endregion

        /// <summary>
        /// Rearrange integer array data
        /// </summary>
        /// <param name="_length"></param>
        /// <returns></returns>
        private int[] GetRearrangeData(int _length)
        {
            int[] data = new int[_length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
            }
            System.Random r = new System.Random(System.DateTime.Now.Millisecond);
            return data.OrderBy(x => r.Next()).ToArray();
        }


        #region Start Photon Method, Called by Master Client


        /// <summary>
        /// Setting Lobby PhotonPlayer Array Position
        /// </summary>
        /// <param name="_id"></param>
        [PunRPC]
        public void RPC_SetPlayerId(int _id)
        {
            playerId = _id;
        }

        /// <summary>
        /// Setting Lobby Player Arena Randam Positon
        /// </summary>
        /// <param name="_playerPosition"></param>
        [PunRPC]
        public void RPC_SetupPlayerPosition(int[] _playerPosition)
        {
            PlayerPosition = _playerPosition;
            for (int i = 0; i < _playerPosition.Length; i++)
            {
                RankManager.PlayersName.Add(PhotonNetwork.playerList[i].NickName);
                PlayerHeroes[i] = new PlayerHero();
                if (_playerPosition[i] == playerId)
                {
                    GameManager.Instance.SelfPlayerArena = PlayerArenas[i].GetComponent<PlayerArena>();
                    map.setPlayerArena(GameManager.Instance.SelfPlayerArena);
                    posId = i;
                    PlayerHeroes[playerId] = new PlayerHero
                    {
                        posId = posId,
                        player = PhotonNetwork.player
                    };

                    //Cameras[posId].enabled = true;

                    GameManager.Instance.SelfPlayerArena.Camera.SetActive(true);
                    GameManager.Instance.MainCamera = GameManager.Instance.SelfPlayerArena.Camera.GetComponent<Camera>();

                    PhotonView.RPC("RPC_SyncPlayerInformation", PhotonTargets.All, playerId, posId, PhotonNetwork.player);
                    playerCharacter = PhotonNetwork.Instantiate(Path.Combine("otherPrefabs", "LifeStone"), Vector3.zero, Quaternion.identity, 0).GetComponent<TFTPlayerCharacter>();
                    playerCharacter.GetComponent<PhotonView>().RPC("RPC_SyncPlayerCharacterPosition", PhotonTargets.All, posId);
                   // createEquipmentBoard();
;                    //PlayerHeroes[playerId] = PlayerHero;
                }
            }
            RankManager.PlayerCollectionSetup();
        }
        void createEquipmentBoard() {
            UnityEngine.Object pPrefab = Resources.Load("otherPrefabs/EquipmentBoard");
            GameObject gameObject = Instantiate(pPrefab, transform.position, transform.rotation) as GameObject;
            gameObject.transform.parent = PlayerArenas[posId].GetComponent<PlayerArena>().equipmentBoard;
            gameObject.transform.localPosition = Vector3.zero;
        }
        [PunRPC]
        public void RPC_SyncPlayerInformation(int SyncPlayerId, int SyncPosId, PhotonPlayer player) {
            //  Debug.Log("SYNC inofor player id " + SyncPlayerId + "  POsid " + SyncPosId);
            PlayerHeroes[SyncPlayerId].setPersonalInformation(SyncPosId, player);
        }

        #endregion

        /// <summary>
        /// Setting All Players Opponent
        /// </summary>
        /// <param name="_matchResult"></param>
        [PunRPC]
        public void RPC_SyncPlayersOpponent(int[] _matchResult)
        {
            SetPlayerOpponent(_matchResult);
        }

        #region Sync Player Hero
        /// <summary>
        /// Setting all player's PlayerHeroes[] and remote player's GameArena Hero position, when player put or take hero form gameboard and herolist
        /// </summary>
        /// <param name="_posId"></param>
        /// <param name="_playerId"></param>
        /// <param name="_name"></param>
        /// <param name="_heroPos"></param>
        /// <param name="_heroLevel"></param>
        /// <param name="_newPos"></param>
        [PunRPC]
        public void RPC_SyncPlayerHeroPlace(int _posId, int _playerId, string _name, int _heroPos, HeroLevel _heroLevel, int _newPos, SyncMoveHero _syncMoveHero)
        {
            Debug.Log("Sync Move Hero Method: " + _syncMoveHero.ToString());
            List<NetworkHero> ChangedHero = PlayerHeroes[_playerId].UsableHeroes;

            // GamePlace enemyArena = PlayerArenas[_posId].GetComponent<PlayerArena>().EnemyArena;
            GamePlace enemyArena = PlayerArenas[_posId].GetComponent<PlayerArena>().SelfArena;
            Debug.Log("Player [" + _playerId + "] move the hero [" + _name + "] to place [" + _newPos + "]");
            //The loop is used for finding the network hero which satisfied the passing value.
            for (int i = 0; i < ChangedHero.Count; i++)
            {
                //Debug.Log("Hero: "+ _name + ", id: "+ i);
                //Debug.Log("name: " + ChangedHero[i].name.Equals(_name));
                //Debug.Log("position: " + (ChangedHero[i].position == _heroPos)+ " " + ChangedHero[i].position + ", " + _heroPos);
                //Debug.Log("HeroLevel: " + (ChangedHero[i].HeroLevel == _heroLevel));

                if (ChangedHero[i].name.Equals(_name) &&
                    ChangedHero[i].position == _heroPos &&
                    ChangedHero[i].HeroLevel == _heroLevel)
                {
                    int lastPos = ChangedHero[i].position;
                    switch (_syncMoveHero)
                    {
                        case SyncMoveHero.AddGameboard:
                            ChangedHero[i].position = _newPos;
                            PlayerHeroes[_playerId].GameboardAddHero(ChangedHero[i]);
                            //Remote Player Game Arena Update
                            if (_playerId != playerId)
                            {
                                //Check Selected Place Whether Null to Prevent Null Reference Exception
                                 if (enemyArena.HeroList.GetChild(_heroPos).childCount != 0)
                                {
                                    Debug.Log("Move the hero to Gameboard");

                                    enemyArena.HeroList.GetChild(_heroPos).GetChild(0).parent = enemyArena.GameBoard.GetChild(_newPos);
                                    Hero _newHeroPlacement = enemyArena.GameBoard.GetChild(_newPos).GetChild(0).GetComponent<Hero>();
                                    _newHeroPlacement.transform.localPosition = Vector3.zero;
                                    _newHeroPlacement.LastHeroPlace = _newHeroPlacement.HeroPlace;
                                    _newHeroPlacement.HeroPlace = _newHeroPlacement.transform.parent.GetComponent<HeroPlace>();
                                   // setOtherPlayerBattleHero();
                                    #region For PhotoNetwork.instatiate
                                    //      Hero _newHeroPlacement = enemyArena.HeroList.GetChild(_heroPos).GetChild(0).GetComponent<Hero>();
                                    //     _newHeroPlacement.GetComponent<PhotonView>().RPC("RPC_AddToGameBoard", PhotonTargets.All, _posId, _newPos);
                                    #endregion

                                }
                            }
                            break;
                        case SyncMoveHero.RemoveGameboard:
                            PlayerHeroes[_playerId].GameboardRemoveHero(ChangedHero[i]);
                            ChangedHero[i].position = _newPos;
                            //Remote Player Game Arena Update
                            if (_playerId != playerId)
                            {
                                //Check Selected Place Whether Null to Prevent Null Reference Exception
                                if (enemyArena.GameBoard.GetChild(_heroPos).childCount != 0)
                                {
                                    Debug.Log("Move away the hero from Gameboard");
                                    enemyArena.GameBoard.GetChild(_heroPos).GetChild(0).parent = enemyArena.HeroList.GetChild(_newPos);

                                    Hero _newHeroPlacement = enemyArena.HeroList.GetChild(_newPos).GetChild(0).GetComponent<Hero>();
                                    _newHeroPlacement.transform.localPosition = Vector3.zero;
                                    _newHeroPlacement.LastHeroPlace = _newHeroPlacement.HeroPlace;
                                    _newHeroPlacement.HeroPlace = _newHeroPlacement.transform.parent.GetComponent<HeroPlace>();
                                }
                            }
                            break;
                        case SyncMoveHero.MoveHero:
                            PlayerHeroes[_playerId].MoveHero(ChangedHero[i], _newPos);
                            //Remote Player Game Arena Update
                            if (_playerId != playerId)
                            {
                                Transform _heroPlacement;
                                //Set the Hero's Placement
                                if (PlayerHeroes[_playerId].GameBoardHeroes.Contains(ChangedHero[i]))
                                {
                                    //Hero is in gameboard
                                    _heroPlacement = enemyArena.GameBoard;
                                    Debug.Log("Moved the hero in Gameboard");
                                    #region For PhotonNetwork.instatiate
                                    //   Hero _newHeroPlacement = enemyArena.GameBoard.GetChild(_heroPos).GetChild(0).GetComponent<Hero>();
                                    //   _newHeroPlacement.GetComponent<PhotonView>().RPC("RPC_AddToGameBoard", PhotonTargets.All, _posId, _newPos);
                                    #endregion

                                }
                                else
                                {
                                    //Hero is not in gameboard
                                    _heroPlacement = enemyArena.HeroList;
                                    Debug.Log("Moved the hero in HeroList");
                                    #region For PhotonNetwork.instatiate
                                    //   Hero _newHeroPlacement = enemyArena.HeroList.GetChild(_heroPos).GetChild(0).GetComponent<Hero>();
                                    // _newHeroPlacement.GetComponent<PhotonView>().RPC("RPC_AddToHeroList", PhotonTargets.All, _posId, _newPos);
                                    #endregion

                                }
                                //Check Selected Place Whether Null to Prevent Null Reference Exception
                                if (_heroPlacement.childCount != 0)
                                {
                                    _heroPlacement.GetChild(_heroPos).GetChild(0).parent = _heroPlacement.GetChild(_newPos);

                                    Hero _newHeroPlacement = _heroPlacement.GetChild(_newPos).GetChild(0).GetComponent<Hero>();
                                    _newHeroPlacement.transform.localPosition = Vector3.zero;
                                    _newHeroPlacement.LastHeroPlace = _newHeroPlacement.HeroPlace;
                                    _newHeroPlacement.HeroPlace = _newHeroPlacement.transform.parent.GetComponent<HeroPlace>();
                                    //_newHeroPlacement.moveToThePlace(_newHeroPlacement, _newHeroPlacement.transform.parent.GetComponent<HeroPlace>());
                                }
                            }
                            break;
                    }
                    break;
                }
            }
            GameManager.Instance.PlayerHero = PlayerHeroes[playerId];
            TeamFlag.Instance.GameboardCard.text = GameManager.Instance.PlayerHero.GameBoardHeroes.Count.ToString();

        }

        /// <summary>
        /// Setting all player's PlayerHeroes[] and remote player's GameArena Hero position, when player buy hero to herolist
        /// </summary>
        /// <param name="_posId"></param>
        /// <param name="_playerId"></param>
        /// <param name="_name"></param>
        /// <param name="_heroPos"></param>
        /// <param name="_heroLevel"></param>
        /// <param name="_syncMethod"></param>
        [PunRPC]
        public void RPC_SyncPlayerHeroes(int _posId, int _playerId, string _name, int _heroPos, HeroLevel _heroLevel, SyncHeroMethod _syncMethod)
        {
            switch (_syncMethod)
            {
                case SyncHeroMethod.AddHero:
                    //Debug.Log("Player [" + _playerId + "] add hero.");
                    PlayerHeroes[_playerId].UsableHeroes.Add(new NetworkHero(_name, _heroPos, _heroLevel));
                    /*   if (_playerId != playerId)
                       {
                           #region Instantiate remote hero object
                           //Loop hero type
                           for (int i = 0; i < MainGameManager.heroTypes.Count; i++)
                           {
                               if (MainGameManager.heroTypes[i].name.Equals(_name))
                               {

                                   Debug.Log("Arena [" + _posId + "] added hero in HeroList [" + _heroPos + "].");
                                   Transform transformParent = PlayerArenas[_posId].GetComponent<PlayerArena>().EnemyArena.HeroList.GetChild(_heroPos);
                                //   Hero remoteHero = (Instantiate(MainGameManager.heroTypes[i].gameObject) as GameObject).GetComponent<Hero>();
                                 //  remoteHero.name = MainGameManager.heroTypes[i].name;
                                 //  remoteHero.transform.parent = transformParent;
                                   //GameObject remoteHero = Instantiate(MainGameManager.heroTypes[i].gameObject, transformParent);
                                   remoteHero.gameObject.transform.localPosition = Vector3.zero;
                                   break;

                               }
                           }
                           #endregion
                       }*/
                    break;
                case SyncHeroMethod.RemoveHero:
                    List<NetworkHero> RemoveHero = PlayerHeroes[_playerId].UsableHeroes;
                    for (int i = 0; i < RemoveHero.Count; i++)
                    {
                        if (RemoveHero[i].name.Equals(_name) &&
                            RemoveHero[i].position == _heroPos &&
                            RemoveHero[i].HeroLevel == _heroLevel)
                        {
                            RemoveHero.Remove(RemoveHero[i]);
                            break;
                        }
                    }
                    if (_playerId != playerId)
                    {
                        #region Destroy remote hero object
                        DestroyImmediate(PlayerArenas[_posId].GetComponent<PlayerArena>().EnemyArena.HeroList.
                            GetChild(_heroPos).GetChild(0).gameObject);
                        #endregion
                    }
                    break;
                case SyncHeroMethod.HeroUpgrade:
                    List<NetworkHero> upgradeUsableHero = PlayerHeroes[_playerId].UsableHeroes;
                    for (int i = 0; i < upgradeUsableHero.Count; i++)
                    {
                        if (upgradeUsableHero[i].name.Equals(_name) &&
                            upgradeUsableHero[i].position == _heroPos &&
                            upgradeUsableHero[i].HeroLevel == _heroLevel - 1)
                        {
                            upgradeUsableHero[i].HeroLevel = _heroLevel;
                            break;
                        }
                    }
                    if (_playerId != playerId)
                    {
                        #region Updrage remote hero object
                        PlayerArenas[_posId].GetComponent<PlayerArena>().EnemyArena.HeroList.
                            GetChild(_heroPos).GetChild(0).GetComponent<Hero>().HeroLevel = _heroLevel;
                        #endregion
                    }
                    break;
            }
            GameManager.Instance.PlayerHero = PlayerHeroes[playerId];
        }

        #endregion

        #region Battle

        [PunRPC]
        void RPC_Battle(int hostID, int guestID)
        {
            battlePosId = PlayerHeroes[hostID].posId;
            if (playerId == guestID)
            {
                PlayerArenas[PlayerHeroes[guestID].posId].GetComponent<PlayerArena>().Camera.SetActive(false);
                PlayerArenas[PlayerHeroes[hostID].posId].GetComponent<PlayerArena>().enemyCamera.SetActive(true);
                opponent.opponentId = hostID;
                setOppoentHero(hostID, hostID);
                isHomeTeam = false;
                setBattleGameBoardHero();
                playerCharacter.GetComponent<PhotonView>().RPC("RPC_PlayerCharacterMoveToGameBoard", PhotonTargets.All, PlayerHeroes[hostID].posId);
            }
            if (playerId == hostID) {
                isHomeTeam = true;
                opponent.opponentId = guestID;
                setOppoentHero(hostID, guestID);
               // setBattleGameBoardHero();
                StartCoroutine(startBattle(PlayerHeroes[hostID].posId));
            }
        }
      /*  void setOtherPlayerBattleHero(int otherPlayerId,int otherPlayerPosId,int placeId,bool isAdd) {
            if (isAdd)
            {

            }
            else {

            }
        }*/
        void setOppoentHero(int homePlayerId, int OpponentPlayerId) {
            foreach (NetworkHero networkHero in PlayerHeroes[OpponentPlayerId].GameBoardHeroes)
            {
                Hero heroObject = PlayerArenas[PlayerHeroes[OpponentPlayerId].posId].
                                  GetComponent<PlayerArena>().SelfArena.GameBoard.
                                  GetChild(networkHero.position).GetChild(0).GetComponent<Hero>();
                heroObject.isEnemy = true;
                //Hero heroObject = GameManager.Instance.SelfPlayerArena.SelfArena.GameBoard.GetChild(networkHero.position).GetChild(0).GetComponent<Hero>(); 
                opponent.hero.Add(heroObject);
                if (playerId == homePlayerId)
                    heroObject.GetComponent<PhotonView>().RPC("RPC_MoveToThePlayerHeroPlace", PhotonTargets.All, PlayerHeroes[homePlayerId].posId, networkHero.position);
            }

        }
        void setBattleGameBoardHero(){
                battleGameBoardHero = new List<Character>(selfGameBoardHero);
        }
        public void battleHeroDie(bool isEnemy,Character hero) {
            if (isHomeTeam)
            {
                if (isEnemy)
                {
                    Debug.Log("character die " + hero.name);
;                    opponent.hero.Remove(hero);
                    if(opponent.opponentId!=-1)
                    PhotonView.RPC("RPC_SyncBattleHero", PlayerHeroes[opponent.opponentId].player,hero.networkPlaceId,true);
                    if (opponent.hero.Count == 0)
                    {
                        Debug.Log("i win ");
                        playerWinBattle(playerId,opponent.opponentId);                    
                    }
                }
                else
                {                  
                    if (opponent.opponentId != -1)
                    {
                        battleGameBoardHero.Remove(hero);
                        PhotonView.RPC("RPC_SyncBattleHero", PlayerHeroes[opponent.opponentId].player, hero.networkPlaceId, false);
                        if (battleGameBoardHero.Count == 0)
                        {
                            Debug.Log("opponent win ");
                            playerWinBattle(opponent.opponentId, playerId);                    
                        }
                    }
                }
            }
        }
        public void playerWinBattle(int playerId,int loserId) {
            if(loserId!=-1)
             PhotonView.RPC("RPC_HitOpponent", PlayerHeroes[playerId].player);
            Debug.Log("player win Battle");
            /*   if (this.playerId == playerId)
               {
                   for (int i = 0; i < battleGameBoardHero.Count; i++)
                   {
                       battleGameBoardHero[i].GetComponent<PhotonView>().RPC("hitPlayerCharacter", PhotonTargets.All);
                   }
               }
               else {
                   for (int i = 0; i < opponent.hero.Count; i++)
                   {
                       opponent.hero[i].GetComponent<PhotonView>().RPC("hitPlayerCharacter", PhotonTargets.All);
                   }
               }*/
            if (playerId == -1 || loserId == -1)
                PhotonView.RPC("RPC_Response", PhotonTargets.All, 1);
            else
                PhotonView.RPC("RPC_Response", PhotonTargets.All, 2);

            if (opponent.opponentId != -1)
            {
               // PhotonView.RPC("RPC_FinishBattle", PlayerHeroes[opponent.opponentId].player);
                PhotonView.RPC("RankChange",PhotonTargets.All, PlayerHeroes[loserId].player.NickName,5);
            }
            // ResetHeroAfterBattle();
            map.resetMap();

        }
        [PunRPC]
        public void RPC_HitOpponent() {
            hitOpponent();
        }
        void hitOpponent() {
            Debug.Log("Newwork site hit oppoent");
            for (int i = 0; i < battleGameBoardHero.Count; i++)
            {
                battleGameBoardHero[i].GetComponent<PhotonView>().RPC("RPC_HitPlayerCharacter", PhotonTargets.All, isHomeTeam);
            }
        }
        [PunRPC]
        public void RPC_SyncBattleHero(int id,bool isSelf) {
            int index;
            if (isSelf)
            {
                index = NetworkManager.Instance.selfGameBoardHero.FindIndex(x => x.networkPlaceId == id);
                if (index != -1)
                    battleGameBoardHero.RemoveAt(index);
            }
            else
            {
                index = NetworkManager.Instance.opponent.hero.FindIndex(x => x.networkPlaceId == id);
                if (index != -1)
                    opponent.hero.RemoveAt(index);
            }
        }
      
         IEnumerator finishBattle() {
            yield return new WaitForSeconds(1.5f);
            if (!isHomeTeam)
            {
                playerCharacter.GetComponent<PhotonView>().RPC("RPC_PlayerCharacterBackToGameBoard", PhotonTargets.All, posId);
                PlayerArenas[PlayerHeroes[opponent.opponentId].posId].GetComponent<PlayerArena>().enemyCamera.SetActive(false);
                GameManager.Instance.SelfPlayerArena.Camera.SetActive(true);
            }
        
            ResetHeroAfterBattle();
        }
        void ResetHeroAfterBattle() {
           Debug.Log("reset Position id: "+playerId+" Hero "+selfGameBoardHero.Count);          
            foreach (Hero hero in selfGameBoardHero)
            {
                hero.gameObject.SetActive(true);             
                hero.photonView.RPC("RPC_AddToGameBoard", PhotonTargets.All, posId, hero.networkPlaceId);
                hero.photonView.RPC("RPC_ResetStatus", PhotonTargets.All);
            }
            opponent.hero.Clear();
        }
        [PunRPC]
        public void RPC_Response(int number) {
            waveFinishResponse+=number;
            Debug.Log("Response +" + number+" total "+waveFinishResponse);
            if (PhotonNetwork.isMasterClient)
            {
                if (waveFinishResponse == PlayerHeroes.Length) {
                    PhotonView.RPC("RPC_endWave", PhotonTargets.All);
                }
            }
               
        }
        [PunRPC]
        public void RPC_resetResponse()
        {
            waveFinishResponse = 0;
        }
        [PunRPC]
        public void RPC_endWave()
        {
            StartCoroutine(finishBattle());
        }
        
        IEnumerator startBattle(int playerPosId) {
            yield return new WaitForSeconds(2);
            battleGameBoardHero = new List<Character>(selfGameBoardHero);
            foreach (Hero hero in battleGameBoardHero)
            {
                //hero
                hero.readyForBattle(false, playerPosId);
            }


            foreach (Character hero in opponent.hero)
            {
                hero.readyForBattle(true, playerPosId);
            }
        }
        #endregion
        #region monster
        [PunRPC]
        public void MonsterBattle() {
            isHomeTeam = true;
            opponent.opponentId = -1;
            battlePosId = posId;
            MonsterWaveManager.Instance.spawnCurrentWaveAllMonster();
        }
        public void spawnMonster(string name,int placeId) {
            Monster monster = PhotonNetwork.Instantiate(Path.Combine("Prefabs", name), Vector3.zero, Quaternion.identity, 0).GetComponent<Monster>();
            monster.GetComponent<PhotonView>().RPC("RPC_MoveToThePlayerHeroPlace", PhotonTargets.All, NetworkManager.Instance.posId, placeId);
            opponent.hero.Add(monster);
        }

        public void BattleWithMonsters() {
            StartCoroutine(startBattle(posId));
        }
        #endregion
        [PunRPC]
        void RPC_Test() {
            Hero newhero = (PhotonNetwork.Instantiate(Path.Combine("Prefabs", "GOBLIN"), Vector3.zero, Quaternion.identity, 0)).GetComponentInChildren<Hero>();
            //  Monster monster = (PhotonNetwork.Instantiate(Path.Combine("Prefabs", "GOBLIN"), Vector3.zero, Quaternion.identity, 0)).GetComponentInChildren<Monster>();
           // newhero.photonView.RPC("RPC_AddToHeroList", PhotonTargets.All,0,1);
            //newhero.name = "Armor Crocodile";
            // if (TFT.GameManager.Instance.BuyHero(newHero));
           // Debug.Log("Hero GameBoard"+ selfGameBoardHero.Count);
        }

        [PunRPC]
        public void RankChange(string _playerName, int _value)
        {
            Debug.Log("RankChange " + _playerName);
            RankManager.DeductHP(_playerName, _value);
        }
    }
}