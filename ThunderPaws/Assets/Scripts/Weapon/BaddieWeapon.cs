﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaddieWeapon : AbstractWeapon {
    /// <summary>
    /// Deprecated.
    /// Used to modify the y value of the shot trajoctory
    /// </summary>
    [Header("Mutator Attributes")]
    public float ShotYMutatorLow = 0.5f;
    /// <summary>
    /// Deprecated.
    /// Used to modify the x value of the shot trajectory
    /// </summary>
    public float ShotYMutatorHigh = 1.5f;

    /// <summary>
    /// Baddie AI reference
    /// </summary>
    private BaddieAI _baddieAI;
    /// <summary>
    /// Layermask indicating what to hit
    /// </summary>
    public LayerMask WhatToHit;

    protected void Start() {
        base.Start();
        _baddieAI = gameObject.transform.parent.transform.parent.GetComponent<BaddieAI>();
        if(_baddieAI == null) {
            Debug.LogError("Weapon.cs: No BaddieAI script found on Baddie");
            throw new MissingReferenceException();
        }
    }

    private void Update() {
        //If the target is within the killzone, shoot
        if (_baddieAI.State == BaddieAI.BaddieState.ATTACK && Time.time > _timeToFire) {
            //Update time to fire
            _timeToFire = Time.time + 1 / FireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Uses the defined high and low values to get a random number between them multiplied by either 1 or -1 for high shots or low shots
    /// </summary>
    /// <returns></returns>
    private float GetShotMutator() {
        return (Random.Range(ShotYMutatorLow, ShotYMutatorHigh) * (Random.Range(0,2)*2-1));

    }

    /// <summary>
    /// Fire a projectile
    /// </summary>
    private void Shoot() {
        //Store mouse position (B)
        Vector2 targetPosition = new Vector2(_baddieAI.Target.position.x, _baddieAI.Target.position.y /*+ GetShotMutator()*/);
        //Store bullet origin spawn popint (A)
        Vector2 firePointPosition = new Vector2(FirePoint.position.x, FirePoint.position.y);
        //Collect the hit data - distance and direction from A -> B
        RaycastHit2D shot = Physics2D.Raycast(firePointPosition, targetPosition - firePointPosition, 100, WhatToHit);


        //Generate bullet effect
        if (Time.time >= TimeToSpawnEffect) {
            //Bullet effect position data
            Vector3 hitPosition;
            Vector3 hitNormal;
            //Arbitrarily large number so the bullet trail flys off the camera
            hitPosition = (targetPosition - firePointPosition) * 100; 
            if (shot.collider != null) {
                //If we most likely hit something store the normal so the particles make sense when they shoot out
                hitNormal = shot.normal;
                hitPosition = shot.point;
            } else {
                //Rediculously huge so we can use it as a sanity check for the effect
                hitNormal = new Vector3(999, 999, 999); 
            }

            //Actually instantiate the effect
            GenerateEffect(hitPosition, hitNormal, WhatToHit);
            TimeToSpawnEffect = Time.time + 1 / EffectSpawnRate;
        }
    }

}
