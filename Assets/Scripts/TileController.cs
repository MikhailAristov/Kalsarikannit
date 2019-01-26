using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour {

	public const float WIDTH = 1.0f;
	public const float HORIZONTAL_SPACING = 0.75f * WIDTH;
	public const float VERTICAL_SPACING = 0.866025403f * WIDTH; // magic number = sqrt(3)/2

	public static float DistanceStressFactor = 1.0f;

	[Range(0f, 1f)]
	public float StressLevel = 0f;
	private float BaseStressLevel = 0f;
	private float EffectiveStressLevel = 0f;

	private float currentHue;
	private float TargetHue;
	private Color TargetColor;

	public SpriteRenderer mySprite;
	public Transform myCenter;
	public TileController Home;

	public float DistanceToHome {
		get { 
			return (Home != null) ? Vector2.Distance(transform.position, Home.transform.position) : 0f;
		}
	}

	// Use this for initialization
	void Start() {
		currentHue = getHue(mySprite.color);
	}

	// Update is called once per frame
	void Update() {
		// Find the home tile
		if(Home == null) {
			Home = GameObject.FindGameObjectWithTag("Respawn").GetComponent<TileController>(); // not NullRef secure, but who cares
		}

		if(!Util.Approx(currentHue, TargetHue)) {
			mySprite.color = Color.Lerp(mySprite.color, TargetColor, 0.1f);
			currentHue = getHue(mySprite.color);
		}
	}

	void FixedUpdate() {
		// Update stress level
		BaseStressLevel = 1f - 2f / (1f + DistanceStressFactor * DistanceToHome * DistanceToHome);
		EffectiveStressLevel = BaseStressLevel + (1f - BaseStressLevel) * StressLevel;
		TargetHue = stressToHue(EffectiveStressLevel);
		TargetColor = Color.HSVToRGB(TargetHue, 0.8f, 0.8f);
	}

	private float getHue(Color col) {
		float h, s, v;
		Color.RGBToHSV(col, out h, out s, out v);
		return h;
	}

	private float stressToHue(float stress) {
		// Linear mapping from [-1,1] to [2/3,0]
		return (1f - stress) / 3f;
	}
}
