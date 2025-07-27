using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

using NUnit.Framework.Constraints;

using UnityEditor;

public class MyGraphics
{
	public Bitmap Image;
	public void DrawImage(Bitmap img, Rectangle dest, Rectangle src, GraphicsUnit graphicsUnit)
	{
		for (var h = 0; h < src.Height; h++)
		{
			for (var w = 0; w < src.Width; w++)
			{
				Image.SetPixel(dest.X + w, dest.Y + h, img.GetPixel(w + src.X, h + src.Y));
			}
		}
	}
}

[UnityEngine.CreateAssetMenu(fileName = "TextureOptimizer")]
public class TextureOptimizer : UnityEngine.ScriptableObject
{
	public UnityEngine.Texture texture;

	public void Optimize()
	{
		var pngPath = AssetDatabase.GetAssetPath(texture);
		Optimize(pngPath);
	}

	public void ClipEmpty()
	{
		var pngPath = AssetDatabase.GetAssetPath(texture);
		ClipEmpty(pngPath);
	}

	public static int GetOptimizedSize(int len)
	{
		if (len == 1)
		{
			return len;
		}
		else if (len <= 0)
		{
			return Math.Max(len, 0);
		}
		if (len % 4 != 0)
		{
			return len + (4 - (4 + len) % 4);
		}
		return len;
	}
	public static bool IsEmpty(Color color)
	{
		return color.R == 0 && color.G == 0 && color.B == 0 && color.A == 0;
	}
	public static Rectangle GetValidContentSize(Bitmap bitmap)
	{
		var width = bitmap.Width;
		var height = bitmap.Height;
		int xl = width;
		int xr = -1;
		int yb = height;
		int yt = -1;
		for (var h = 0; h < height; h++)
		{
			int w;
			for (w = 0; w <= Math.Min(xl,width-1); w++)
			{
				var p = bitmap.GetPixel(w, h);
				if (!IsEmpty(p))
				{
					xl = Math.Min(xl, w - 1);
					if (yb == height)
					{
						yb = h - 1;
					}
					yt = Math.Max(yt, h + 1);
					xr = Math.Max(xr, w + 1);
					break;
				}
			}
			for (var w2 = width - 1; w2 >= Math.Max(xr,0); w2--)
			{
				var p = bitmap.GetPixel(w2, h);
				if (!IsEmpty(p))
				{
					xr = Math.Max(xr, w2 + 1);
					yt = Math.Max(yt, h + 1);
					break;
				}
			}

			if (yt != h + 1)
			{
				var p = bitmap.GetPixel((xl + xr) / 2, h);
				if (!IsEmpty(p))
				{
					yt = h + 1;
				}
			}
		}
		bool found = false;
		for (var h = height - 1; h >= yt; h--)
		{
			for (var w = xl + 1; w < xr; w++)
			{
				var p = bitmap.GetPixel(w, h);
				if (!IsEmpty(p))
				{
					yt = h + 1;
					found = true;
					break;
				}
			}
			if (found)
			{
				break;
			}
		}

		return new Rectangle(xl + 1, yb + 1, xr - xl - 1, yt - yb - 1);
	}

