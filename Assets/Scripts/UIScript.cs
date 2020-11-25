using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour {

	public Camera playerCamera;

	public Text t_11;
	public Text t_12;
	public Text t_21;
	public Text t_22;
	public Text t_31;
	public Text t_32;
	public Text t_41;
	public Text t_42;

	public Transform b_11;
	public Transform b_12;
	public Transform b_21;
	public Transform b_22;
	public Transform b_31;
	public Transform b_32;
	public Transform b_41;
	public Transform b_42;

	private int[,] buildings = new int[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
	private List<int> spawnSample = new List<int>();
	private List<Point> points = new List<Point>();
	private bool cooldown;
	private bool spawning;
	private int spawnPointer;
	private float cooldownTime;
	private float startCooldownTime = 0.01f;
	private float radiusBetweenBuildings = 7f;

	private class Point {
		private float x, y;

		public Point() {
			x = 0;
			y = 0;
		}

		public Point(float newX, float newY) {
			x = newX;
			y = newY;
		}

		public float GetX() {
			return x;
		}
		public void SetX(float newX) {
			x = newX;
		}
		public float GetY() {
			return y;
		}
		public void SetY(float newY) {
			y = newY;
		}
	}

	// Use this for initialization
	void Start () {
		cooldown = false;
		spawning = false;
		spawnPointer = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (!cooldown) {
			if (spawning) {
				//Instantiate (b_12, new Vector3 (Camera.current.transform.position.x, 0, Camera.current.transform.position.z), Quaternion.Euler(-90f, 0f, 0f));
				//temp.transform.localScale.Set (0.2f, 0.2f, 0.2f);
				//temp.transform.localScale.Scale(new Vector3(0.2f, 0.2f, 0.2f));
				if (spawnPointer < spawnSample.Count) {
					SpawnObject (spawnSample [spawnPointer], points [spawnPointer].GetX (), points [spawnPointer].GetY ());
					spawnPointer++;
				} else
					spawning = false;
				StartCooldown ();
			} else if (Input.GetKey (KeyCode.F)) {
				points.Clear ();

				int numbersOfBuilding = spawnSample.Count;
				Debug.Log ("Number of buildings: " + numbersOfBuilding);
				int t = 2;
				if (numbersOfBuilding > 8)
					while (1 + Mathf.Pow (7, t) - 2 * Mathf.Pow (7, t - 2) < numbersOfBuilding)
						t++;
				else
					t = 1;

				float l = 1f;
				while ((l / 2 * Mathf.Sqrt (3 / (1 + Mathf.Pow (7, t)))) < radiusBetweenBuildings)
					l += 0.5f;
				//Debug.Log("L: " + l);
				points.Add (new Point (playerCamera.transform.position.x, playerCamera.transform.position.z));

				DrawGosper (playerCamera.transform.position.x, playerCamera.transform.position.z, l, 0, t, 0);
				//Debug.Log ("Capacity: " + points.Capacity);
				RestractPoints ();
				Debug.Log ("Points to build: " + points.Count);

				spawning = true;
			
				spawnPointer = 0;
				StartCooldown ();
			} else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey (KeyCode.Alpha1)) {
				buildings [0, 1]++;
				//buildings [0, 1] += 10;
				/*for (int i = 0; i < 10; i++)*/ spawnSample.Add (1);
				t_12.text = buildings [0, 1].ToString();
				StartCooldown ();
			} else if (Input.GetKey (KeyCode.Alpha1)) {
				buildings [0, 0]++;
				//buildings [0, 0] += 10;
				/*for (int i = 0; i < 10; i++)*/ spawnSample.Add (2);
				t_11.text = buildings [0, 0].ToString();
				StartCooldown ();
			} else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey (KeyCode.Alpha2)) {
				buildings [1, 1]++;
				//buildings [1, 1] += 10;
				/*for (int i = 0; i < 10; i++)*/ spawnSample.Add (3);
				t_22.text = buildings [1, 1].ToString();
				StartCooldown ();
			} else if (Input.GetKey (KeyCode.Alpha2)) {
				buildings [1, 0]++;
				//buildings [1, 0] += 10;
				/*for (int i = 0; i < 10; i++)*/ spawnSample.Add (4);
				t_21.text = buildings [1, 0].ToString();
				StartCooldown ();
			} else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey (KeyCode.Alpha3)) {
				buildings [2, 1]++;
				spawnSample.Add (5);
				t_32.text = buildings [2, 1].ToString();
				StartCooldown ();
			} else if (Input.GetKey (KeyCode.Alpha3)) {
				buildings [2, 0]++;
				spawnSample.Add (6);
				t_31.text = buildings [2, 0].ToString();
				StartCooldown ();
			} else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey (KeyCode.Alpha4)) {
				buildings [3, 1]++;
				spawnSample.Add (7);
				t_42.text = buildings [3, 1].ToString();
				StartCooldown ();
			} else if (Input.GetKey (KeyCode.Alpha4)) {
				buildings [3, 0]++;
				spawnSample.Add (8);
				t_41.text = buildings [3, 0].ToString();
				StartCooldown ();
			}
		} else {
			cooldownTime -= Time.deltaTime;
			if (cooldownTime <= 0) {
				cooldown = false;
				//Debug.Log ("Cooldown end");
			}
		}
	}

	void SpawnObject (int sample, float x, float y) {
		switch (sample) {
		case 1:
			Instantiate (b_11, new Vector3 (x, 0, y), new Quaternion ());
			//Debug.Log ("x: " + x + " z: " + y);
			break;
		case 2:
			Instantiate (b_12, new Vector3 (x, 0, y), Quaternion.Euler (-90f, 0f, 0f));
			//Debug.Log ("x: " + x + " z: " + y);
			break;
		case 3:
			Instantiate (b_21, new Vector3 (x, 0, y), new Quaternion ());
			//Debug.Log ("x: " + x + " z: " + y);
			break;
		case 4:
			Instantiate (b_22, new Vector3 (x, 0, y), new Quaternion ());
			//Debug.Log ("x: " + x + " z: " + y);
			break;
		default:
			//
			break;
		}
	}

	void StartCooldown () {
		cooldownTime = startCooldownTime;
		cooldown = true;
		//Debug.Log ("Cooldown start");
	}
	
	void DrawGosper(float x, float y, float l, float u, int t, int q) {
		if (t > 0) {
			if (q == 1) {
				//формулы построения
				x += l * Mathf.Cos (u);
				y -= l * Mathf.Sin (u);
				u += Mathf.PI;
			}
			u -= 2 * Mathf.PI / 19; //соединение линий
			l /= Mathf.Sqrt (7); //масштаб
			//функции рисования
			DrawGosperRef (ref x, ref y, l, u, t - 1, 0);
				
			DrawGosperRef (ref x, ref y, l, u + Mathf.PI / 3, t - 1, 1);
				
			DrawGosperRef (ref x, ref y, l, u + Mathf.PI, t - 1, 1);
				
			DrawGosperRef (ref x, ref y, l, u + 2 * Mathf.PI / 3, t - 1, 0);
				
			DrawGosperRef (ref x, ref y, l, u, t - 1, 0);
				
			DrawGosperRef (ref x, ref y, l, u, t - 1, 0);
				
			DrawGosperRef (ref x, ref y, l, u - Mathf.PI / 3, t - 1, 1);
		} else {
			points.Add(new Point((x + Mathf.Cos(u)*l), (y - Mathf.Sin(u)*l)));
			//Debug.Log ("x: " + (x + Mathf.Cos (u) * l) + " y: " + (y - Mathf.Sin (u) * l));
		}
	}
	
	void DrawGosperRef(ref float x, ref float y, float l, float u, int t, int q) {
		DrawGosper(x, y, l, u, t, q);
		x += l * Mathf.Cos(u);
		y -= l * Mathf.Sin(u);
	}

	/*IEnumerator*/ void RestractPoints() {
		for (int i = 0; i < points.Count; i++) {
			for (int j = i + 1; j < points.Count; j++) {
				if ((Mathf.Abs (points [i].GetX () - points [j].GetX ()) < 1) && (Mathf.Abs (points [i].GetY () - points [j].GetY ()) < 1)) {
					points.RemoveAt (j);
					j--;
				}
				//yield return new WaitForFixedUpdate();
			}
		}
	}
}
