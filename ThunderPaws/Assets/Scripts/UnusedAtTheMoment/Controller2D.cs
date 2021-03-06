﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : RaycastController_old {
    public CollisionInfo collisions;
    Vector2 playerInput;

    private Animator animator;
    private Transform playerGraphics;
    public Transform playerArm;
    private bool facingRight = true;
    private bool rightSideUp = true;

    //max angle we can run up or down without sliding
    float maxClimbAngle = 80f;
    float maxDescendAngle = 75f;

    public override void Start() {
        //parent start
        base.Start();
        collisions.faceDir = 1;
        animator = GetComponent<Animator>();
        playerGraphics = transform.FindChild("Graphics");
        if(playerGraphics == null) {
            //couldn't find player graphics 
            Debug.LogError("Cannot find Graphics on player");
        }
    }

    public void Move(Vector2 moveAmount, bool standingOnPlatform) {//this method is now useless - I think
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false) {
        //handle collisions
        UpdateRaycastOrigins();
        //Blank slate each time
        collisions.Reset();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        if (moveAmount.x != 0) {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }
        if (moveAmount.y < 0) {
            DescendSlope(ref moveAmount);
        }
        //due to wallsliding always check horizontal collisions
        HorizontalCollisions(ref moveAmount);
        if (moveAmount.y != 0) {
            VerticalCollisions(ref moveAmount);
        }

        //Rotate the player based on direction pointing - its more natural
        Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - playerArm.position;
        //Normalize the vector x + y + z = 1
        diff.Normalize();
        //find the angle in degrees
        float rotZ = Mathf.Abs(Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        if ((rotZ <= 90f) && !facingRight) {
            //face right
            Flip();
            if (!rightSideUp) {
                invertArm();
            }
        } else if ((rotZ > 90f) && facingRight) {
            //face left
            Flip();
            if (rightSideUp) {
                invertArm();
            }
        }
        //multiply by input so animation plays only when input is supplied instead of all the time because its a moving platform
        animator.SetFloat("Speed", Mathf.Max(Mathf.Abs(moveAmount.x), Mathf.Abs(moveAmount.y)) * (input.Equals(Vector2.zero) ? 0 : 1));

        //Move the object
        transform.Translate(moveAmount);

        if (standingOnPlatform) {
            collisions.below = true;
        }
    }


    //Pass in a reference to the actual parameter variable so any change inside the method changes the passed in variable
    void HorizontalCollisions(ref Vector2 moveAmount) {
        //get direction of moveAmount
        float directionX = collisions.faceDir;
        //positive value of moveAmount + skinWidth to get out of the collider
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        if (Mathf.Abs(moveAmount.x) < skinWidth) {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; ++i) {//no way this matters
                                                      //moving left                 moving right
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit) {
                //check for moving platforms that we're not inside the collision. I.E. the plaform is behind us
                if (hit.distance == 0) {
                    continue;
                }
                //get the angle of the surface we hit
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (i == 0 && slopeAngle <= maxClimbAngle) {//bottom most ray
                    if (collisions.descendingSlope) {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld) {//started climbing a new angle
                        distanceToSlopeStart = hit.distance - skinWidth;
                        //subtract so when we call ClimbSlop, it only uses moveAmount x it has when it reaches the slope
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle);
                    //reset it
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                //only check other rays if not climbing slope or slope angle > maxclimb angle
                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
                    //set y moveAmount to the distance between where we fired, and where the raycast intersected with an obstacle
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    //also update moveAmount on y axis
                    if (collisions.climbingSlope) {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    //if we hit something going left or right, store the info
                    collisions.left = (directionX == -1);
                    collisions.right = (directionX == 1);
                }
            }
        }
    }

    //Pass in a reference to the actual parameter variable so any change inside the method changes the passed in variable
    void VerticalCollisions(ref Vector2 moveAmount) {
        //get direction of moveAmount
        float directionY = Mathf.Sign(moveAmount.y);
        //positive value of moveAmount + skinWidth to get out of the collider
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; ++i) {
            //moving down                 moving up
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x); //include .x because we want to do it from where we will be once we've moved
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            if (hit) {
                //Check for one way platforms - or completely through ones
                if (hit.collider.tag == "OBSTACLE-THROUGH") {
                    if (directionY == 1 || hit.distance == 0) {
                        continue;
                    }
                }
                if (collisions.fallingThroughPlatform) {
                    continue;
                }
                if (playerInput.y == -1) {
                    collisions.fallingThroughPlatform = true;
                    Invoke("ResetFallingThroughPlatform", 0.25f);//give the player half a second chance to fall through the platform
                    continue;
                }
                //set y moveAmount to the distance between where we fired, and where the raycast intersected with an obstacle
                moveAmount.y = (hit.distance - skinWidth) * directionY;
                //we change the ray length so that if there is a higher ledge on the left, but not the right, we dont pass through the higher point.
                rayLength = hit.distance;

                //also update moveAmount x
                if (collisions.climbingSlope) {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                //if we hit something going up or down, store the info
                collisions.below = (directionY == -1);
                collisions.above = (directionY == 1);
            }
        }

        //handle slope changes within a current slope
        if (collisions.climbingSlope) {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit) {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle) {//this means we've collided with a new slope
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    //Speed when climbing slope same as normal. Treat moveAmount.x as the total distance  up the slope we want to move
    //then that distance and slope angle = moveAmount x and y
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle) {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        //check if we're jumping on the slope
        if (moveAmount.y <= climbmoveAmountY) {
            moveAmount.y = climbmoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); //but maintain direction right or left
            //assume we're standing on the ground if climbing slope
            collisions.below = true;
            //store the slope info
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    //Same as climbing just inverse
    void DescendSlope(ref Vector2 moveAmount) {
        float directionX = Mathf.Sign(moveAmount.x);
        //cast a ray down and if we're moving left, bottom right, otherwise bottom right
        Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            //if we have a flat surface, dont care
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
                if (Mathf.Sign(hit.normal.x) == directionX) {//check if moving down the slope
                    //distance to slope is wihtin the distance we need to move to get ot it, so the slope should take effect
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
                        float moveDistance = Mathf.Abs(moveAmount.x);
                        float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); //but maintain direction right or left
                        moveAmount.y -= descendmoveAmountY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void ResetFallingThroughPlatform() {
        collisions.fallingThroughPlatform = false;
    }

    private void Flip() {
        // Switch the way the player is labelled as facing.
        facingRight = !facingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = playerGraphics.localScale;
        theScale.x *= -1;
        playerGraphics.localScale = theScale;
    }

    private void invertArm() {
        //switch the way the arm is labeled as facing
        rightSideUp = !rightSideUp;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = playerArm.localScale;
        theScale.y *= -1;
        playerArm.localScale = theScale;

        //Also deal with the arm rotation axis offset since the graphics, arm, and colliders are all seperate.
        //This 0.3 offset is because the pivot point on the graphics is dead center, but the arm is at the shoulder for a natural arm movement.
        //The offset allows the arm to stay in place when left or right. Otherwise it jutts out when facing left because its flipping scale based on the rotational axis
        if (theScale.y < 0f) {
            theScale = playerArm.transform.localPosition;
            theScale.x += 0.3f;
        } else {
            theScale = playerArm.transform.localPosition;
            theScale.x -= 0.3f;
        }
        playerArm.transform.localPosition = theScale;
    }

    public struct CollisionInfo {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 moveAmountOld;
        public int faceDir;//1 right   -1 left
        public bool fallingThroughPlatform;

        public void Reset() {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
        }
    }

}