	/// <summary>
	/// 优化纹理格式
	/// </summary>
	/// <param name="pngPath">图片路径</param>
	/// <param name="around">采取环绕约束方式</param>
	public static void Optimize(string pngPath, bool around = true)
	{
		optimize(pngPath, false, around);
	}
	/// <summary>
	/// 裁切纹理空白
	/// </summary>
	/// <param name="pngPath">图片路径</param>
	public static void ClipEmpty(string pngPath)
	{
		optimize(pngPath, true);
	}
	/// <summary>
	/// 优化纹理格式
	/// </summary>
	/// <param name="pngPath">图片路径</param>
	/// <param name="clipEmpty">是否去除空像素点</param>
	/// <param name="around">采取环绕约束方式</param>
	private static void optimize(string pngPath, bool clipEmpty, bool around = true)
	{
		{
			using var imgRaw = Image.FromFile(pngPath);
			using var bitRaw = new Bitmap(imgRaw);

			using Bitmap bitMap = optimize(bitRaw, out _, clipEmpty, around);

			var tempPath = FileUtil.GetUniqueTempPathInProject();
			bitMap.Save(tempPath, ImageFormat.Png);
			bitMap.Dispose();

			imgRaw.Dispose();
			bitRaw.Dispose();

			File.Copy(tempPath, pngPath, true);
			File.Delete(tempPath);
		}

		AssetDatabase.Refresh();
	}
	private static Bitmap optimize(Bitmap bitRaw, out Rectangle opRect, bool clipEmpty, bool around = true)
	{
		var rect = GetValidContentSize(bitRaw);
		int opWidth;
		int opHeight;
		float startX;
		float startY;
		if (clipEmpty)
		{
			opWidth = GetOptimizedSize(rect.Width);
			opHeight = GetOptimizedSize(rect.Height);
			startX = -(rect.X - (opWidth - rect.Width) / 2.0f);
			startY = -(rect.Y - (opHeight - rect.Height) / 2.0f);
		}
		else if (around)
		{
			opWidth = GetOptimizedSize(rect.Width);
			opHeight = GetOptimizedSize(rect.Height);
			if (bitRaw.Width - opWidth >= 4)
			{
				opWidth = GetOptimizedSize(bitRaw.Width - 4);
				startX = (opWidth - bitRaw.Width) / 2.0f;
			}
			else
			{
				startX = -(rect.X - (opWidth - rect.Width) / 2.0f);
			}
			if (bitRaw.Height - opHeight >= 4)
			{
				opHeight = GetOptimizedSize(bitRaw.Height - 4);
				startY = (opHeight - bitRaw.Height) / 2.0f;
			}
			else
			{
				startY = -(rect.Y - (opHeight - rect.Height) / 2.0f);
			}
		}
		else
		{
			opWidth = GetOptimizedSize(bitRaw.Width);
			opHeight = GetOptimizedSize(bitRaw.Height);
			startX = (opWidth - bitRaw.Width) / 2.0f;
			startY = (opHeight - bitRaw.Height) / 2.0f;
		}
		if (startX == 0 && startY == 0 && opWidth == bitRaw.Width && opHeight == bitRaw.Height)
		{
			// 无需优化
			opRect = new Rectangle(0, 0, bitRaw.Width, bitRaw.Height);
			return bitRaw;
		}

		Bitmap bitMap = new Bitmap(opWidth, opHeight);
		using Graphics g = Graphics.FromImage(bitMap);
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.SmoothingMode = SmoothingMode.HighQuality;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;

		g.DrawImage(bitRaw, (int)startX, (int)startY, bitRaw.Width, bitRaw.Height);
		g.Dispose();

		opRect = new Rectangle(-(int)startX, -(int)startY, opWidth, opHeight);
		return bitMap;

	}
	public static void OptimizeSprite9(string pngPath)
	{
		{
			var importer = TextureImporter.GetAtPath(pngPath) as TextureImporter;
			var border = importer.spriteBorder;

			using var imgRaw = Image.FromFile(pngPath);
			using var bitRaw = new Bitmap(imgRaw);
			var rect = GetValidContentSize(bitRaw);

			if (border.sqrMagnitude == 0)
			{
				var border2 = DetectScale9Rect(bitRaw, rect);
				if (border2.sqrMagnitude != 0)
				{
					border2.x += rect.X;
					border2.y += rect.Y;
					border2.z += (bitRaw.Width - rect.Right);
					border2.w += (bitRaw.Height - rect.Bottom);
					border = border2;
				}
				else
				{
					imgRaw.Dispose();
					bitRaw.Dispose();
					ClipEmpty(pngPath);
					return;
				}
			}

			int opWidth;
			int opHeight;

			var realWidth1 = (int)border.x - rect.Left;
			var realWidth2 = (int)border.z - (imgRaw.Width - rect.Right);
			if (realWidth1 <= 0)
			{
				if (realWidth2 <= 0)
				{
					realWidth1 = rect.Width / 2;
					realWidth2 = rect.Width - realWidth1;
				}
				else
				{
					realWidth1 = rect.Width - realWidth2;
				}
			}
			else if(realWidth2<=0)
			{
				realWidth2=rect.Width - realWidth1;
			}
			var realHeight1 = (int)border.w - rect.Top;
			var realHeight2 = (int)border.y - (imgRaw.Height - rect.Bottom);
			if (realHeight1 <= 0)
			{
				if (realHeight2 <= 0)
				{
					realHeight1 = rect.Height / 2;
					realHeight2 = rect.Height - realHeight1;
				}
				else
				{
					realHeight1 = rect.Height - realHeight2;
				}
			}
			else if(realHeight2<=0)
			{
				realHeight2 = rect.Height - realHeight1;
			}
			int contentWidth = realWidth1 + realWidth2;
			int contentHeight = realHeight1 + realHeight2;
			opWidth = contentWidth;//GetOptimizedSize(contentWidth);
			opHeight = contentHeight;//GetOptimizedSize(contentHeight);
			var dx = (opWidth - contentWidth) / 2;
			var dy = (opHeight - contentHeight) / 2;
			var opX = rect.X - dx;
			var opY = rect.Y - dy;
			var opRect = new Rectangle(opX, opY, rect.Width + opWidth - contentWidth, rect.Height + opHeight - contentHeight);

			var width1 = realWidth1 + dx;
			var height1 = realHeight1 + dy;
			var width2 = opWidth - width1;
			var height2 = opHeight - height1;

			using Bitmap bitMap = new Bitmap(width1 + width2, height1 + height2);
			//var g = new MyGraphics()
			//{
			//    Image = bitMap,
			//};
			var g = Graphics.FromImage(bitMap);
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			g.DrawImage(bitRaw, new Rectangle(dx, dy, realWidth1, realHeight1), new Rectangle(rect.X, rect.Y, realWidth1, realHeight1), GraphicsUnit.Pixel);
			g.DrawImage(bitRaw, new Rectangle(width1, dy, realWidth2, realHeight1), new Rectangle(rect.Right - realWidth2, rect.Y, realWidth2, realHeight1), GraphicsUnit.Pixel);
			g.DrawImage(bitRaw, new Rectangle(dx, height1, realWidth1, realHeight2), new Rectangle(rect.X, rect.Bottom - realHeight2, realWidth1, realHeight2), GraphicsUnit.Pixel);
			g.DrawImage(bitRaw, new Rectangle(width1, height1, realWidth2, realHeight2), new Rectangle(rect.Right - realWidth2, rect.Bottom - realHeight2, realWidth2, realHeight2), GraphicsUnit.Pixel);

			bitRaw.Dispose();
			imgRaw.Dispose();
			g.Dispose();

			using Bitmap bitMap2 = optimize(bitMap, out var opRect2, true, true);

			importer.spriteBorder = new UnityEngine.Vector4(
				width1 - opRect2.Left,
				height2 - (opHeight - opRect2.Bottom),
				width2 - (opWidth - opRect2.Right),
				height1 - opRect2.Top
			);
			importer.SaveAndReimport();

			var savePath = FileUtil.GetUniqueTempPathInProject();
			bitMap2.Save(savePath, ImageFormat.Png);
			bitMap.Dispose();
			bitMap2.Dispose();

			File.Copy(savePath, pngPath, true);
			File.Delete(savePath);

			AssetDatabase.Refresh();
		}
	}

