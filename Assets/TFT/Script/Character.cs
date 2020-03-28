﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TFT;
using UnityEngine.UI;
using System.IO;

public class Character : MonoBehaviour
{
    public float Health { get; set; }
    protected float MaxHealth { get;  set; }
    public float MaxMp;
    protected float Mp { get; set; }
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    public float SkillPower { get; set; }
    public float PhysicalDefense { get; set; }
    public float MagicDefense { get; set; }
    public bool isEnemy;
    public HeroState HeroState;
    public HeroPlace HeroPlace, LastHeroPlace;
    public PhotonView photonView;  
    public Animator animator;    
    protected bool isAttackCooldown;
    protected Vector3 cameraPos;
    public Character targetEnemy;
    public Character testHero;
    public GameObject HeroBar;
    protected Image hpBar;
    protected Image mpBar;
    public GameObject bullet;
    
    protected float attackRange = 1.7f;

    // Start is called before the first frame update

      //  hpBar = HeroBar.transform.GetChild(0).GetChild(0).GetComponent<Image>();
       // mpBar = HeroBar.transform.GetChild(0).GetChild(1).GetComponent<Image>();
    

    // Update is called once per frame
    /*void Update()
    {
        Vector3 targetPostition = new Vector3(HeroBar.transform.position.x, cameraPos.y, HeroBar.transform.position.x);
        HeroBar.transform.LookAt(targetPostition);
        if (HeroState == HeroState.Idle)
        {
            if (targetEnemy == null)
                targetEnemy = NetworkManager.Instance.getCloestEnemyTarget(isEnemy, transform);
            else
            {
                HeroState = HeroState.Walking;
                followEnemy();
            }

        }
        if (HeroState == HeroState.Fight)
        {
            if (!isAttackCooldown)
                photonView.RPC("RPC_AttackAnimation", PhotonTargets.All);
            animator.SetTrigger("Attack");

        }
    }*/
    public void attackTarget()
    {
        
        //Debug.Log(targetEnemy.name + " Take Damage");
        if (targetEnemy != null)
        {            
            photonView.RPC("RPC_IncreaseMp", PhotonTargets.All,10f);
            //GetComponent<PhotonView>().RPC("RPC_IncreaseMp", PhotonTargets.All,10);
            targetEnemy.photonView.RPC("RPC_TargetTakeDamage", PhotonTargets.All, AttackDamage);
        }
        // Debug.Log(targetEnemy.name+" have hp: "+ targetEnemy.Health);
       
    }
    public void shootTarget()
    {

        //Debug.Log(targetEnemy.name + " Take Damage");
        if (targetEnemy != null)
        {
            photonView.RPC("RPC_IncreaseMp", PhotonTargets.All, 10f);
            PhotonNetwork.Instantiate(Path.Combine("Bullet", bullet.name), Vector3.zero, Quaternion.identity, 0);
            //targetEnemy.photonView.RPC("RPC_TargetTakeDamage", PhotonTargets.All, AttackDamage);
        }
        // Debug.Log(targetEnemy.name+" have hp: "+ targetEnemy.Health);

    }

    [PunRPC]
    public void RPC_TargetTakeDamage(float damage)
    {
        syncAdjustHp(-damage);
    }
    [PunRPC]
    public void RPC_Heal(float index)
    {
        syncAdjustHp(index);
    }
    public void syncAdjustHp(float damage)
    {
        Health += damage;
        if (Health > MaxHealth)
            Health = MaxHealth;
        if (Health <= 0)
        {
            die();
        }
        hpBar.fillAmount = Health / MaxHealth;
    }
    [PunRPC]
    public void RPC_IncreaseMp(float index)
    {
        syncAdjustMp(index);
    }
    [PunRPC]
    public void RPC_ReduceMp(float index)
    {
        syncAdjustMp(-index);
    }
    public void syncAdjustMp(float mp)
    {
        Mp += mp;
        if (Mp > MaxMp)
            Mp = MaxMp;
        if (Mp <= 0)
        {
            Mp = 0;
        }
        mpBar.fillAmount = Mp / MaxMp;
    }
    public virtual void die() { }

    protected void followEnemy()
    {
        checkWithInAttackRange();
        if(HeroState!=HeroState.Fight)
        PathFindingManager.Instance.requestPath(HeroPlace, targetEnemy.HeroPlace, OnPathFind);
      //  float dis = Vector3.Distance(transform.position, targetEnemy.transform.position);

        // Debug.Log("Distance " + dis + " AttackRange " + attackRange);
      /*  if (dis > attackRange)
        {
            // Debug.Log("Distance " + dis + " AttackRange " + attackRange);
            //  Debug.Log("followEnemy");
            PathFindingManager.Instance.requestPath(HeroPlace, targetEnemy.HeroPlace, OnPathFind);
        }
        else
        {
            if (HeroState != HeroState.Fight)
            {
                HeroState = HeroState.Fight;
                // animator.SetBool("Walk", false);
                photonView.RPC("RPC_StopWalk", PhotonTargets.All);

            }

        }*/
    }
    public void checkWithInAttackRange() {
       
        float dis = Vector3.Distance(HeroPlace.transform.position,targetEnemy.HeroPlace.transform.position);
        if (dis <= attackRange) {
              HeroState = HeroState.Fight;         
              photonView.RPC("RPC_StopWalk", PhotonTargets.All);
      //      return true;
        }
       // return false;
    }
    
