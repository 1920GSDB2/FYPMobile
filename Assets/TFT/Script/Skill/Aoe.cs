﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aoe : MonoBehaviour
{
    
    public float delayOpenCollider=0;
    public Collider collider;
    protected float damage;
    protected bool isMirror,isAlly;
    void Start()
    {
        collider = GetComponent<Collider>();
        StartCoroutine(openCollider());

    }
    public void setDamage(float damage,bool isMirror,bool isEnemy){
        this.damage = damage;
        this.isMirror = isMirror;
        this.isAlly = isEnemy;
    }
    IEnumerator openCollider() {
        yield return new WaitForSeconds(delayOpenCollider);
        collider.enabled = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!isMirror)
        {
            if (other.tag == "Character")
            {
                bool target = other.GetComponent<Character>().isEnemy;
                if (target!=isAlly)
                {
                    other.GetComponent<PhotonView>().RPC("RPC_TargetTakeDamage", PhotonTargets.All, damage);
                }
            }
        }
        //  target.GetComponent<PhotonView>().RPC("RPC_TargetTakeDamage", PhotonTargets.All, attackDamage, (byte)type, duration);
    }
}
