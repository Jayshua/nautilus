using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LowPolyUtils
{
	/// <summary>
	/// Helper method to sample a raw file with floating point coordinates
	/// </summary>
	public static float SampleBilinear(float[,] rawFloats, float u, float v)
	{
		int xLength = rawFloats.GetLength(0);
		float x = u * xLength;
		int xPrev = Mathf.FloorToInt(x);
		float xLerp = x - xPrev;
		int xNext = xPrev + 1;

		if (xNext <= 0)
		{
			xPrev = 0;
			xNext = 1;
			xLerp = 0.0f;
		}

		if (xPrev >= xLength - 1)
		{
			xPrev = xLength - 2;
			xNext = xLength - 1;
			xLerp = 1.0f;
		}

		int yLength = rawFloats.GetLength(1);
		float y = v * yLength;
		int yPrev = Mathf.FloorToInt(y);
		float yLerp = y - yPrev;
		int yNext = yPrev + 1;

		if (yNext <= 0)
		{
			yPrev = 0;
			yNext = 1;
			yLerp = 0.0f;
		}

		if (yPrev >= yLength - 1)
		{
			yPrev = yLength - 2;
			yNext = yLength - 1;
			yLerp = 1.0f;
		}

		float prevY = Mathf.Lerp(rawFloats[xPrev, yPrev], rawFloats[xNext, yPrev], xLerp);
		float nextY = Mathf.Lerp(rawFloats[xPrev, yNext], rawFloats[xNext, yNext], xLerp);
		return Mathf.Lerp(prevY, nextY, yLerp);
	}

	/// <summary>
	/// Helper method to sample a raw file with floating point coordinates
	/// </summary>
	public static Vector3 SampleBilinear(Vector3[,] rawPoints, float u, float v)
	{
		int xLength = rawPoints.GetLength(0);
		float x = u * xLength;
		int xPrev = Mathf.FloorToInt(x);
		float xLerp = x - xPrev;
		int xNext = xPrev + 1;

		if (xNext <= 0)
		{
			xPrev = 0;
			xNext = 1;
			xLerp = 0.0f;
		}

		if (xPrev >= xLength - 1)
		{
			xPrev = xLength - 2;
			xNext = xLength - 1;
			xLerp = 1.0f;
		}

		int yLength = rawPoints.GetLength(1);
		float y = v * yLength;
		int yPrev = Mathf.FloorToInt(y);
		float yLerp = y - yPrev;
		int yNext = yPrev + 1;

		if (yNext <= 0)
		{
			yPrev = 0;
			yNext = 1;
			yLerp = 0.0f;
		}

		if (yPrev >= yLength - 1)
		{
			yPrev = yLength - 2;
			yNext = yLength - 1;
			yLerp = 1.0f;
		}

		Vector3 prevY = Vector3.Lerp(rawPoints[xPrev, yPrev], rawPoints[xNext, yPrev], xLerp);
		Vector3 nextY = Vector3.Lerp(rawPoints[xPrev, yNext], rawPoints[xNext, yNext], xLerp);
		return Vector3.Lerp(prevY, nextY, yLerp);
	}

	/// <summary>
	/// Helper method to sample a raw file with floating point coordinates
	/// </summary>
	public static Color SampleBilinear(Color[,] rawColors, float u, float v)
	{
		int xLength = rawColors.GetLength(0);
		float x = u * xLength;
		int xPrev = Mathf.FloorToInt(x);
		float xLerp = x - xPrev;
		int xNext = xPrev + 1;

		if (xNext <= 0)
		{
			xPrev = 0;
			xNext = 1;
			xLerp = 0.0f;
		}

		if (xPrev >= xLength - 1)
		{
			xPrev = xLength - 2;
			xNext = xLength - 1;
			xLerp = 1.0f;
		}

		int yLength = rawColors.GetLength(1);
		float y = v * yLength;
		int yPrev = Mathf.FloorToInt(y);
		float yLerp = y - yPrev;
		int yNext = yPrev + 1;

		if (yNext <= 0)
		{
			yPrev = 0;
			yNext = 1;
			yLerp = 0.0f;
		}

		if (yPrev >= yLength - 1)
		{
			yPrev = yLength - 2;
			yNext = yLength - 1;
			yLerp = 1.0f;
		}

		Color prevY = Color.Lerp(rawColors[xPrev, yPrev], rawColors[xNext, yPrev], xLerp);
		Color nextY = Color.Lerp(rawColors[xPrev, yNext], rawColors[xNext, yNext], xLerp);
		return Color.Lerp(prevY, nextY, yLerp);
	}

	/// <summary>
	/// Helper method to sample a raw file with floating point coordinates
	/// </summary>
	public static void SampleBilinear(float[,,] rawFloats, float u, float v, float[] outWeights)
	{
		int xLength = rawFloats.GetLength(0);
		float x = u * xLength;
		int xPrev = Mathf.FloorToInt(x);
		float xLerp = x - xPrev;
		int xNext = xPrev + 1;

		if (xNext <= 0)
		{
			xPrev = 0;
			xNext = 1;
			xLerp = 0.0f;
		}

		if (xPrev >= xLength - 1)
		{
			xPrev = xLength - 2;
			xNext = xLength - 1;
			xLerp = 1.0f;
		}

		int yLength = rawFloats.GetLength(1);
		float y = v * yLength;
		int yPrev = Mathf.FloorToInt(y);
		float yLerp = y - yPrev;
		int yNext = yPrev + 1;

		if (yNext <= 0) 
		{
			yPrev = 0;
			yNext = 1;
			yLerp = 0.0f;
		}

		if (yPrev >= yLength - 1)
		{
			yPrev = yLength - 2;
			yNext = yLength - 1;
			yLerp = 1.0f;
		}

		for (int i = 0; i < rawFloats.GetLength(2); ++i)
		{
			float prevY = Mathf.Lerp(rawFloats[xPrev, yPrev, i], rawFloats[xNext, yPrev, i], xLerp);
			float nextY = Mathf.Lerp(rawFloats[xPrev, yNext, i], rawFloats[xNext, yNext, i], xLerp);
			outWeights[i] = Mathf.Lerp(prevY, nextY, yLerp);
		}
	}
}
