using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwormController : MonoBehaviour {

	public const float UPDATE_INTERVAL = 0.5f;
	public const float SET_STRESS_TO = 0.9f;

	public int Length;
	public TileController Head;
	public LineRenderer Body;

	private bool isSlithering;

	private List<TileController> Segments;
	private Dictionary<TileController, int> SegmentLifetime;

	// Use this for initialization
	void Start() {
		Segments = new List<TileController>();
		SegmentLifetime = new Dictionary<TileController, int>();
	}

	public void Reset(int len, TileController start) {
		Debug.Assert(!isSlithering);
		Length = len;
		Head = start;
		isSlithering = true;
		StartCoroutine(Slither());
	}

	public void MoveToPool(Transform pool) {
		transform.SetParent(pool);
		transform.localPosition = Vector2.zero;
		Head = null;
		Body.enabled = false;
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	void FixedUpdate() {
		if(SegmentLifetime != null && isSlithering) {
			foreach(TileController t in SegmentLifetime.Keys) {
				t.StressLevel = Mathf.Max(SET_STRESS_TO, t.StressLevel);
			}
		}
	}

	private IEnumerator Slither() {
		yield return new WaitUntil(() => Segments != null && SegmentLifetime != null);
		Segments.Add(Head);
		SegmentLifetime.Add(Head, Length);
		float nextUpdateAt = Time.timeSinceLevelLoad;
		bool reachedFullLength = false, stopped = false;
		// Slither until the last segment dies
		while(SegmentLifetime.Count > 0) {
			// Wait a second
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > nextUpdateAt);
			// Update each segment
			foreach(TileController t in new List<TileController>(SegmentLifetime.Keys)) {
				SegmentLifetime[t] -= 1;
				if(SegmentLifetime[t] < 1) {
					Segments.Remove(t);
					SegmentLifetime.Remove(t);
				}
			}
			// Unless the head is at the edge of the board (i.e. has less than 5 neighbors, move it onwards, extending the sworm
			if(!stopped) {
				if(Head.myNeighbours.Count > 4) {
					TileController newHead = FindFreeTileForHead();
					if(newHead != null) {
						Segments.Add(newHead);
						SegmentLifetime.Add(newHead, Length);
						Head = newHead;
					}
					if(Segments.Count >= Length) {
						reachedFullLength = true;
					}
				} else {
					stopped = true;
				}
				// If the sworm stops at the edge before reaching full size, artificially shorten its lifespan
				if(stopped && !reachedFullLength && Segments.Count < Length) {
					foreach(TileController t in Segments) {
						SegmentLifetime[t] -= Length - Segments.Count - 1;
					}
					Length = Segments.Count;
				}
			}

			// Update display
			if(Debug.isDebugBuild && Segments.Count > 1) {
				UpdateDisplay();
			}
			nextUpdateAt += UPDATE_INTERVAL;
		}
		// Stop slithering after the entire sworm is dead
		isSlithering = false;
		GameObject.FindGameObjectWithTag("GameController").SendMessage("OnSwormEliminated", this);
	}

	private TileController FindFreeTileForHead() {
		List<TileController> freeTiles = new List<TileController>();
		foreach(TileController t in Head.myNeighbours) {
			if(!Segments.Contains(t) && t.CompareTag("Tile")) {
				freeTiles.Add(t);
			}
		}
		if(freeTiles.Count > 0) {
			return freeTiles[UnityEngine.Random.Range(0, freeTiles.Count)];
		} else {
			return null;
		}
	}

	private void UpdateDisplay() {
		Debug.Assert(Segments.Count > 1);
		Vector3[] pathPositions = new Vector3[Segments.Count];
		for(int i = 0; i < Segments.Count; i++) {
			pathPositions[i] = Segments[i].transform.position - transform.position;	
		}
		// Adapt the line
		Body.enabled = true;
		Body.positionCount = Segments.Count;
		Body.SetPositions(pathPositions);
	}
}
