﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayer : MonoBehaviour
{
    public TextMeshProUGUI PlayerLevel;
    public Image PlayerIcon;
    public TextMeshProUGUI PlayerName;
    public Button ReadyButton;
    public Button KickButton;
    public PhotonPlayer PhotonPlayer { get; private set; }
    bool isLocal;
    // Start is called before the first frame update
    void Start()
    {
        if (isLocal) KickButton.gameObject.SetActive(false);
    }
    public void setPhotonPlayer(PhotonPlayer photonPlayer)
    {
        PhotonPlayer = photonPlayer;
        PlayerName.text = photonPlayer.NickName;
    }

}
