﻿using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OnionRing
{
	public class TextureSlicer
	{
		public static SlicedTexture Slice(Texture2D texture)
		{
			var slicer = new TextureSlicer(texture);
			return slicer.Slice();
		}
		
		private Texture2D texture;
		private int width;
		private int height;
		private int[] pixels;
		private readonly int safetyMargin = 2;
		private readonly int margin = 2;
		
		private TextureSlicer(Texture2D texture)
		{
			this.texture = texture;
			this.width = texture.width;
			this.height = texture.height;
			this.pixels = texture.GetPixels().Select((color) => { return color.a > 0 ? color.GetHashCode() : 0; }).ToArray();
		}
		
		private void CalcLine(List<ulong> list, out int start, out int end)
		{
			start = 0;
			end = 0;
			int tmpStart = 0;
			int tmpEnd = 0;
			ulong tmpHash = list[0];
			for(int i=0;i<list.Count;++i)
			{
				if(tmpHash == list[i])
				{
					tmpEnd = i;
				}
				else
				{
					if(end - start < tmpEnd - tmpStart)
					{
						start = tmpStart;
						end = tmpEnd;
					}
					tmpStart = i;
					tmpEnd = i;
					tmpHash = list[i];
				}
			}
			if(end - start < tmpEnd - tmpStart)
			{
				start = tmpStart;
				end = tmpEnd;
			}
			
			end -= (safetyMargin*2 + margin);
			if(end < start)
			{
				start = 0;
				end = 0;
			}
		}
		
		private static List<ulong> CreateHashList(int aMax, int bMax, Func<int, int, int> f)
		{
			var hashList = new List<ulong>();
			for(int a=0;a<aMax;++a)
			{
				ulong line = 0;
				for(int b=0;b<bMax;++b) line = line + (ulong)(f(a, b) * b);
				hashList.Add(line);
			}
			return hashList;
		}
		
		private SlicedTexture Slice()
		{
			int xStart, xEnd;
			{
				var hashList = CreateHashList(width, height, (x, y) => { return Get(x, y); });
				CalcLine(hashList, out xStart, out xEnd);
			}
			
			int yStart, yEnd;
			{
				var hashList = CreateHashList(height, width, (y, x) => { return Get(x, y); });
				CalcLine(hashList, out yStart, out yEnd);
			}
			
			bool skipX = false;
			if(xEnd - xStart < 2)
			{
				skipX = true;
				xStart = 0;
				xEnd = 0;
			}
			
			bool skipY = false;
			if(yEnd - yStart < 2)
			{
				skipY = true;
				yStart = 0;
				yEnd = 0;
			}
			var output = GenerateSlicedTexture(xStart, xEnd, yStart, yEnd);
			int left = xStart + safetyMargin;
			int bottom = yStart + safetyMargin;
			int right = width-xEnd - safetyMargin - margin;
			int top = height-yEnd - safetyMargin - margin;
			if(skipX)
			{
				left = 0;
				right = 0;
			}
			if(skipY)
			{
				top = 0;
				bottom = 0;
			}
			return new SlicedTexture(output, new Boarder(left, bottom, right, top));
		}
		
		private Texture2D GenerateSlicedTexture(int xStart, int xEnd, int yStart, int yEnd)
		{
			var outputWidth = width - (xEnd - xStart);
			var outputHeight = height - (yEnd - yStart);
			var originalPixels = texture.GetPixels();
			var outputPixels = new Color[outputWidth * outputHeight];
			for(int x=0, originalX=0;x<outputWidth;++x,++originalX)
			{
				if(originalX == xStart) originalX += (xEnd - xStart);
				for(int y=0, originalY=0;y<outputHeight;++y,++originalY)
				{
					if(originalY == yStart) originalY += (yEnd - yStart);
					outputPixels[y * outputWidth + x] = originalPixels[originalY * width + originalX];
				}
			}
			var output = new Texture2D(outputWidth, outputHeight);
			output.SetPixels(outputPixels);
			return output;
		}
		
		private int Get(int x, int y)
		{
			return pixels[y * width + x];
		}
	}
	
	public class SlicedTexture
	{
		public SlicedTexture(Texture2D texture, Boarder boarder)
		{
			Texture = texture;
			Boarder = boarder;
		}
		
		public Texture2D Texture { get; private set; }
		public Boarder Boarder { get; private set; }
	}
	
	public class Boarder
	{
		public Boarder(int left, int bottom, int right, int top)
		{
			Left = left;
			Bottom = bottom;
			Right = right;
			Top = top;
		}
		
		public Vector4 ToVector4() { return new Vector4(Left, Bottom, Right, Top); }
		
		public int Left { get; private set; }
		public int Bottom { get; private set; }
		public int Right { get; private set; }
		public int Top { get; private set; }
	}
}