	public class Region
	{
		public int id;
		public Color Pixel;
		public Rectangle Rect;

		public bool IsClosed = false;

		public int Area => this.Rect.Width * this.Rect.Height;

		public bool IsBeContain(Color pixel, Rectangle rect)
		{
			if (Pixel != pixel)
			{
				return false;
			}

			if (
				(rect.X <= Rect.X && Rect.Right <= rect.Right)
			)
			{
				return true;
			}

			return false;
		}

		public bool IsRelavant(Color pixel, Rectangle rect)
		{
			if (Pixel != pixel)
			{
				return false;
			}

			if (Rect.Bottom != rect.Y)
			{
				return false;
			}

			if (
				(Rect.X <= rect.X && rect.X < Rect.Right)
				|| (Rect.X < rect.Right && rect.Right <= Rect.Right)
				|| (rect.X <= Rect.X && Rect.Right <= rect.Right)
			)
			{
				return true;
			}

			return false;
		}

		public void Limit(Color pixel, Rectangle rect)
		{
			Rect.X = Math.Max(Rect.X, rect.X);
			var Right = Math.Min(Rect.Right, rect.Right);
			Rect.Width = Right - Rect.X;
			var Bottom = Math.Max(Rect.Bottom, rect.Bottom);
			Rect.Height= Bottom - Rect.Y;
			if (Rect.Width == 0)
			{
				IsClosed = true;
			}
		}

	}

	public class RegionManager
	{
		public List<Region> Regions = new List<Region>();
		public List<Region> ClosedRegions = new List<Region>();
		public int regionIdAcc = 0;

		public Region GetOrCreateRegion()
		{
			if (ClosedRegions.Count > 0)
			{
				var region = ClosedRegions[ClosedRegions.Count - 1];
				region.IsClosed = false;
				region.id = 0;
				ClosedRegions.RemoveAt(ClosedRegions.Count - 1);
				return region;
			}
			else
			{
				return new Region();
			}
		}
		public Region GenNewRegion(Color pixel, Rectangle rect)
		{
			var region = GetOrCreateRegion();
			{
				region.id = regionIdAcc++;
				region.Pixel = pixel;
				region.Rect = rect;
			};
			return region;
		}
		public void CloseRegion(Region region)
		{
			region.IsClosed = true;
			ClosedRegions.Add(region);
			Regions.Remove(region);
		}
		public Region ExpandRegion(Region region0)
		{
			var region = GetOrCreateRegion();
			{
				region.id = regionIdAcc++;
				region.Pixel = region0.Pixel;
				region.Rect = region0.Rect;
			};
			return region;
		}
		public void CleanOutDateRegion(int y)
		{
			for (var i = 0; i < Regions.Count; i++)
			{
				var region = Regions[i];
				if (region.IsClosed || region.Rect.Bottom < y)
				{
					CloseRegion(region);
				}
			}
		}
		public void UpdateRegions(Color pixel, Rectangle rect)
		{
			CleanOutDateRegion(rect.Y);

			var relativeRegions = Regions.Where(region => region.IsRelavant(pixel, rect)).ToArray();
			foreach (var region in relativeRegions)
			{
				if (region.IsBeContain(pixel, rect))
				{
					region.Limit(pixel, rect);
				}
				else
				{
					var subRegion = ExpandRegion(region);
					subRegion.Limit(pixel, rect);
					Regions.Add(subRegion);
				}
			}

			// 九宫必须满足从头到尾连续一致
			if (rect.Y == 0)
			{
				var newRegion = this.GenNewRegion(pixel, rect);
				Regions.Add(newRegion);
			}
		}

