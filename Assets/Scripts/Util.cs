﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util : MonoBehaviour {

	public const float NEGLIGIBLE = 1e-5f;

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

	public static string GetNewGuid() {
		Guid g = Guid.NewGuid();
		return g.ToString();
	}
}
