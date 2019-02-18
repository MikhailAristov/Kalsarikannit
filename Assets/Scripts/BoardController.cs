using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {

	public bool ExpoMode = false;
	public const float TIME_BEFORE_GAME_RESET_IN_EXPO_MODE = 20f; // in seconds

	public Canvas ThankYou;
	public Transform Background;
	public GameObject TilePrefab;

	public GameObject StandingTargetPrefab;
	public Transform StandingTargetPool;

	public GameObject SwormPrefab;
	public Transform SwormPool;

	public PlayerController Player;
	public TileController HomeTile;

	public const int VERTICAL_SIZE = 27;

	public const int MAX_STANDING_STRESS_FACTORS = VERTICAL_SIZE * VERTICAL_SIZE / 40;
	private int currentStandingStressFactors = 0;
	public int StressFactorsEliminated = 0;

	public int EffectiveMaxStressFactors {
		get { 
			return Mathf.Max(0, MAX_STANDING_STRESS_FACTORS - (StressFactorsEliminated / 3));
		}
	}

	public float StressFactorProgress {
		get { 
			return (float)StressFactorsEliminated / (MAX_STANDING_STRESS_FACTORS * 3);
		}
	}

	public const int MAX_SWORMS = VERTICAL_SIZE * VERTICAL_SIZE / 60;
	private int currentSworms = 0;

	public List<TileController> AllTiles;

	// Use this for initialization
	void Start() {
		placeTiles(VERTICAL_SIZE);
		scaleTheBoard(VERTICAL_SIZE);

		takePlayerHome();

		StartCoroutine(ManageStandingStressFactors());
		StartCoroutine(ManageSworms());
	}
	
	// Update is called once per frame
	void Update() {
		if(Input.GetKeyUp(KeyCode.Escape)) {
			Application.Quit();
		}
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
					newTile.tag = "Home";
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

	private void scaleTheBoard(int DiameterCount) {
		float scaleFactor = TileController.VERTICAL_SPACING * (0.5f + DiameterCount);
		Background.transform.localScale = new Vector2(scaleFactor, scaleFactor);
		// Update basic stress level for tiles
		TileController.DistanceStressFactor = 30f / scaleFactor / scaleFactor;
	}

	private TileController GetRandomFreeTileWeightedByStress() {
		Dictionary<TileController, float> availableTiles = new Dictionary<TileController, float>();
		foreach(TileController t in AllTiles) {
			// Implicit: Not Home tile, either
			if(!t.IsOccupied && t.CompareTag("Tile")) {
				availableTiles.Add(t, t.EffectiveStressLevel + 1f);
			}
		}
		return Util.PickWeightedRandom(availableTiles);
	}

	private TileController GetRandomTileNotOnEdge() {
		List<TileController> availableTiles = new List<TileController>();
		foreach(TileController t in AllTiles) {
			// Implicit: Not Home tile, either
			if(!t.IsOnEdge && t.CompareTag("Tile")) {
				availableTiles.Add(t);
			}
		}
		return Util.PickAtRandom(availableTiles);
	}

	private void takePlayerHome() {
		Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
		Player.PlaceOnTile(HomeTile);
	}

	private IEnumerator ManageStandingStressFactors() {
		// Wait until player moves from its initial position
		yield return new WaitUntil(() => Player.HasMoved);
		// Start adding stress factors
		float nextStressFactorAt;
		while(EffectiveMaxStressFactors > 0) {
			// Wait until a new stress factor can be added
			yield return new WaitUntil(() => currentStandingStressFactors < EffectiveMaxStressFactors);
			// Update waiting time
			nextStressFactorAt = Time.timeSinceLevelLoad + Mathf.Max(5f, currentStandingStressFactors);
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > nextStressFactorAt);
			// Spawn a new stress factor
			spawnStandingStressFactor(GetRandomFreeTileWeightedByStress());
		}
	}

	private TargetController spawnStandingStressFactor(TileController onTile) {
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
		currentStandingStressFactors += 1;
		return ctrl;
	}

	void OnStressFactorEliminated(TargetController stressFactor) {
		stressFactor.MoveToPool(StandingTargetPool);
		currentStandingStressFactors -= 1;
		// Update base stress curve parameters
		StressFactorsEliminated += 1;
		if(StressFactorsEliminated > 2) {
			TileController.DistanceStressFactor *= (float)StressFactorsEliminated / (StressFactorsEliminated + 1);
		}
	}

	private IEnumerator ManageSworms() {
		// Wait until player moves from its initial position
		yield return new WaitUntil(() => Player.HasMoved);
		// Start adding sworms
		float nextSwormAt;
		while(EffectiveMaxStressFactors > 0) {
			// Wait until a new stress factor can be added
			yield return new WaitUntil(() => currentSworms < Mathf.Max(1, MAX_SWORMS - Mathf.Max(0, StressFactorsEliminated - 2)));
			// Update waiting time
			nextSwormAt = Time.timeSinceLevelLoad + Mathf.Max(3f, currentSworms);
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > nextSwormAt);
			// Spawn a new stress factor
			spawnSworm(Mathf.RoundToInt(Mathf.Sqrt(VERTICAL_SIZE)) + 1, GetRandomTileNotOnEdge());
		}
	}

	private SwormController spawnSworm(int Length, TileController onTile) {
		// Check if there are targets in the pool
		SwormController ctrl = SwormPool.GetComponentInChildren<SwormController>();
		// If nothing found, spawn a new target
		if(ctrl == null) {
			GameObject newSworm = GameObject.Instantiate(SwormPrefab);
			newSworm.name = Util.GetUniqueName("Sworm");
			ctrl = newSworm.GetComponent<SwormController>();
		}
		// Place on tile and return
		ctrl.transform.SetParent(transform);
		ctrl.Reset(Length, onTile);
		currentSworms += 1;
		return ctrl;
	}

	void OnSwormEliminated(SwormController sworm) {
		sworm.MoveToPool(SwormPool);
		currentSworms -= 1;
	}

	void OnPlayerAsleep() {
		ThankYou.gameObject.SetActive(true);
	}
}
