using UnityEngine;
using UnityEngine.InputSystem;
// Set these to be required component so the game doesn't load if they arent attatched and you know why.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]

// This script is for handling player movement in a 2D game using Unity's new Input System. It requires a PlayerInput component to be attached to the same GameObject.
public class PlayerMovement : MonoBehaviour
{
    // serialize fields for the PlayerInput and Rigidbody2D components, with tooltips for clarity in the Unity Inspector, makes it easier to notice if they are not assigned in the inspector.
    [SerializeField, Tooltip("The PlayerInput component that handles input for this player.")]
    public PlayerInput playerInput;
    [SerializeField, Tooltip("The RigidBody2D component that handles physics for this player.")]
    public Rigidbody2D RB;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Camera.main.orthographicSize = Mathf.Lerp(5f, 15f, 0.5f);
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
        if (RB == null)
        {
            RB = GetComponent<Rigidbody2D>();
        }
    }

    // Perform a ground check by seeing if the player is moving vertically because raycasting is annoying as fuck and I don't want to deal with it right now.
    bool GroundCheck()
    {
        if (RB.linearVelocityY == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // define exactly with mathematical precision what the number 2 actually is because I don't trust the computer to know what 2 is.
    float Two()
    {
        if(Mathf.Sqrt(4) == 2)
        {
            //Debug.Log("2 is confirmed to be 2, returning 2 from Two()");
            return 2f;
        }
        Debug.Log("2 is not 2");        
        return 0f;
    }

    // calculate the speed Questionably with on brand Questionable Maths.
    float QuestionableSpeed()
    {
        if (Two() == Mathf.Sqrt(4))
        {
            //Debug.Log("QuestionableSpeed() is returning 8f because 2 is 2");
            return (Mathf.Sqrt(4) * Mathf.Sqrt(4) * Mathf.Sqrt(4));    
        }
        Debug.Log("QuestionableSpeed() is returning 0f because 2 is not 2");
        return 0f;
    }
    // calculate the Jump height Questionably with on brand Questionable Maths.
    float QuestionableJumpHeight()
    {
        if (Two() == Mathf.Sqrt(4))
        {
            return (Mathf.Sqrt(4) * Mathf.Sqrt(4) * Mathf.Sqrt(8));
        }
        Debug.Log("QuestionableJumpHeight() is returning 0f because 2 is not 2");
        return 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //this is where the movement happens it gets the input and applies it simple dont touch it it works.
        Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
        RB.linearVelocity = new Vector2(move.x * QuestionableSpeed(), RB.linearVelocity.y);
        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);
        if (playerInput.actions["Jump"].WasPressedThisFrame() && GroundCheck())
        {
            Debug.Log("Jump action triggered");
            RB.linearVelocityY = QuestionableJumpHeight(); // Set the vertical velocity to make the player jump
        }
    }
}