    public void OnPathFind(List<Node> path, bool isFindPath)
    {
        if (isFindPath)
        {
            if (path != null)
            {
                //Debug.Log("Find path? " + isFindPath + " Hero " + name);
                StartCoroutine(FollowStep(path[0]));
            }
        }       

    }
    IEnumerator FollowStep(Node step)
    {
        //Debug.Log("FollowStep");
       // withInAttackRange();

        GetComponent<PhotonView>().RPC("RPC_FollowStep", PhotonTargets.Others, step.heroPlace.PlaceId, step.heroPlace.gridY);
        MoveToThePlace(step.heroPlace);
        transform.LookAt(step.heroPlace.transform);
        animator.SetBool("Walk", true);
       // Vector3 destination = new Vector3(step.heroPlace.transform.position.x, transform.position.y, step.heroPlace.transform.position.z);
        while (HeroState == HeroState.Walking)
        {

            if (transform.position != step.heroPlace.transform.position)
            {
               
                transform.position = Vector3.MoveTowards(transform.position, step.heroPlace.transform.position, 3 * Time.deltaTime);
                //   Debug.Log("Move X "+step.heroPlace.gridX+" Y "+ step.heroPlace.gridY);
            }
            else
            {
                //  Debug.Log("finish");
                // isLoop = false;               
                photonView.RPC("RPC_StopWalk", PhotonTargets.All);
               
                StartCoroutine(FindPathAgain());
                //PathFindingManager.Instance.requestPath(HeroPlace, targetEnemy.HeroPlace, OnPathFind);                          
                break;
            }
            yield return null;
        }

    }
    IEnumerator FindPathAgain()
    {
        //yield return new WaitForSeconds(2);
        yield return new WaitForSeconds(0.3f);
        followEnemy();
    }
    [PunRPC]
    public void RPC_FollowStep(int placeId, int YPos)
    {
        SyncFollowStep(placeId, YPos);
    }
    public void SyncFollowStep(int placeId, int YPos)
    {
        bool isEnemyPlace;
        if (YPos <= 3)
            isEnemyPlace = true;
        else
            isEnemyPlace = false;
        HeroPlace heroPlace = NetworkManager.Instance.GetOpponentHeroPlace(placeId, isEnemyPlace);
        StartCoroutine(RPC_FollowHeroPlace(heroPlace));
    }
    public IEnumerator RPC_FollowHeroPlace(HeroPlace step)
    {
        animator.SetBool("Walk", true);
        while (transform.position != step.transform.position)
        {
            transform.position = Vector3.MoveTowards(transform.position, step.transform.position, 3 * Time.deltaTime);
            yield return null;
        }
    }
    public void MoveToThePlace(HeroPlace newHeroPlace)
    {
        HeroPlace.leavePlace();
        newHeroPlace.setHeroOnPlace(this);
        //SetHeroPlace(newHeroPlace);
        LastHeroPlace = HeroPlace;
        HeroPlace = newHeroPlace;
    }
    [PunRPC]
    public void RPC_StopWalk()
    {
        animator.SetBool("Walk", false);
    }
    public void readyForBattle(bool isEnemy, int posId)
    {
        HeroState = HeroState.Idle;
        this.isEnemy = isEnemy;
        //Debug.Log("State " + HeroState + " Enemy " + isEnemy);
        photonView.RPC("RPC_ShowHpBar", PhotonTargets.All, posId);
        
    }
    [PunRPC]
    public void RPC_ShowHpBar(int posid)
    {
        if (NetworkManager.Instance.opponent.hero.Contains(this))
            setHpBarColor(Color.red);
        HeroBar.SetActive(true);
        cameraPos = NetworkManager.Instance.getCamera(posid).transform.position;
    }
    [PunRPC]
    public void RPC_AttackAnimation()
    {
       // transform.LookAt(targetEnemy.transform);
        animator.SetTrigger("Attack");
        //transform.LookAt(targetEnemy.transform);
    }
    [PunRPC]
    public void RPC_MoveToThePlayerHeroPlace(int posId, int placeId)
    {
        HeroPlace heroPlace = NetworkManager.Instance.GetMyGameBoardEnemyHeroPlace(posId, placeId);
        SetHeroPlace(heroPlace);
        // Debug.Log( this.name + " become enemy? " + isEnemy+" Pos "+heroPlace.gridX+" "+heroPlace.gridY);
    }
    public void SetHeroPlace(HeroPlace heroPlace)
    {
        heroPlace.setHeroOnPlace(this);
        //   HeroPlace.leavePlace();
        // transform.parent = heroPlace.gameObject.transform;
        transform.localPosition = Vector3.zero;
        LastHeroPlace = heroPlace;
        HeroPlace = heroPlace;

    }
    public void setHpBarColor(Color color) {
        hpBar.color = color;
    }
    public IEnumerator basicAttackCoolDown()
    {
        isAttackCooldown = true;
        Debug.Log(name+"Coolown");
        yield return new WaitForSeconds(1 / (AttackSpeed * 2.5f));
        Debug.Log(name+"Cool Finish");
        isAttackCooldown = false;
    }
}
