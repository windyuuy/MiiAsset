using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Kaitai;

namespace Assets.Framework.AssetsManager.Editor
{
	public class TextureEditorUtils
	{
		//public static float GetChannel(long color,int channelMask)
		//{
		//	if(channelMask == 24)
		//	{
		//		return color.A / 255.0f;
		//	}
		//	else if (channelMask == 16)
		//	{
		//		return color.R / 255.0f;
		//	}
		//	else if (channelMask == 8)
		//	{
		//		return color.G / 255.0f;
		//	}
		//	else if (channelMask == 0)
		//	{
		//		return color.B / 255.0f;
		//	}
		//	throw new NotImplementedException();
		//}

		public static double CalcTextureDetailLevelFromFile(string path)
		{
			using var imgRaw = Image.FromFile(path);
			using MemoryStream bitStream = new MemoryStream();
			imgRaw.Save(bitStream, ImageFormat.Bmp);
			bitStream.Seek(0, SeekOrigin.Begin);
			var bitRaw = new Bmp(new KaitaiStream(bitStream));
			var blt = TextureEditorUtils.CalcTextureDetailLevel(bitRaw);
			return blt;
		}

		public static double CalcTextureDetailLevel(Image imgRaw)
		{
			using MemoryStream bitStream = new MemoryStream();
			imgRaw.Save(bitStream, ImageFormat.Bmp);
			bitStream.Seek(0, SeekOrigin.Begin);
			var bitRaw = new Bmp(new KaitaiStream(bitStream));
			var blt = TextureEditorUtils.CalcTextureDetailLevel(bitRaw);
			return blt;
		}

		public static double CalcTextureDetailLevel(Bmp bitmap)
		{
			var aD = CalcTextureDetailLevel2(bitmap, (uint)bitmap.DibInfo.ColorMaskAlpha);
			var rD = CalcTextureDetailLevel2(bitmap, (uint)bitmap.DibInfo.ColorMaskRed);
			var gD = CalcTextureDetailLevel2(bitmap, (uint)bitmap.DibInfo.ColorMaskGreen);
			var bD = CalcTextureDetailLevel2(bitmap, (uint)bitmap.DibInfo.ColorMaskBlue);

			var cD= (aD + rD + gD + bD) / 4;
			return cD;
		}

		public static byte GetPixelChannel(Bmp bitmap, ushort bitsPerPixel, uint width,int h,int w, uint channelMask)
		{
			long k = (width * h + w)*(bitsPerPixel/8);
			byte p;
			if (channelMask == 0 && bitsPerPixel==32)
			{
				p = bitmap.M_RawBitmap[k + 3];
			}
			else if (channelMask == 0x00FF)
			{
				p = bitmap.M_RawBitmap[k  + 0];
			}
			else if (channelMask == 0x0FF00)
			{
				p = bitmap.M_RawBitmap[k + 1];
			}
			else if (channelMask == 0x0FF0000)
			{
				p = bitmap.M_RawBitmap[k + 2];
			}
			else if (channelMask == 0)
			{
				return 0;
			}
			else
			{
				throw new NotImplementedException();
			}
			return p;
		}

		public static double ptR2(double p0,double p1)
		{
			if (p0 == p1)
			{
				return 0;
			}
			var l= Math.Sqrt(
				Math.Log(Math.Abs(p1 - p0))/Math.Log(2)
			);
			return l;
		}

		public static double CalcTextureDetailLevel2(Bmp bitmap,uint channelMask)
		{
			double total = 0;

			var width = bitmap.DibInfo.Header.ImageWidth;
			var height = bitmap.DibInfo.Header.ImageHeight;
			var bitsPerPixel = bitmap.DibInfo.Header.BitsPerPixel;
			int width1 = (int)(width - 1);
			int height1= height - 1;
			Parallel.For(0, height1, h =>
			{
				double total0 = 0;
				for (var w = 0; w < width - 1; w++)
				{
					var p0 = GetPixelChannel(bitmap, bitsPerPixel, width, h, w, channelMask);
					var p1 = GetPixelChannel(bitmap, bitsPerPixel, width, h + 1, w, channelMask);
					var p2 = GetPixelChannel(bitmap, bitsPerPixel, width, h, w + 1, channelMask);
					var p3 = GetPixelChannel(bitmap, bitsPerPixel, width, h + 1, w + 1, channelMask);

					var pA = ptR2(p1, p0) + ptR2(p2, p0) + ptR2(p3, p0)*0.707;

					total0 += pA;
				}

				while (true)
				{
					var localTotal = total;
					if (localTotal == Interlocked.CompareExchange(ref total, localTotal + total0, localTotal))
					{
						break;
					}
				}
			});

			//double total2 = 0;
			//for (var h = 0; h < height - 1; h++)
			//{
			//	for (var w = 0; w < width - 1; w++)
			//	{
			//		var p0 = GetPixelChannel(bitmap, bitsPerPixel, width, h, w, channelMask);
			//		var p1 = GetPixelChannel(bitmap, bitsPerPixel, width, h + 1, w, channelMask);
			//		var p2 = GetPixelChannel(bitmap, bitsPerPixel, width, h, w + 1, channelMask);
			//		var p3 = GetPixelChannel(bitmap, bitsPerPixel, width, h + 1, w + 1, channelMask);

			//		var pA = ptR2(p1, p0) + ptR2(p2, p0) + ptR2(p3, p0);

			//		total += pA;
			//	}
			//}
			//if ((int)total != (int)total2)
			//{
			//	throw new Exception("unmatched result");
			//}

			if (width == 0 || height == 0)
			{
				return 0;
			}

			total /= (2.707 * 8 * width * height);
			total *= 100;
			return total;
		}
	}
}
