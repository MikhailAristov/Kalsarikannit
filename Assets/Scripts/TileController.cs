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
	[Range(-1f, 1f)]
	public float EffectiveStressLevel = 0f;

	private float currentHue;
	private float TargetHue;
	private Color TargetColor;

	public SpriteRenderer mySprite;
	public Transform myCenter;
	public TileController Home;

	public List<TileController> myNeighbours;

	public float DistanceToHome {
		get { 
			return (Home != null) ? Vector2.Distance(transform.position, Home.transform.position) : 0f;
		}
	}

	// Use this for initialization
	void Start() {
		currentHue = getHue(mySprite.color);
		myNeighbours = new List<TileController>();
	}

	// Update is called once per frame
	void Update() {
		// Find the home tile
		if(Home == null) {
			Home = GameObject.FindGameObjectWithTag("Respawn").GetComponent<TileController>(); // not NullRef secure, but who cares
		}

		if(!Util.Approx(currentHue, TargetHue)) {
			mySprite.color = Color.Lerp(mySprite.color, TargetColor, 2f * Time.deltaTime);
			currentHue = getHue(mySprite.color);
		}
	}

	void FixedUpdate() {
		// Update stress level
		BaseStressLevel = 1f - 2f / (1f + DistanceStressFactor * DistanceToHome * DistanceToHome);
		// Update from neighbours
		if(myCenter.GetComponentsInChildren<TargetController>().Length == 0) {
			float stressFromNeighbour = 0.8f * GetMaxNeighbourStress();
			if(!Util.Approx(StressLevel, stressFromNeighbour)) {
				StressLevel = Mathf.Lerp(StressLevel, stressFromNeighbour, 100f * Time.fixedDeltaTime);
			}
		}
		// Update effective levels and colors
		EffectiveStressLevel = BaseStressLevel + (1f - BaseStressLevel) * StressLevel * (CompareTag("Respawn") ? 0.5f : 1f);
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

	private float GetMaxNeighbourStress() {
		float result = 0;
		if(myNeighbours.Count > 0) {
			foreach(TileController n in myNeighbours) {
				if(n.StressLevel > result) {
					result = n.StressLevel;
				}
			}
		}
		return result;
	}

	void OnTriggerEnter2D(Collider2D other) {
		// Try getting the tile controller
		TileController ctrl = other.GetComponent<TileController>();
		if(ctrl != null) {
			Debug.Assert(ctrl != this);
			myNeighbours.Add(ctrl);
		}
	}

	void OnTriggerExit2D(Collider2D other) {
		// Try getting the tile controller
		TileController ctrl = other.GetComponent<TileController>();
		if(ctrl != null && myNeighbours.Contains(ctrl)) {
			myNeighbours.Remove(ctrl);
		}
	}

	void OnMouseEnter() {
		GameObject.FindGameObjectWithTag("Player").SendMessage("OnMouseMovedOverTile", this);
	}

	void OnMouseDown() {
		GameObject.FindGameObjectWithTag("Player").SendMessage("OnMouseMovedOverTile", this);
	}

	void OnMouseExit() {
		GameObject.FindGameObjectWithTag("Player").SendMessage("OnMouseMovedFromTile", this);
	}
}
