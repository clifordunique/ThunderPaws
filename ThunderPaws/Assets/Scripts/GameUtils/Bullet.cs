﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : BulletBase {

    private void Start() {
        //Call the base start
        base.Start();
    }

    private void Update () {
        //Raycast to check if we could potentially the target
        RaycastHit2D possibleHit = Physics2D.Raycast(transform.position, TargetPos - transform.position);
        if (possibleHit.collider != null){
            //Mini raycast to check handle ellusive targets
            RaycastHit2D distCheck = Physics2D.Raycast(transform.position, TargetPos - transform.position, 0.2f, WhatToHit);
            if (distCheck.collider != null) {
                HitTarget(transform.position, distCheck.collider);
                //We don't want to stop the bullet trajectory if we're hitting the trigger.
                //If we're on the ground - which is the only time the rocket jump boost can be applied, the bullet should hit the ground instead of the trigger
                if (distCheck.collider.gameObject.tag != "ROCKETJUMPTRIGGER") {
                    return;
                }
            }

            //Last check is simplest check
            Vector3 dir = TargetPos - transform.position;
            float distanceThisFrame = MoveSpeed * Time.deltaTime;
            //Length of dir is distance to target. if thats less than distancethisframe we've already hit the target
            if (dir.magnitude <= distanceThisFrame) {
                //Make sure the player didn't dodge out of the way
                distCheck = Physics2D.Raycast(transform.position, TargetPos - transform.position, 0.2f, WhatToHit);
                if (distCheck.collider != null) {
                    HitTarget(transform.position, distCheck.collider);
                    //We don't want to stop the bullet trajectory if we're hitting the trigger.
                    //If we're on the ground - which is the only time the rocket jump boost can be applied, the bullet should hit the ground instead of the trigger
                    if (distCheck.collider.gameObject.tag != "ROCKETJUMPTRIGGER") {
                        return;
                    }
                }
            }
        }
        //Move as a constant speed
        transform.Translate(TargetDirection.normalized * MoveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Destroy and generate effects
    /// </summary>
    /// <param name="hitPos"></param>
    /// <param name="hitObject"></param>
    protected override void HitTarget(Vector3 hitPos, Collider2D hitObject) {
        //Damage whoever we hit - or rocket jump
        Player player;
        switch (hitObject.gameObject.tag) {
            case "Player":
                Debug.Log("We hit " + hitObject.name + " and did " + Damage + " damage");
                player = hitObject.GetComponent<Player>();
                if(player != null) {
                    player.DamageHealth(Damage);
                }
                break;
            case "ROCKETJUMPTRIGGER":
                Debug.Log("We hit " + hitObject.name + " and rocket jumped!");
                player = hitObject.GetComponentInParent<Player>();
                if (player != null) {
                    player.AllowRocketJump();
                }
                break;
            case "BADDIE":
                Debug.Log("We hit " + hitObject.name + " and did " + Damage + " damage");
                if(hitObject.GetComponent<Baddie>() != null) {
                    Baddie baddie = hitObject.GetComponent<Baddie>();
                    if (baddie != null) {
                        //Naturally someone would realize they're being attacked if they were shot so retaliate
                        if (baddie.State != MentalStateEnum.ATTACK) {
                            baddie.State = MentalStateEnum.ATTACK;
                        }
                        baddie.DamageHealth(Damage);
                    }
                }else if(hitObject.GetComponent<BaddieBoss>() != null) {
                    BaddieBoss boss = hitObject.GetComponent<BaddieBoss>();
                    boss.DamageHealth(Damage);
                }
                break;
        }
        if (!hitObject.gameObject.tag.Equals("ROCKETJUMPTRIGGER")) {
            //Mask it so when we hit something the particles shoot OUT from it.
            Transform hitParticles = Instantiate(HitPrefab, hitPos, Quaternion.FromToRotation(Vector3.up, TargetNormal)) as Transform;
            //Destroy hit particles
            Destroy(hitParticles.gameObject, 1f);
            Destroy(gameObject);
        }
    }
}