		public Region FindMaxRegion()
		{
			// 九宫必须满足从头到尾连续一致
			var iter = Enumerable.Empty<Region>().Concat(Regions);//.Concat(ClosedRegions);
			if (iter.Any())
			{
				var maxArea = iter.Max(region => region.Area);
				var maxRegion = iter.FirstOrDefault(region => region.Area == maxArea);
				return maxRegion;
			}
			return null;
		}
	}

	public class BitmapSource
	{
		public Bitmap Bitmap;
		public Rectangle Range;
		public bool IsReverse = false;

		public int Width
		{
			get
			{
				if (IsReverse)
				{
					return Range.Height;
				}
				else
				{
					return Range.Width;
				}
			}
		}
		public int Height
		{
			get
			{
				if (IsReverse)
				{
					return Range.Width;
				}
				else
				{
					return Range.Height;
				}
			}
		}
		public Color GetPixel(int x, int y)
		{
			try
			{
				if (IsReverse)
				{
					return Bitmap.GetPixel(Range.X + y, Range.Y + x);
				}
				else
				{
					return Bitmap.GetPixel(Range.X + x, Range.Y + y);
				}
			}catch(Exception e)
			{
				UnityEngine.Debug.LogException(e);
				throw e;
			}
		}
	}

	public class BitmapSegmentReader
	{
		public BitmapSource Bitmap;

		public int y = 0;
		public int x = 0;

		public bool IsEnd()
		{
			return y >= Bitmap.Height;
		}

		public Region Read()
		{
			var width = Bitmap.Width;
			var height = Bitmap.Height;
			if (y >= height)
			{
				return null;
			}
			var pixel = Bitmap.GetPixel(x, y);
			var w = x + 1;
			for (; w < width; w++)
			{
				var pNext = Bitmap.GetPixel(w, y);
				if (pixel != pNext)
				{
					break;
				}
			}
			var region = new Region()
			{
				Pixel = pixel,
				Rect = new Rectangle(x, y, w - x, 1),
			};
			if (w == width)
			{
				y++;
				x = 0;
			}
			else
			{
				x = w;
			}
			return region;
		}
	}


	/// <summary>
	/// 检测九宫格区域
	/// </summary>
	/// <param name="bitmap"></param>
	public static UnityEngine.Vector4 DetectScale9Rect(Bitmap bitmap, Rectangle? contentRect0)
	{
		// 支持检测9,6,3,2等
		var contentRect = contentRect0 ?? GetValidContentSize(bitmap);

		Region xRegion;
		Region yRegion;
		{
			var xBitmap = new BitmapSource() { Bitmap = bitmap, Range = contentRect, };
			var regionManager = new RegionManager();
			var segmentReader = new BitmapSegmentReader() { Bitmap = xBitmap };
			while (!segmentReader.IsEnd())
			{
				var segRegion = segmentReader.Read();
				regionManager.UpdateRegions(segRegion.Pixel, segRegion.Rect);
			}
			xRegion = regionManager.FindMaxRegion();
		}

		{
			var yBitmap = new BitmapSource() { Bitmap = bitmap, Range = contentRect, IsReverse = true, };
			var regionManager = new RegionManager();
			var segmentReader = new BitmapSegmentReader() { Bitmap = yBitmap };
			while (!segmentReader.IsEnd())
			{
				var segRegion = segmentReader.Read();
				regionManager.UpdateRegions(segRegion.Pixel, segRegion.Rect);
			}
			yRegion = regionManager.FindMaxRegion();
		}

		int BL;
		int BR;
		int BT;
		int BB;

		if (xRegion == null)
		{
			BL = 0;
			BR = 0;
		}
		else
		{
			BL = xRegion.Rect.X;
			BR = bitmap.Width - xRegion.Rect.Right;
		}

		if (yRegion == null)
		{
			BB = 0;
			BT = 0;
		}
		else
		{
			BB = yRegion.Rect.X;
			BT = bitmap.Height - yRegion.Rect.Right;
		}

		var rect9 = new UnityEngine.Vector4(BL, BT, BR, BB);
		return rect9;

	}
}
