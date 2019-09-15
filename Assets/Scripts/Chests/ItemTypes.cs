using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PowerUps
{
	Spyglass,
	PowderKeg,
	CannonShot,
	LemonJuice,
	WindBucket
}

public class Powerups
{
	//	const int SPYGLASS = 0;
	//	const int POWDER_KEG = 1;
	//	const int CANNON_SHOT = 2;
	//	const int LEMON_JUICE = 3;
	//	const int WIND_BUCKET = 4;

	public static int[] ToIntArray (PowerUps[] powerups)
	{
		return Array.ConvertAll (powerups, it => (int)it);
	}

	public static string ToString(PowerUps powerup) {
		switch (powerup) {
		case PowerUps.Spyglass:
			return "Spyglass O' Fortune";
		case PowerUps.PowderKeg:
			return "Powder Deg O' Doom";
		case PowerUps.CannonShot:
			return "Cannonshot O' Peril";
		case PowerUps.LemonJuice:
			return "Lemon Jice O' Health";
		case PowerUps.WindBucket:
			return "Bag O' Wind";
		default:
			return "";
		}
	}

	public static string ToString(int powerup) {
		return Powerups.ToString((PowerUps)powerup);
	}
}