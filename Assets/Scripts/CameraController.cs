using System;
using System.IO;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public PlayerController Player;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		if(!Util.Approx(transform.position, Player.transform.position)) {
			transform.position = Vector3.Lerp(transform.position, new Vector3(Player.transform.position.x, Player.transform.position.y, transform.position.z), 5f * Time.deltaTime);
		}

		if(Input.GetKeyUp(KeyCode.Space)) {
			takeScreenshot();
		}
	}

	// Stores a screenshot to My Pictures
	public void takeScreenshot() {
		// Create directory if necessary
		string targetDir = String.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.productName);
		if(!Directory.Exists(targetDir)) {
			Directory.CreateDirectory(targetDir);
		}
		// Take and store the screenshot
		string filepath = String.Format("{0}\\screenshot_{1:yyyyMMddHHmmssfff}.png", targetDir, System.DateTime.Now);
		ScreenCapture.CaptureScreenshot(filepath);
		Debug.LogFormat("Screenshot saved to {0}.", filepath);
	}
}
