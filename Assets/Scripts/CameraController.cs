using System;
using System.IO;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public PlayerController Player;
	public Camera ThisCamera;

	private const float MOUSE_SCROLL_MARGIN = 0.1f;
	private Vector2 HalfScreenSize;
	private Vector2 MaxDistanceToPlayer;
	private bool FollowingPlayer;

	// Use this for initialization
	void Start() {
		HalfScreenSize = new Vector2(ThisCamera.orthographicSize * ThisCamera.aspect, ThisCamera.orthographicSize);
		MaxDistanceToPlayer = HalfScreenSize / 2f;
		FollowingPlayer = false;
		// Confine the cursor to the game window
		Cursor.lockState = CursorLockMode.Confined;
	}
	
	// Update is called once per frame
	void Update() {
		// Take a screenshot when prompted
		if(Input.GetKeyUp(KeyCode.Space)) {
			takeScreenshot();
		}

		if(FollowingPlayer) {
			return;
		}

		// Move the camera to follow the player if necessary
		Vector2 PlayerRelativePosition = Player.transform.position - transform.position;
		if(Mathf.Abs(PlayerRelativePosition.x) > MaxDistanceToPlayer.x || Mathf.Abs(PlayerRelativePosition.y) > MaxDistanceToPlayer.y) {
			transform.position = Vector3.Lerp(transform.position, new Vector3(Player.transform.position.x, Player.transform.position.y, transform.position.z), 0.5f * Time.deltaTime);
		}

		// Follow the mouse cursor
		Vector3 MouseRelativePosition = ThisCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)) - transform.position;
		if((MouseRelativePosition.x > HalfScreenSize.x - MOUSE_SCROLL_MARGIN && PlayerRelativePosition.x > -MaxDistanceToPlayer.x + MOUSE_SCROLL_MARGIN)
			|| (MouseRelativePosition.x < -HalfScreenSize.x + MOUSE_SCROLL_MARGIN && PlayerRelativePosition.x < MaxDistanceToPlayer.x - MOUSE_SCROLL_MARGIN)
			|| (MouseRelativePosition.y > HalfScreenSize.y - MOUSE_SCROLL_MARGIN && PlayerRelativePosition.y > -MaxDistanceToPlayer.y + MOUSE_SCROLL_MARGIN)
			|| (MouseRelativePosition.y < -HalfScreenSize.y + MOUSE_SCROLL_MARGIN && PlayerRelativePosition.y < MaxDistanceToPlayer.y - MOUSE_SCROLL_MARGIN)) {
			transform.Translate(MouseRelativePosition * Time.deltaTime);
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

	public void FocusOnPlayer() {
		FollowingPlayer = true;
		StartCoroutine(MoveOverPlayerToken());
	}

	private IEnumerator MoveOverPlayerToken()  {
		while(Vector2.Distance(Player.transform.position, transform.position) > Util.NEGLIGIBLE) {
			transform.position = Vector3.Lerp(transform.position, new Vector3(Player.transform.position.x, Player.transform.position.y, transform.position.z), 0.5f * Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
	}
}
