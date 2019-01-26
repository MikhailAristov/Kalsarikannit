using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	public const float LOW_STRESS_THRESHOLD = -0.5f;
	private const float PULSE_EXPANSION_FACTOR = 1.5f;
	private const float PULSE_DURATION = 0.5f;

	[Range(-1f, 1f)]
	public float StressLevel = LOW_STRESS_THRESHOLD;

	public BoardController myBoard;
	public Transform mySprite;
	public TileController CurrentTile;
	public TileController TargetTile;
	private List<TileController> PathToTarget;

	private Vector2 initSpriteScale, pulsingSpriteScale;

	// Use this for initialization
	void Start() {
		myBoard = GameObject.FindGameObjectWithTag("GameController").GetComponent<BoardController>();
		initSpriteScale = mySprite.transform.localScale;
		pulsingSpriteScale = initSpriteScale * PULSE_EXPANSION_FACTOR;
		StartCoroutine(PulseWithStress());
		PathToTarget = new List<TileController>();
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	void FixedUpdate() {
		if(!Util.Approx(StressLevel, CurrentTile.EffectiveStressLevel)) {
			StressLevel = Mathf.Lerp(StressLevel, CurrentTile.EffectiveStressLevel, 1f * Time.fixedDeltaTime);
		}
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

	void OnMouseMovedOverTile(TileController tile) {
		if(tile != CurrentTile) {
			TargetTile = tile;
			RecalculatePathToTarget(tile);
			DisplayPathToTarget();
		}
	}

	void OnMouseMovedFromTile(TileController tile) {
		if(TargetTile = tile) {
			TargetTile = null;
		}
	}

	// Classic A*: https://en.wikipedia.org/wiki/A*_search_algorithm
	private void RecalculatePathToTarget(TileController goal) {
		List<TileController> closedSet = new List<TileController>();
		List<TileController> openSet = new List<TileController>() { CurrentTile };
		Dictionary<TileController, TileController> cameFrom = new Dictionary<TileController, TileController>();
		Dictionary<TileController, float> gScore = new Dictionary<TileController, float>();
		Dictionary<TileController, float> fScore = new Dictionary<TileController, float>();
		foreach(TileController t in myBoard.AllTiles) {
			gScore[t] = (t == CurrentTile) ? 0f : float.MaxValue;
			fScore[t] = (t == CurrentTile) ? Vector2.Distance(t.transform.localPosition, goal.transform.localPosition) : float.MaxValue;
		}
		while(openSet.Count > 0) {
			// Select lowest fScore for current
			TileController current = openSet[0];
			foreach(TileController t in openSet) {
				if(fScore[t] < fScore[current]) {
					current = t;
				}
			}
			if(current == goal) {
				PathToTarget.Clear();
				PathToTarget.Add(current);
				while(cameFrom.ContainsKey(current)) {
					current = cameFrom[current];
					PathToTarget.Insert(0, current);
				}
				return;
			}

			// Move current node
			openSet.Remove(current);
			closedSet.Add(current);
			// Expand neighbours
			foreach(TileController n in current.myNeighbours) {
				if(closedSet.Contains(n)) {
					continue;
				}
				float tentativeGScore = gScore[current] + TileController.VERTICAL_SPACING;
				if(!openSet.Contains(n)) {
					openSet.Add(n);
				} else if(tentativeGScore >= gScore[n]) {
					continue;
				}
				if(cameFrom.ContainsKey(n)) {
					cameFrom[n] = current;
				} else {
					cameFrom.Add(n, current);
				}
				gScore[n] = tentativeGScore;
				fScore[n] = tentativeGScore + Vector2.Distance(n.transform.localPosition, goal.transform.localPosition);
			}
		}
		Debug.LogError("No path to target found!");
	}

	private void DisplayPathToTarget() {
		Util.DisplayList(PathToTarget);
	}
}
