using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetController : MonoBehaviour {

	public SpriteRenderer mySprite;
	public TileController CurrentTile;

	// How often, in seconds, does the target add to stress of its tile
	public const float STRESS_INCREASE_INTERVAL = 2.0f;
	private const float PULSE_EXPANSION_FACTOR = 1.5f;

	public float NextStressIncreaseIn = STRESS_INCREASE_INTERVAL;

	private bool isPulsing = false;
	private Vector2 initSpriteScale, pulsingSpriteScale;

	// Use this for initialization
	void Start() {
		initSpriteScale = mySprite.transform.localScale;
		pulsingSpriteScale = initSpriteScale * PULSE_EXPANSION_FACTOR;
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	public void PlaceOnTile(TileController target) {
		Debug.Assert(target != null);
		transform.parent = target.myCenter;
		transform.localPosition = Vector2.zero;
		CurrentTile = target;
		NextStressIncreaseIn = STRESS_INCREASE_INTERVAL;
	}

	public void MoveToPool(Transform pool) {
		transform.parent = pool;
		transform.localPosition = Vector2.zero;
		CurrentTile = null;
	}

	void FixedUpdate() {
		if(CurrentTile != null) {
			NextStressIncreaseIn -= Time.fixedDeltaTime;
			if(NextStressIncreaseIn < 0) {
				NextStressIncreaseIn += STRESS_INCREASE_INTERVAL;
				if(!isPulsing) {
					isPulsing = true;
					StartCoroutine(Pulse());
				}
			}
		}
	}

	private void IncreaseStress() {
		if(CurrentTile != null && !Util.Approx(CurrentTile.StressLevel, 1f)) {
			CurrentTile.StressLevel = Mathf.Lerp(CurrentTile.StressLevel, 1f, 0.1f * STRESS_INCREASE_INTERVAL);
		}
	}

	private IEnumerator Pulse() {
		float ExpansionStopAt = Time.timeSinceLevelLoad + STRESS_INCREASE_INTERVAL / 3f;
		float ContractionStopAt = Time.timeSinceLevelLoad + STRESS_INCREASE_INTERVAL * 2f / 3f;
		// Expand
		while(Time.timeSinceLevelLoad < ExpansionStopAt && Vector2.Distance(mySprite.transform.localScale, pulsingSpriteScale) > Util.NEGLIGIBLE) {
			mySprite.transform.localScale = Vector2.Lerp(mySprite.transform.localScale, pulsingSpriteScale, 5f * Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
		// Raise stress level
		IncreaseStress();
		// Contract
		while(Time.timeSinceLevelLoad < ContractionStopAt && Vector2.Distance(mySprite.transform.localScale, initSpriteScale) > Util.NEGLIGIBLE) {
			mySprite.transform.localScale = Vector2.Lerp(mySprite.transform.localScale, initSpriteScale, 3f * Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
		isPulsing = false;
	}
}
