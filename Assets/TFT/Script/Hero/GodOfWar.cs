﻿using System;
using UnityEngine;
public class GodOfWar : Hero
{
    public override void setAttribute()
    {
        MpRecoverRate = 2f;
    }
    public override void UseSkill()
    {
        photonView.RPC("RPC_ReduceMp", PhotonTargets.All, MaxMp);
        photonView.RPC("RPC_castAoeSkill", PhotonTargets.All, targetEnemy.photonView.viewID);
    }
}
