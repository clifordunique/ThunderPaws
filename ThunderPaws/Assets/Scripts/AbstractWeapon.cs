﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Abstract class that has functionality all weapons share
public abstract class AbstractWeapon : MonoBehaviour {

    [Header("Abstract: Attributes")]
    //how fast the weapon can shoot per second in addition to the first click
    public float fireRate = 0f; //0 is single shot, 0 > is machine gun-esc
    //how much damage it does
    public int Damage = 10;
    public LayerMask whatToHit;

    [Header("Abstract: Effects")]
    //bullet graphics
    public Transform bulletTrailPrefab;
    public Transform hitPrefab;
    public Transform muzzleFlashPrefab;
    public Transform firePoint;//where the bulklet will spawn

    [Header("Abstract: TimeAttributes")]
    //graphics spawning
    public float timeToSpawnEffect = 0f;
    public float effectSpawnRate = 10f;
    //delay between firing
    public float _timeToFire = 0f;

    protected void Start () {
        if (firePoint == null) {
            Debug.LogError("AbstractWeapon.cs: No firePoint found");
        }
    }

    public virtual void GenerateEffect(Vector3 shotPos, Vector3 shotNormal) {
        //fire the projectile - this will travel either out of the frame or hit a target - below should instantiate and destroy immediately
        Transform trail = Instantiate(bulletTrailPrefab, firePoint.position, firePoint.rotation) as Transform;
        Bullet bullet = trail.GetComponent<Bullet>();
        bullet.Fire(shotPos, shotNormal);//fire at the point clicked

        //Generate muzzleFlash
        Transform muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation) as Transform;
        //parent to firepoint
        muzzleFlash.parent = firePoint;
        //randomize its size a bit
        float size = Random.Range(0.2f, 0.5f);
        muzzleFlash.localScale = new Vector3(size, size, size);
        //Destroy muzzle flash
        Destroy(muzzleFlash.gameObject, 0.035f);
    }

}
