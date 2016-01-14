using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft_Renderer
{
    static public class Kernel
    {





        public static string Source = @" 
 kernel void CalcShadow(
int width,
int height,
 
__global float* zBufferShadow,

float upper0,
float upper1,
float upper2,
float mid0,
float mid1,
float mid2,
float stepSizeLong0,
float stepSizeLong1,
float stepSizeLong2,
float stepSizeShortUp0,
float stepSizeShortUp1,
float stepSizeShortUp2,
float stepSizeShortDown0,
float stepSizeShortDown1,
float stepSizeShortDown2,

float lengthShortUpY,
int halfWidth,
int halfHeight



) {		
	int i = get_global_id(0);

	float longLineEnd[3];
	float shortLineEnd[3];

	longLineEnd[0] = upper0 - i * stepSizeLong0;
	longLineEnd[1] = upper1 - i * stepSizeLong1;
	longLineEnd[2] = upper2 - i * stepSizeLong2;

	if (i < lengthShortUpY)
	{
		shortLineEnd[0] = upper0 - i * stepSizeShortUp0;
		shortLineEnd[1] = upper1 - i * stepSizeShortUp1;
		shortLineEnd[2] = upper2 - i * stepSizeShortUp2;

	}
	else
	{

		float k = i - lengthShortUpY;

		shortLineEnd[0] = mid0 - k * stepSizeShortDown0;
		shortLineEnd[1] = mid1 - k * stepSizeShortDown1;
		shortLineEnd[2] = mid2 - k * stepSizeShortDown2;
	}


	//растеризация отрезка
	//======================================================

	float lengthX = fabs(longLineEnd[0] - shortLineEnd[0]);

	float step[3] = {
		(longLineEnd[0] - shortLineEnd[0]) / lengthX,
		(longLineEnd[1] - shortLineEnd[1]) / lengthX,
		(longLineEnd[2] - shortLineEnd[2]) / lengthX
	};


	float d[3];

	for (float m = 0.01f; m <= lengthX-0.01f; m++)
	{

		d[0] = longLineEnd[0] - m * step[0];
		d[1] = longLineEnd[1] - m * step[1];
		d[2] = longLineEnd[2] - m * step[2];


		int xInt = (int)(d[0] + 0.5f);
		int yInt = (int)(d[1] + 0.5f);


		if (xInt < halfWidth && yInt < halfHeight && xInt > -halfWidth && yInt > -halfHeight)
		{

			int frameX = xInt + halfWidth;
			int frameY = yInt + halfHeight;

			//шейдер
			//======================================================
			if (zBufferShadow[frameX*height+frameY] < (d[2]+0.01f))
			{
				zBufferShadow[frameX*height+frameY] = d[2];
			}
			//===========

		}
	}

	//===========
}





//===========//===========//===========
//===========//===========//===========
//===========//===========//===========






kernel void CalcLight(

int width,
int height,

__global float* zBufferShadow,

float upper0,
float upper1,
float upper2,
float mid0,
float mid1,
float mid2,
float stepSizeLong0,
float stepSizeLong1,
float stepSizeLong2,
float stepSizeShortUp0,
float stepSizeShortUp1,
float stepSizeShortUp2,
float stepSizeShortDown0,
float stepSizeShortDown1,
float stepSizeShortDown2,

float lengthShortUpY,
int halfWidth,
int halfHeight,

float lightIntensity,

__global float* rotateCoefs,
__global float* bufferLight,
__global float* zBuffer




) {		
	int i = get_global_id(0);

	float longLineEnd[3];
	float shortLineEnd[3];

	longLineEnd[0] = upper0 - i * stepSizeLong0;
	longLineEnd[1] = upper1 - i * stepSizeLong1;
	longLineEnd[2] = upper2 - i * stepSizeLong2;

	if (i < lengthShortUpY)
	{
		shortLineEnd[0] = upper0 - i * stepSizeShortUp0;
		shortLineEnd[1] = upper1 - i * stepSizeShortUp1;
		shortLineEnd[2] = upper2 - i * stepSizeShortUp2;

	}
	else
	{

		float k = i - lengthShortUpY;

		shortLineEnd[0] = mid0 - k * stepSizeShortDown0;
		shortLineEnd[1] = mid1 - k * stepSizeShortDown1;
		shortLineEnd[2] = mid2 - k * stepSizeShortDown2;
	}


	//растеризация отрезка
	//======================================================

	float lengthX = fabs(longLineEnd[0] - shortLineEnd[0]);

	float step[3] = {
		(longLineEnd[0] - shortLineEnd[0]) / lengthX,
		(longLineEnd[1] - shortLineEnd[1]) / lengthX,
		(longLineEnd[2] - shortLineEnd[2]) / lengthX
	};


	float d[3];

	for (float m = 0.01f; m <= lengthX-0.01f; m++)
	{

		d[0] = longLineEnd[0] - m * step[0];
		d[1] = longLineEnd[1] - m * step[1];
		d[2] = longLineEnd[2] - m * step[2];


		int xInt = (int)(d[0] + 0.5f);
		int yInt = (int)(d[1] + 0.5f);


		if (xInt < halfWidth && yInt < halfHeight && xInt > -halfWidth && yInt > -halfHeight)
		{

			int frameX = xInt + halfWidth;
			int frameY = yInt + halfHeight;


			//шейдер
			//======================================================
			if (zBuffer[frameX*height+frameY] <= (d[2]+1))
					{

						float rotated[3] = {
							rotateCoefs[0] * d[0] + rotateCoefs[1] * d[1] + rotateCoefs[2] * d[2],
							rotateCoefs[3] * d[0] + rotateCoefs[4] * d[1] + rotateCoefs[5] * d[2],
							rotateCoefs[6] * d[0] + rotateCoefs[7] * d[1] + rotateCoefs[8] * d[2]
						};


						int xIntShadow = (int)(rotated[0] + 0.5f);
						int yIntShadow = (int)(rotated[1] + 0.5f);


						if (xIntShadow < halfWidth && yIntShadow < halfHeight && xIntShadow > -halfWidth && yIntShadow > -halfHeight &&
								(rotated[2]) >= zBufferShadow[(xIntShadow + halfWidth)* height + (yIntShadow + halfHeight)]) 
						{
							bufferLight[frameX*height+frameY] += lightIntensity;
						}

					}
			//===========

		}
	}

	//===========
}
";

    }
}
