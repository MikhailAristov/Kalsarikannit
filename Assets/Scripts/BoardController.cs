using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {

	public GameObject TilePrefab;
	public Transform Background;

	public const int VERTICAL_SIZE = 11;

	// Use this for initialization
	void Start() {
		placeTiles(VERTICAL_SIZE);
		scaleBackground(VERTICAL_SIZE);
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
				} else {
					newTile.name = "Tile [" + Util.GetNewGuid() + "]";
				}
			}
		}
	}

	private void scaleBackground(int DiameterCount) {
		float scaleFactor = TileController.VERTICAL_SPACING * (0.5f + DiameterCount);
		Background.transform.localScale = new Vector2(scaleFactor, scaleFactor);
		// Update basic stress level for tiles
		TileController.DistanceStressFactor = 30f / scaleFactor / scaleFactor;
	}
}
