﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    public Collection collectionPrefab;
    public Transform collectionBag;
    public Button unLockBtn,closeBtn;
    public GameObject buyPanel;
    int currentCharacterPrice;
    TFTCharacter currnetCharacterType;
    public TextMeshProUGUI payText;
    public void Start()
    {
        Instance = this;
        TFTCharacter type =0;
        for (int i = 0; i < (int)TFTCharacter.TotalCharacter; i++) {
            CreateCollection(type++,true);
        }
        unLockBtn.onClick.AddListener(BuyCharacter);
        closeBtn.onClick.AddListener(ClosePurchaseMenu);
       
    }
    public void CreateCollection(TFTCharacter type,bool isLock) {
        Collection collection = Instantiate(collectionPrefab,collectionBag);
        collection.setCollection(type,isLock);

    }

    public void OpenPurchaseMenu(TFTCharacter type) {
        currnetCharacterType = type;
        currentCharacterPrice = CollectionStore.Instance.GetPrice(currnetCharacterType);
        payText.text = "$ " + currentCharacterPrice + "to unlock this item";
        buyPanel.SetActive(true);
    }
    public void ClosePurchaseMenu() {
        buyPanel.SetActive(false);
    }
    public void BuyCharacter()
    {
        
    }


}
