using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCam : MonoBehaviour
{
    [SerializeField]
    float flightSpeed = 100.0f;
    [SerializeField]
    float Sensitivity = 2.0f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.Minus)) 
        {
            flightSpeed -= 1.0f;
        }

        if(Input.GetKey(KeyCode.Equals)) 
        {
            flightSpeed += 1.0f;
        }

        flightSpeed = Mathf.Clamp(flightSpeed, 1.0f, 1000.0f);

        float forward = Input.GetAxis("Forward");
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        transform.Translate(flightSpeed * Time.deltaTime * new Vector3(horizontal, vertical, forward), transform);

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        transform.eulerAngles += new Vector3(-mouseY * Sensitivity, mouseX * Sensitivity, 0);
        Mathf.Clamp(transform.rotation.x, -Mathf.PI/2, Mathf.PI/2);
    }
}
