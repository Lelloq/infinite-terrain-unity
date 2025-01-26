using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    #region Camera
    public Camera cam;
    public Transform camTransform;
    public Transform bodyTransform;

    public float sens = 2.0f;
    public int fov = 90;

    private float camRot = 0.0f;
    #endregion

    #region Movement
    public CharacterController controller;

    KeyCode forwardKey;
    KeyCode backwardKey;
    KeyCode leftKey;
    KeyCode rightKey;
    KeyCode jumpKey;

    public int speed = 32;
    public int jumpPower = 40;
    int dirX = 0;
    int dirZ = 0;

    bool isGrounded;
    private float g = -25f;
    public Transform groundCheck;
    private float radius = 1.2f;
    public LayerMask GroundMask;
    Vector3 vel;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        forwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Forward", "W"));
        backwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Backward", "S"));
        leftKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Left", "A"));
        rightKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Right", "D"));
        jumpKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Jump", "Space"));
    }

    // Update is called once per frame
    void Update()
    {
    #region Mouse
        float mY = Input.GetAxis("Mouse Y");
        float mX = Input.GetAxis("Mouse X");

        camRot -= mY * sens;
        camRot = Mathf.Clamp(camRot, -90, 90);

        camTransform.localRotation = Quaternion.Euler(camRot, 0, 0);
        bodyTransform.Rotate(Vector3.up * mX * sens);
    #endregion

    #region Keyboard and Movement
        isGrounded = Physics.CheckSphere(groundCheck.position, radius, GroundMask);
        if (vel.y < 0 && isGrounded) vel.y = -2;
        if (Input.GetKey(backwardKey)) dirZ = -1;
        else if (Input.GetKeyUp(backwardKey)) dirZ = 0;
        else if (Input.GetKey(forwardKey)) dirZ = 1;
        else if (Input.GetKeyUp(forwardKey)) dirZ = 0;
        if (Input.GetKey(backwardKey) && Input.GetKey(forwardKey)) dirZ = 0;

        if (Input.GetKey(leftKey)) dirX = -1;
        else if (Input.GetKeyUp(leftKey)) dirX = 0;
        else if (Input.GetKey(rightKey)) dirX = 1;
        else if (Input.GetKeyUp(rightKey)) dirX = 0;
        if (Input.GetKey(leftKey) && Input.GetKey(rightKey)) dirX = 0;

        Vector3 move = transform.right * dirX + transform.forward * dirZ;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetKey(jumpKey) && isGrounded)
        {
            vel.y = Mathf.Sqrt(jumpPower * -2f * g);
        }

        vel.y += g * Time.deltaTime * 3;

        controller.Move(vel * Time.deltaTime);
    #endregion
    }
}
