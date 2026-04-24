using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManagerFPS : MonoBehaviour
{
    [SerializeField] FPS_Movement movement;
    [SerializeField] FPS_MouseLook mouseLook;
    [SerializeField] PlayerInteractions playerInteractions;
    PlayerControlsFPS controls;
    PlayerControlsFPS.GroundMovementActions groundMovement;

    Vector2 horizontalInput;
    Vector2 mouseInput;

    private void Awake()
    {
        controls = new PlayerControlsFPS();
        groundMovement = controls.GroundMovement;

        groundMovement.HorizontalMovement.performed += ctx => horizontalInput = ctx.ReadValue<Vector2>();

        groundMovement.Jump.performed += _ => movement.OnJumpPressed();
        groundMovement.Grab.performed += _ => playerInteractions.OnGrabPressed();

        groundMovement.MouseX.performed += ctx => mouseInput.x = ctx.ReadValue<float>();
        groundMovement.MouseY.performed += ctx => mouseInput.y = ctx.ReadValue<float>();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDestroy()
    {
        controls.Disable();
    }

    private void Update()
    {
        movement.ReceiveInput(horizontalInput);
        mouseLook.ReceiveInput(mouseInput);
    }

}

