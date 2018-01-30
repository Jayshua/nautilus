using UnityEngine;
using System.Collections;

public class TextureDraw
{
	public static void DrawLine(Texture2D tex, int aFromX, int aFromY, int aToX, int aToY, Color col)
	{
		int dy = (int)(aToY - aFromY);
		int dx = (int)(aToX - aFromX);
		int stepx, stepy;

		if (dy < 0) { dy = -dy; stepy = -1; }
		else { stepy = 1; }
		if (dx < 0) { dx = -dx; stepx = -1; }
		else { stepx = 1; }
		dy <<= 1;
		dx <<= 1;

		float fraction = 0;

		tex.SetPixel(aFromX, aFromY, col);
		if (dx > dy)
		{
			fraction = dy - (dx >> 1);
			while (Mathf.Abs(aFromX - aToX) > 1)
			{
				if (fraction >= 0)
				{
					aFromY += stepy;
					fraction -= dx;
				}
				aFromX += stepx;
				fraction += dy;
				tex.SetPixel(aFromX, aFromY, col);
			}
		}
		else
		{
			fraction = dx - (dy >> 1);
			while (Mathf.Abs(aFromY - aToY) > 1)
			{
				if (fraction >= 0)
				{
					aFromX += stepx;
					fraction -= dy;
				}
				aFromY += stepy;
				fraction += dx;
				tex.SetPixel(aFromX, aFromY, col);
			}
		}
	}

}
