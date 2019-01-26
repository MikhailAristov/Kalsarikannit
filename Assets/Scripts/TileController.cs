using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour {

	public const float WIDTH = 1.0f;
	public const float HORIZONTAL_SPACING = 0.75f * WIDTH;
	public const float VERTICAL_SPACING = 0.866025403f * WIDTH; // magic number = sqrt(3)/2

	[Range(-1f, 1f)]
	public float StressLevel = 0f;
	private Color TargetColor;

	public SpriteRenderer mySprite;
	public Transform myCenter;
	public TileController Home;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		// Find the home tile
		if(Home == null) {
			Home = GameObject.FindGameObjectWithTag("Respawn").GetComponent<TileController>(); // not NullRef secure, but who cares
		}

		mySprite.color = Color.Lerp(mySprite.color, TargetColor, 0.1f);
	}

	void FixedUpdate() {
		TargetColor = Color.HSVToRGB(stressToHue(StressLevel), 0.8f, 0.8f);
	}

	private float stressToHue(float stress) {
		// Linear mapping from [-1,1] to [2/3,0]
		return (1f - stress) / 3f;
	}
}
