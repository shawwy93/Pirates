using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPS_MouseLook : MonoBehaviour
{

    [SerializeField] float sensitivityX = 8f;
    [SerializeField] float sensitivityY = 8f;
    float mouseX, mouseY;

    [SerializeField] Transform playerCam;
    [SerializeField] float xClamp = 80f;
    float xRot = 0;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, mouseX * Time.deltaTime);

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -xClamp, xClamp);
        Vector3 targetRot = transform.eulerAngles;
        targetRot.x = xRot;
        playerCam.eulerAngles = targetRot;
    }

    public void ReceiveInput(Vector2 mouseInput)
    {
        mouseX = mouseInput.x * sensitivityX;
        mouseY = mouseInput.y * sensitivityY;

    }

}
