using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	public const float LOW_STRESS_THRESHOLD = -0.5f;
	private const float PULSE_EXPANSION_FACTOR = 1.5f;
	private const float PULSE_DURATION = 0.5f;

	[Range(-1f, 1f)]
	public float StressLevel = -0.5f;

	public Transform mySprite;
	public TileController CurrentTile;

	private Vector2 initSpriteScale, pulsingSpriteScale;

	// Use this for initialization
	void Start() {
		initSpriteScale = mySprite.transform.localScale;
		pulsingSpriteScale = initSpriteScale * PULSE_EXPANSION_FACTOR;
		StartCoroutine(PulseWithStress());
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	public void PlaceOnTile(TileController target) {
		Debug.Assert(target != null);
		transform.parent = target.myCenter;
		transform.localPosition = Vector2.zero;
		CurrentTile = target;
	}

	private IEnumerator PulseWithStress() {
		float lastPulseAt = Time.timeSinceLevelLoad;
		while(true) {
			// Don't pulsate at all on low stress
			yield return new WaitUntil(() => StressLevel > LOW_STRESS_THRESHOLD);
			// Calculate next pulse
			lastPulseAt = Time.timeSinceLevelLoad;
			// Prepare pulse
			float ExpansionStopAt = Time.timeSinceLevelLoad + PULSE_DURATION / 2f;
			float ContractionStopAt = Time.timeSinceLevelLoad + PULSE_DURATION;
			// Expand
			while(Time.timeSinceLevelLoad < ExpansionStopAt && Vector2.Distance(mySprite.localScale, pulsingSpriteScale) > Util.NEGLIGIBLE) {
				mySprite.localScale = Vector2.Lerp(mySprite.localScale, pulsingSpriteScale, 10f * Time.deltaTime);
				yield return new WaitForEndOfFrame();
			}
			// Contract
			while(Time.timeSinceLevelLoad < ContractionStopAt && Vector2.Distance(mySprite.localScale, initSpriteScale) > Util.NEGLIGIBLE) {
				mySprite.localScale = Vector2.Lerp(mySprite.localScale, initSpriteScale, 5f * Time.deltaTime);
				yield return new WaitForEndOfFrame();
			}
			// Wait for next pulse
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > (lastPulseAt + 4f - 3.5f * StressLevel));
		}
	}
}
