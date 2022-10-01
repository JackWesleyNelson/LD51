using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum PlayerPower
{
    Slide, Charge, Flip
}

public class Game : MonoBehaviour
{
    
    float playerWalkSpeed = 6f, jumpSpeed = 6f, chargeSpeed = 18f, chargeTime = .75f, slideSpeed = 18f, slideTime = .75f, maxDistanceToBeConsideredGrounded = .05f;
    [SerializeField]
    float movementScalar = 2f;
    bool isJumping = false, isCharging = false, isFlipped = false, isSliding = false, isGrounded = false;
    PlayerPower playerCurrentPower = PlayerPower.Slide;
    Vector2 movementForceToAdd = new Vector2(), rightHitOffset = new Vector3(.5f, 0), leftHitOffset = new Vector3(-.5f, 0), playerPos2D;
    Transform playerTransform;

    [SerializeField]
    LayerMask inversePlayerLayerMask;
    [SerializeField]
    Collider2D playerCollider = null;
    [SerializeField]
    Rigidbody2D playerRigidbody = null;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = playerRigidbody.transform;
        playerPos2D = playerTransform.position;
        StartCoroutine(Every10Seconds());
    }

    // Update is called once per frame
    void Update()
    {
        #region Ideas

        //Every 10 seconds switch to the next game mode, WarioWare-ish.
        //persist state of previous game mode?

        //Platformer where every 10 seconds the world flips upside down, or something changes about the character a special ability converting into the next option 
        //Abilities? Charge through object, add glide to jump, ability to breath underwater, grappling hook, jump continues until you hit an object which you stick to (inversion on jump), 
        //Jump, charge, flip
        //Have it be like an endless runner where something is chasing the player, and they need to choose the route which matches their ability to keep ahead of the danger?

        //Switch between arcade style games. One where you're trying not to get sucked into the center, one where you are trying to climb falling platforms, etc. 
        //Have matching "tiers" so that if you end on a tier in one game that's where you start in the next game.

        //Tower Defense where a new wave comes every 10 seconds, or it switches between the units moving and being able to upgrade/place towers every 10 seconds.

        //3 Tetris like puzzle games that are being played on each third of the screen. Only one game is active at a time for 10 seconds before pausing and shifting to the next (or random?) game.
        //Could make the other puzzle games progress at a much slower pace while they aren't active, so that the player is constantly trying to catch up or fix what the game screwed up while they couldn't play.
        //Tetris, poyo poyo, 

        #endregion
        playerPos2D = playerTransform.position;

    }

    private void FixedUpdate()
    {
        movementForceToAdd.x = 0;
        movementForceToAdd.y = 0;


        TryToGrowPlayer();
        CheckGrounded();
        CheckHittingWall();

        if (Input.GetKey(KeyCode.A))
        {
            movementForceToAdd.x -= playerWalkSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movementForceToAdd.x += playerWalkSpeed;
        }
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            StartCoroutine(Jump());
            isJumping = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivatePower();
        }
        if (isCharging)
        {
            if (movementForceToAdd.x < 0)
            {
                movementForceToAdd.x = -chargeSpeed;
            }
            else
            {
                movementForceToAdd.x = chargeSpeed;
            }
        }
        if (isJumping)
        {
            //TODO: consider changing jump properties based on if the space key is still held down.
            if (isFlipped)
            {
                movementForceToAdd.y *= -1f;
            }
        }
        if (isSliding)
        {
            if (movementForceToAdd.x < 0)
            {
                movementForceToAdd.x = -slideSpeed;
            }
            else
            {
                movementForceToAdd.x = slideSpeed;
            }
            //Reduce character height to some value.
        }
        
        movementForceToAdd *= movementScalar;
        movementForceToAdd += (Vector2) Physics.gravity;
        playerRigidbody.velocity = (movementForceToAdd);
    }

    private void ActivatePower()
    {
        if(playerCurrentPower == PlayerPower.Charge && !isCharging)
        { 
            isCharging = true;
            StartCoroutine(ChargeTime());
        }
        if(playerCurrentPower == PlayerPower.Slide && !isJumping && isGrounded)
        {
            isSliding = true;
            StartCoroutine(SlideTime());
        }
        if(playerCurrentPower == PlayerPower.Flip && isGrounded)
        {
            //Add scrolling texture overlay indicating that gravity is reversed when we're flipped, and remove it when we go back to normal.
            isFlipped = true;
            Physics.gravity = Physics.gravity * -1f;
        }
    }

    //TODO: Change the state of the UI to indicate the current ability, as well as the time left on that ability (such as the outer edge of a square/diamond losing length until the time is over)
    private void SwitchToNextPower()
    {
        playerCurrentPower++;
        int powerCount = PlayerPower.GetNames(typeof(PlayerPower)).Length;
        if (((int)playerCurrentPower) >= powerCount)
        {
            playerCurrentPower = 0;
        }
    }

    private IEnumerator Every10Seconds()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            SwitchToNextPower();
        }
    }

    private IEnumerator Jump()
    {
        float timeElapsed = 0f;
        //Jump up
        while(timeElapsed <= .4f)
        {
            movementForceToAdd.y = jumpSpeed + Physics.gravity.y * -1f;            
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        //Float momentarily
        while (timeElapsed <= .55f)
        {
            movementForceToAdd.y = Physics.gravity.y;
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        //Quickly drop
        while (!isGrounded)
        {
            movementForceToAdd.y = jumpSpeed * -1.5f;
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        yield return null;
    }

    //Consider doing Coyote time, where we set a time frame where the player can still jump after they're no longer grounded.
    private void CheckGrounded()
    {
        RaycastHit2D leftHit;
        RaycastHit2D middleHit;
        RaycastHit2D rightHit;

        if (isFlipped)
        {
            //Check the ground above the character
            leftHit = Physics2D.Raycast(playerPos2D + leftHitOffset, Vector2.up);
            middleHit = Physics2D.Raycast(playerPos2D, Vector2.up);
            rightHit = Physics2D.Raycast(playerPos2D + rightHitOffset, Vector2.up);
        }
        else
        {
            //Check the ground below the character, like normal.
            leftHit = Physics2D.Raycast(playerPos2D + leftHitOffset, Vector2.down);
            middleHit = Physics2D.Raycast(playerPos2D, Vector2.down);
            rightHit = Physics2D.Raycast(playerPos2D + rightHitOffset, Vector2.down);
        }

        ValidateHit(leftHit);
        ValidateHit(middleHit);
        ValidateHit(rightHit);
    }

    private void ValidateHit(RaycastHit2D hit)
    {
        Collider2D collider = hit.collider;
        if ((collider) && collider.IsTouching(playerCollider))
        {
            Debug.Log("Grounded");
        }
        else
        {
            Debug.Log("Not grounded");
        }
    }

    private void CheckHittingWall()
    {
        if (isCharging)
        {
            //Ignore destructible walls
        }
        else
        {
            //Don't ignore any walls.
        }
    }

    //Check if we hit a wall, and if so
    private void EarlyCancelCharge()
    {
        StopCoroutine(ChargeTime());
        isCharging = false;
    }

    private IEnumerator ChargeTime()
    {
        yield return new WaitForSeconds(chargeTime);
        isCharging = false;
    }

    //Check if we hit a wall, and if so
    private void EarlyCancelSlide()
    {
        StopCoroutine(SlideTime());
        isCharging = false;
    }

    private IEnumerator SlideTime()
    {
        yield return new WaitForSeconds(slideTime);
        isSliding = false;
    }

    private void TryToGrowPlayer()
    {
        //Check if there is enough room above the player (or below if flipped) to return to their normal size, and if so do it.
    }

}
