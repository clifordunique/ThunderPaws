﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBase : MonoBehaviour {

    /// <summary>
    /// Who the companion follows
    /// </summary>
    public Transform Target;
    /// <summary>
    /// Buffer for position dampeneing so movment is not sudden and jerky
    /// </summary>
    protected float Dampening = 1f;
    /// <summary>
    /// How far to look ahead from our current position
    /// </summary>
    protected float LookAheadFactor = 3;
    /// <summary>
    /// How fast we get to the desired position
    /// </summary>
    protected float LookAheadReturnSpeed = 0.5f;
    /// <summary>
    /// Determines if we should be looking for the target or wheather we're in a close enough range
    /// </summary>
    protected float LookAheadMoveThreshold = 0.1f;
    //Threshold of camera movement down
    protected float YPosClamp = -1;

    protected float OffsetZ;
    protected Vector3 LastTargetPosition;
    protected Vector3 CurrentVelocity;
    protected Vector3 LookAheadPos;

    private float nextTimeToSearch = 0f;
    private float searchDelay = 0.5f;

    private string _searchName;

    protected void InitializeSearchName(string target) {
        _searchName = target;
    }

    protected void Start() {
        LastTargetPosition = Target.position;
        OffsetZ = (transform.position - Target.position).z;
        transform.parent = null;
    }

    protected void FindPlayer() {
        if (nextTimeToSearch <= Time.time) {
            GameObject searchResult = GameObject.FindGameObjectWithTag(_searchName);
            if (searchResult != null) {
                Target = searchResult.transform;
                nextTimeToSearch = Time.time + searchDelay;
            }
        }
    }
}
