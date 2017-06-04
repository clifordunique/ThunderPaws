﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour {//TODO: Turn this into an abstract class, then a PlayerWeapon, and AIWeapon that extend it

    [Header("Attributes")]
    //how fast the weapon can shoot per second in addition to the first click
    public float fireRate = 0f; //0 is single shot, 0 > is machine gun-esc
    //how much damage it does
    public int Damage = 10;
    public LayerMask whatToHit;

    [Header("Effects")]
    //bullet graphics
    public Transform bulletTrailPrefab;
    public Transform hitPrefab;
    public Transform muzzleFlashPrefab;
    public Transform firePoint;//where the bulklet will spawn
    private CameraShake _camShake;

    [Header("DeltaAttributes")]
    //graphics spawning
    public float timeToSpawnEffect = 0f;
    public float effectSpawnRate = 10f;
    //handle camera shake for hits
    public float camShakeAmount = 0.025f;
    public float camShakeLength = 0.1f;
    //delay between firing
    private float _timeToFire = 0f;


    private void Start() {
        _camShake = GameMaster.instance.GetComponent<CameraShake>();
        if(_camShake == null) {
            Debug.LogError("Weapon.cs: No CameraShake found on game master");
        }

        if (firePoint == null) {
            Debug.LogError("Weapon.cs: No firePoint found");
        }
    }

    private void Update() {
        if (fireRate == 0) {//single fire
            if (Input.GetButtonDown("Fire1")) {
                Shoot();
            }
        } else {//auto
            if(Input.GetButton("Fire1") && Time.time > _timeToFire) {
                //update time to fire
                _timeToFire = Time.time + 1 / fireRate;
                Shoot();
            }
        }
    }

    //fire a projectile
    private void Shoot() {
        //store mouse position (B)
        Vector2 mousePosition = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
        //store bullet origin spawn popint (A)
        Vector2 firePointPosition = new Vector2(firePoint.position.x, firePoint.position.y);
        //collect the hit data - distance and direction from A -> B
        RaycastHit2D hit = Physics2D.Raycast(firePointPosition, mousePosition - firePointPosition, 100, whatToHit);

        //the bullet fires off into the direction not stopping where the mouse it
        //draw a line for testing
        Debug.DrawLine(firePointPosition, (mousePosition - firePointPosition) * 100, Color.cyan);
        if (hit.collider != null) {
            //draw a line for testing
            Debug.DrawLine(firePointPosition, hit.point, Color.red);
            //check for baddie hits and damage them
        }

        //generate bullet effect
        if (Time.time >= timeToSpawnEffect) {
            //bullet effect position data
            Vector3 hitPosition;
            Vector3 hitNormal;

            if (hit.collider == null) {//we didn't hit anything within the definet layerMask
                hitPosition = (mousePosition - firePointPosition) * 30; //arbitrarily laarge number so the bullet trail flys off the camera
                hitNormal = new Vector3(999, 999, 999); //rediculously huge so we can use it as a sanity check for the effect
            } else {//we hit something
                hitPosition = hit.point;//exactly where the collision occured
                hitNormal = hit.normal;
            }
            //actually instantiate the effect
            GenerateEffect(hitPosition, hitNormal);
            timeToSpawnEffect = Time.time + 1 / effectSpawnRate;
        }
    }

    void GenerateEffect(Vector3 hitPos, Vector3 hitNormal) {
        //Generate hit Particles
        if(hitNormal != new Vector3(999, 999, 999)) {
            //we actually hit something
            //mask it so when we hit something the particles shoot OUT from it.
            Transform hitParticles = Instantiate(hitPrefab, hitPos, Quaternion.FromToRotation(Vector3.up, hitNormal)) as Transform;
            //Destroy hit particles
            Destroy(hitParticles.gameObject, 1f);
        }

        //Generate muzzleFlash
        Transform muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation) as Transform;
        //parent to firepoint
        muzzleFlash.parent = firePoint;
        //randomize its size a bit
        float size = Random.Range(0.2f, 0.5f);
        muzzleFlash.localScale = new Vector3(size, size, size);
        //Destroy muzzle flash
        Destroy(muzzleFlash.gameObject, 0.035f);//TODO: this looks laaggy. idk why its so fast on destruction. had to make it 0.035 instead of desired 0.02

        //Generate bullet trail
        Transform trail = Instantiate(bulletTrailPrefab, firePoint.position, firePoint.rotation) as Transform;
        LineRenderer lr = trail.GetComponent<LineRenderer>();
        //allows the bullet trail to stop where the collision happenned
        if (lr != null) {
            lr.SetPosition(0, firePoint.position);//start position index
            lr.SetPosition(1, hitPos);//end position index
        }
        Destroy(trail.gameObject, 0.035f);//TODO: this looks laaggy. idk why its so fast on destruction. had to make it 0.035 instead of desired 0.02

        //Generate camera shake
        _camShake.Shake(camShakeAmount, camShakeLength);

        //TODO: generate audio
    }

}
