using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {

	public Transform Background;
	public GameObject TilePrefab;

	public GameObject StandingTargetPrefab;
	public Transform StandingTargetPool;

	public PlayerController Player;
	public TileController HomeTile;

	public const int VERTICAL_SIZE = 11;

	public List<TileController> AllTiles;

	// Use this for initialization
	void Start() {
		placeTiles(VERTICAL_SIZE);
		scaleBackground(VERTICAL_SIZE);

		takePlayerHome();

		//spawnStandingTarget(getRandomFreeTile());
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	private void placeTiles(int DiameterCount) {
		Debug.Assert(DiameterCount % 2 == 1);
		// Place the first row
		int halfDiameterCount = DiameterCount / 2; // intentional cast to int, rounding down
		for(int x = -halfDiameterCount; x <= halfDiameterCount; x++) {
			// Calculate how many tiles in current column
			int yCount = DiameterCount - Mathf.Abs(x);
			float vertOffset = -0.5f * TileController.VERTICAL_SPACING * (yCount - 1);
			// Spawn tiles
			for(int y = 0; y < yCount; y++) {
				GameObject newTile = GameObject.Instantiate(TilePrefab);
				newTile.transform.SetParent(transform);
				newTile.transform.localPosition =
					new Vector2(TileController.HORIZONTAL_SPACING * x, vertOffset + TileController.VERTICAL_SPACING * y);
				// Manipulate names
				if(x == 0 && y == halfDiameterCount) {
					newTile.tag = "Respawn";
					newTile.name = "Home";
					HomeTile = newTile.GetComponent<TileController>();
				} else {
					newTile.name = Util.GetUniqueName("Tile");
				}
			}
		}
		AllTiles = new List<TileController>();
		AllTiles.AddRange(GetComponentsInChildren<TileController>());
	}

	private void scaleBackground(int DiameterCount) {
		float scaleFactor = TileController.VERTICAL_SPACING * (0.5f + DiameterCount);
		Background.transform.localScale = new Vector2(scaleFactor, scaleFactor);
		// Update basic stress level for tiles
		TileController.DistanceStressFactor = 30f / scaleFactor / scaleFactor;
	}

	private TileController getRandomFreeTile() {
		GameObject[] listOfTiles = GameObject.FindGameObjectsWithTag("Tile");
		TileController result = null;
		// Pick a random free result (endless loop negligible for large enough boards)
		do {
			result = listOfTiles[UnityEngine.Random.Range(0, listOfTiles.Length)].GetComponent<TileController>();
		} while(result.myCenter.childCount > 0);
		return result;
	}

	private TargetController spawnStandingTarget(TileController onTile) {
		// Check if there are targets in the pool
		TargetController ctrl = StandingTargetPool.GetComponentInChildren<TargetController>();
		// If nothing found, spawn a new target
		if(ctrl == null) {
			GameObject newTarget = GameObject.Instantiate(StandingTargetPrefab);
			newTarget.name = Util.GetUniqueName("Standing Target");
			ctrl = newTarget.GetComponent<TargetController>();
		}
		// Place on tile and return
		ctrl.PlaceOnTile(onTile);
		return ctrl;
	}

	private void takePlayerHome() {
		Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
		Player.PlaceOnTile(HomeTile);
	}
}
