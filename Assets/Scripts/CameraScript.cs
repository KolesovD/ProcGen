using System.Collections; 
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {

	private float camMoveSpeed = 3f;
	private float camMoveSpeedShift = 30f;
	private float camAngleSpeed = 150f;
	private float fixedUpdateTime = 0.02f;
	//private Quaternion startCamRotation;
	private float horizontalRotate;
	private float verticalRotate;


	// Use this for initialization
	void Start () {
		//startCamRotation = transform.rotation;
		Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update () {
		if ((Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0)) {
			horizontalRotate += camAngleSpeed * Input.GetAxis("Mouse X") * Time.deltaTime;
			verticalRotate -= camAngleSpeed * Input.GetAxis("Mouse Y") * Time.deltaTime;

			if (horizontalRotate > 360f) horizontalRotate -= 360f;
			else if (horizontalRotate < -360f) horizontalRotate += 360f;
			verticalRotate = Mathf.Clamp(verticalRotate, -80f, 80f);

			//Camera.current.transform.rot;
			//Quaternion.LookRotation
			//Quaternion rotateHorizontal = Quaternion.AngleAxis(horizontalRotate, transform.up);
			//Debug.Log("horizontalRotate: " + horizontalRotate);
			//Quaternion rotateVertical = Quaternion.AngleAxis(verticalRotate, transform.right);
			//Debug.Log("verticalRotate: " + verticalRotate);

			transform.rotation = /*Quaternion.Lerp (startCamRotation, */Quaternion.Euler(verticalRotate, horizontalRotate, 0)/*, 1)*/;
		}

		if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift)) {
			if (Input.GetKey (KeyCode.W))
				transform.position += transform.forward * camMoveSpeedShift * Time.deltaTime;
			if (Input.GetKey (KeyCode.S))
				transform.position += transform.forward * -camMoveSpeedShift * Time.deltaTime;
			if (Input.GetKey (KeyCode.A))
				transform.position += transform.right * -camMoveSpeedShift * Time.deltaTime;
			if (Input.GetKey (KeyCode.D))
				transform.position += transform.right * camMoveSpeedShift * Time.deltaTime;
		} else {
			if (Input.GetKey (KeyCode.W))
				transform.position += transform.forward * camMoveSpeed * Time.deltaTime;
			if (Input.GetKey (KeyCode.S))
				transform.position += transform.forward * -camMoveSpeed * Time.deltaTime;
			if (Input.GetKey (KeyCode.A))
				transform.position += transform.right * -camMoveSpeed * Time.deltaTime;
			if (Input.GetKey (KeyCode.D))
				transform.position += transform.right * camMoveSpeed * Time.deltaTime;
		}
	}
		
}
