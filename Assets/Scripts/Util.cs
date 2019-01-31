using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util : MonoBehaviour {

	public const float NEGLIGIBLE = 1e-2f;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		
	}

	public static bool Approx(float a, float b) {
		return (Mathf.Abs(a - b) < NEGLIGIBLE);
	}

	public static bool Approx(Vector2 a, Vector2 b) {
		return Approx(Vector2.Distance(a, b), 0f);
	}

	public static bool Between(int x, int min, int max) {
		Debug.Assert(min < max);
		return (x >= min && x < max);
	}

	public static bool Between(float x, float min, float max) {
		Debug.Assert(min < max);
		return (x >= min && x < max);
	}

	public static string GetNewGuid() {
		Guid g = Guid.NewGuid();
		return g.ToString();
	}

	public static string GetUniqueName(string ClassName) {
		return String.Format("{0} [{1}]", ClassName, GetNewGuid());
	}

	public static void DisplayList<T>(List<T> list) where T:MonoBehaviour {
		string names = "";
		foreach(T t in list) {
			names += (t.name + "\n");
		}
		Debug.Log("list: \n" + names);
	}

	public static T PickAtRandom<T>(List<T> options) {
		if(options.Count > 0) {			
			return options[UnityEngine.Random.Range(0, options.Count)];
		} else {
			return default(T);
		}
	}

	public static T PickWeightedRandom<T>(Dictionary<T, float> options, float sum = 0) {
		if(options.Count > 0) {
			// Calculate the sum of all option weights if necessary
			if(sum <= 0) {
				sum = 0;
				foreach(T key in options.Keys) {
					sum += options[key];
				}
			}
			// Pick a random value up to the sum and keep subtracting weights from it until it reaches zero
			float randomVal = UnityEngine.Random.Range(0f, sum);
			foreach(T key in options.Keys) {
				Debug.Assert(options[key] >= 0f);
				randomVal -= options[key];
				if(randomVal < 0) {
					return key;
				}
			}
			throw new ArgumentException("The given sum (" + sum + ") didn't match the contents of the options");
		} else {
			return default(T);
		}
	}
}
