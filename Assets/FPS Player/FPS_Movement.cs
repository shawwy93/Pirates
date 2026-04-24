using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPS_Movement : MonoBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] float speed = 10f;
    Vector2 horizontalInput;

    [SerializeField] float jumpHeight = 3.5f;
    bool jump;

    [SerializeField] float gravity = -30f;
    Vector3 verticalV = Vector3.zero;
    [SerializeField] LayerMask groundMask;
    public bool isGrounded;

    private void Update()
    {
        isGrounded = Physics.CheckSphere(transform.position - (new Vector3(0, transform.localScale.y, 0)), 0.2f, groundMask);
        if (isGrounded)
        {
            verticalV.y = 0;
        }
        else
        {
            verticalV.y += gravity * Time.deltaTime;
        }

        Vector3 horizontalV = (transform.right * horizontalInput.x + transform.forward * horizontalInput.y) * speed;
        controller.Move(horizontalV * Time.deltaTime);

        if (jump)
        {
            if (isGrounded)
            {
                verticalV.y = Mathf.Sqrt(-2f * jumpHeight * gravity);
            }
            jump = false;

        }

        controller.Move(verticalV * Time.deltaTime);
    }

    public void ReceiveInput (Vector2 _horizontalInput)
    {
        horizontalInput = _horizontalInput;
    }

    public void OnJumpPressed()
    {
        jump = true;
    }

}
