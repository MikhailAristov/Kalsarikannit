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
	public LineRenderer PathDisplay;

	private Vector2 initSpriteScale, pulsingSpriteScale;

	private bool isMoving;
	private bool emergencyStop;

	public TargetController AdjacentStressFactor;

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
		if(isMoving && Input.GetMouseButtonUp(1)) {
			emergencyStop = true;
			PathDisplay.enabled = false;
		} 

		if(!isMoving && Input.GetMouseButtonUp(0)) {
			isMoving = true;
			StartCoroutine(FollowPathToTarget());
		}
	}

	void FixedUpdate() {
		if(!Util.Approx(StressLevel, CurrentTile.EffectiveStressLevel)) {
			// More stress is easier than less...
			if(CurrentTile.EffectiveStressLevel < Mathf.Min(StressLevel, 0) && CurrentTile.CompareTag("Respawn")) {
				StressLevel = Mathf.Lerp(StressLevel, CurrentTile.EffectiveStressLevel, 0.5f * Time.fixedDeltaTime);
			} else if(CurrentTile.EffectiveStressLevel > Mathf.Max(StressLevel, 0)) {
				StressLevel = Mathf.Lerp(StressLevel, CurrentTile.EffectiveStressLevel, 3f * Time.fixedDeltaTime);
			}
		}

		if(!isMoving && AdjacentStressFactor != null && AdjacentStressFactor.CurrentTile == CurrentTile) {
			AdjacentStressFactor.Reduce(Time.fixedDeltaTime);
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
		if(!isMoving && tile != CurrentTile) {
			TargetTile = tile;
			RecalculatePathToTarget(tile);
			DisplayPathToTarget();
		}
	}

	void OnMouseMovedFromTile(TileController tile) {
		if(!isMoving && TargetTile == tile) {
			TargetTile = null;
			HidePathToTarget();
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
				fScore[n] = tentativeGScore + Mathf.Max(TileController.VERTICAL_SPACING, Vector2.Distance(n.transform.localPosition, goal.transform.localPosition));
			}
		}
		Debug.LogError("No path to target found!");
	}

	private void DisplayPathToTarget() {
		Debug.Assert(PathToTarget.Count > 1);
		Vector3[] pathPositions = new Vector3[PathToTarget.Count];
		for(int i = 0; i < PathToTarget.Count; i++) {
			pathPositions[i] = PathToTarget[i].transform.position - transform.position;	
		}
		// Adapt the line
		PathDisplay.enabled = true;
		PathDisplay.positionCount = PathToTarget.Count;
		PathDisplay.SetPositions(pathPositions);
	}

	private void HidePathToTarget() {
		PathDisplay.enabled = false;
	}

	private IEnumerator FollowPathToTarget() {
		if(PathToTarget.Count > 1 && PathDisplay.enabled) {
			// Stabilize path while moving
			PathDisplay.transform.SetParent(transform.parent);
			// Jump across tiles
			for(int i = 1; i < PathToTarget.Count; i++) {
				// Clear adjacent stress factor
				AdjacentStressFactor = null;
				// Update my parent
				transform.SetParent(PathToTarget[i].myCenter);
				CurrentTile = PathToTarget[i];
				// Lerp to parent's local position zero
				while(transform.localPosition.magnitude > Util.NEGLIGIBLE) {
					transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, 5f * Time.deltaTime);
					yield return new WaitForEndOfFrame();
				}
				// If emergency stop requested, stop after the current jump
				if(emergencyStop) {
					break;
				}
			}
		}
		// Grab back the path display and hide it
		PathDisplay.transform.SetParent(transform);
		PathDisplay.transform.localPosition = Vector2.zero;
		PathDisplay.enabled = false;
		TargetTile = null;
		PathToTarget.Clear();
		// Check for stress factors
		AdjacentStressFactor = CurrentTile.myCenter.GetComponentInChildren<TargetController>();
		// Stop moving
		emergencyStop = false;
		isMoving = false;
	}
}
