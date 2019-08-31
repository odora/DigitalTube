/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2019/8/25
 * Time: 6:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;

namespace DigitalTube
{
	class Program
	{
		private static bool myTubeIsOne(Mat img, int a, int b, int c, int d)
		{
			bool tube_flag = false;
			for (int m = a; m <= b; m++)
			{
				for (int n = c; n <= d; n++)
				{
					if (255 == img.At<byte>(m, n))
					{
						tube_flag = true;
					}
				}
			}
			return tube_flag;
		}


		/**
		 * 穿线法识别单个数字
		 */
		private static char myIdentification(Mat input_img)
		{
			int tube = 0;
			int[,] tube_rect = {
				{ input_img.Rows *0  , input_img.Rows *1/3,input_img.Cols *1/2, input_img.Cols *1/2 },
				{ input_img.Rows *1/3, input_img.Rows *1/3,input_img.Cols *2/3, input_img.Cols -1   },
				{ input_img.Rows *2/3, input_img.Rows *2/3,input_img.Cols *2/3, input_img.Cols -1   },
				{ input_img.Rows *2/3, input_img.Rows -1  ,input_img.Cols *1/2, input_img.Cols *1/2 },
				{ input_img.Rows *2/3, input_img.Rows *2/3,input_img.Cols *0  , input_img.Cols *1/3 },
				{ input_img.Rows *1/3, input_img.Rows *1/3,input_img.Cols *0  , input_img.Cols *1/3 },
				{ input_img.Rows *1/3, input_img.Rows *2/3,input_img.Cols *1/2, input_img.Cols *1/2 }
			};

			// 识别数字1, 为了和8区分。需要比较长宽之比
			if ((0.0 + input_img.Rows) / input_img.Cols >= 3.0)
			{
				tube = 6;
			}
			// 其他场合，进行段位比较
			else
			{
				for (int i = 0; i < 7; i++)
				{
					if (myTubeIsOne(input_img, tube_rect[i, 0], tube_rect[i, 1], tube_rect[i, 2], tube_rect[i, 3]))
					{
						tube = tube + (int) Math.Pow((double) 2, (double) i);
					}
				}
			}
			switch (tube)
			{
				case 63 : return '0';
				case 6  : return '1';
				case 91 : return '2';
				case 79 : return '3';
				case 102: return '4';
				case 109: return '5';
				case 125: return '6';
				case 7  : return '7';
				case 127: return '8';
				case 111: return '9';
			}
			return 'e';
		}

		/**
		 * 扩展图形边界
		 */
		private static Mat CopyBorder(Mat Source)
		{
			// 图形形态学(腐蚀)
			var Eroded = new Mat();
			Mat KernelOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(4, 4));
			Cv2.MorphologyEx(Source, Eroded, MorphTypes.Open, KernelOpen, new Point(-1, -1), 2);

			// 往四周扩展10个像素
			var CopyBordered = new Mat();
			Cv2.CopyMakeBorder(Eroded, CopyBordered, 10, 10, 10, 10, BorderTypes.Constant, new Scalar(0));

			// 图形形态学(膨胀)			
			var Dilated = new Mat();
			Mat KernelClose = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
			Cv2.MorphologyEx(CopyBordered, Dilated, MorphTypes.Close, KernelClose, new Point(-1, -1), 6);

			return Dilated;
		}

		/**
		 * 去掉小数点等噪点
		 */
		private static Mat DropSmallAreaNoise(Mat ImgBinary)
		{
			var Binary2 = new Mat();
			Cv2.BitwiseNot(ImgBinary, Binary2);
			Point[][] Contours = Cv2.FindContoursAsArray(Binary2, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

			// 找出面积最大的区域
			int MaxArea = 0;
			foreach (Point[] Contour in Contours) {
				Rect Region = Cv2.BoundingRect(Contour);
				var Area1 = Region.Width * Region.Height;
				if (Area1 > MaxArea)
				{
					MaxArea = Area1;
				}
			}

			// 构造图像掩码
			Mat MaskMat = Mat.Zeros(Binary2.Rows, Binary2.Cols, MatType.CV_8UC1);
			foreach (Point[] Contour in Contours) {
				Rect Region = Cv2.BoundingRect(Contour);
				var Area1 = Region.Width * Region.Height;
				if (Region.Height > Region.Width && (0.0 + Region.Height) / Region.Width < 3 && Area1 * 4 < MaxArea)
				{
					// 设置感兴趣区域为纯白色(假定白色为背景色)
					MaskMat[Region].SetTo(new Scalar(255));
				}
			}

			var Result = new Mat();
			Cv2.BitwiseOr(ImgBinary, MaskMat, Result);
			return Result;
		}

		/**
		 * 识别的主流程
		 * 返回识别后的数字
		 * needSave是否输出中间图像
		 */
		public static string Process(string Img, bool needSave = false)
		{
			// 图片路径
			// const string Img = @"D:\work\sharp\DigitalTube1\data\1873.jpg";
			string ImgPath = Path.GetDirectoryName(Img);
			string ImgName = Path.GetFileNameWithoutExtension(Img);
			string ImgPref = Path.Combine(ImgPath, ImgName);

			// 显示原始图片
			var OriginImg = Cv2.ImRead(Img);
			// ------------------- debug start
			//Cv2.NamedWindow("OriginImg", WindowMode.Normal);
			//Cv2.ImShow("OriginImg", OriginImg);
			// ------------------- debug end

			// 转化成灰度图
			var Grayscale = Cv2.ImRead(Img, ImreadModes.Grayscale);
			// ------------------- debug start
			//Cv2.NamedWindow("Grayscale", WindowMode.Normal);
			//Cv2.ImShow("Grayscale", Grayscale);
			// ------------------- debug end

			// 往四周扩展10个像素
			var CopyBordered = new Mat();
			Cv2.CopyMakeBorder(Grayscale, CopyBordered, 10, 10, 10, 10, BorderTypes.Constant, new Scalar(255));
			// ------------------- debug start
			//Cv2.NamedWindow("CopyBordered", WindowMode.Normal);
			//Cv2.ImShow("CopyBordered", CopyBordered);
			// ------------------- debug end

			// 进行高斯模糊变换（去噪）
			var Blured = new Mat();
			Cv2.GaussianBlur(CopyBordered, Blured, new Size(15, 15), 0);
			// ------------------- debug start
			//Cv2.NamedWindow("Blured", WindowMode.Normal);
			//Cv2.ImShow("Blured", Blured);
			// ------------------- debug end

			// 转化为二值图片（只有黑白无灰色）
			var Binary = new Mat();
			Cv2.Threshold(Blured, Binary, 128, 255, ThresholdTypes.Binary);
			// ------------------- debug start
			//Cv2.NamedWindow("Binary", WindowMode.Normal);
			//Cv2.ImShow("Binary", Binary);
			// ------------------- debug end

			// 去掉小数点（小块面积去噪）
			var DropNoise = DropSmallAreaNoise(Binary);
			// ------------------- debug start
			//Cv2.NamedWindow("DropNoise", WindowMode.Normal);
			//Cv2.ImShow("DropNoise", DropNoise);
			// ------------------- debug end

			// 图形形态学（腐蚀）
			var Eroded = new Mat();
			Mat KernelOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(4, 4));
			Cv2.MorphologyEx(DropNoise, Eroded, MorphTypes.Open, KernelOpen, new Point(-1, -1), 7);
			// ------------------- debug start
			//Cv2.NamedWindow("Eroded", WindowMode.Normal);
			//Cv2.ImShow("Eroded", Eroded);
			// ------------------- debug end

			// 图形形态学（膨胀）
			var Dilated = new Mat();
			Mat KernelClose = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
			Cv2.MorphologyEx(Eroded, Dilated, MorphTypes.Close, KernelClose, new Point(-1, -1), 5);
			// ------------------- debug start
			//Cv2.NamedWindow("Dilated", WindowMode.Normal);
			//Cv2.ImShow("Dilated", Dilated);
			// ------------------- debug end

			// 图形取反变换（黑白颠倒）
			var Binary2 = new Mat();
			Cv2.BitwiseNot(Dilated, Binary2);
			// ------------------- debug start
			//Cv2.NamedWindow("Binary2", WindowMode.Normal);
			//Cv2.ImShow("Binary2", Binary2);
			// ------------------- debug end

			var Morphologyed = Binary2.Clone();

			// 识别图片轮廓（后续按照轮廓分割）
			var Rects = new List<Rect>();

			// 识别轮廓
			Point[][] Contours = Cv2.FindContoursAsArray(Binary2, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
			Console.WriteLine("Contours Count = " + Contours.Length);
			foreach (Point[] Contour in Contours) {
				Rect Region = Cv2.BoundingRect(Contour);
				Cv2.Rectangle(Morphologyed, Region, new Scalar(193, 0, 0), 4);
				Rects.Add(Region);
			}
			// ------------------- debug start
			//Cv2.NamedWindow("Contours", WindowMode.Normal);
			//Cv2.ImShow("Contours", Morphologyed);
			// ------------------- debug start
			if (needSave)
			{
				Cv2.ImWrite(ImgPref + "__Contours.png", Morphologyed);
			}

			// 对轮廓进行排序（X轴方向，横写文字）
			Rects.Sort((a, b) => (a.X - b.X));

			// 对每个轮廓部分进行处理
			var ImgParts = new Mat[Rects.Count];
			for (var i = 0; i < Rects.Count; i++)
			{
				// 对图形进行边界扩充
				var ImgPart = Binary2[Rects[i]].Clone();
				ImgParts[i] = CopyBorder(ImgPart);
				// ------------------- debug start
				//Cv2.NamedWindow("Number" + (i + 1), WindowMode.Normal);
				//Cv2.ImShow("Number" + (i + 1), ImgParts[i]);
				// ------------------- debug end
				if (needSave)
				{
					Cv2.ImWrite(ImgPref + "__Contours(" + (i + 1) + ").png", ImgParts[i]);
				}
			}

			// 用穿线法将图形识别成数字
			var Numbers = new char[Rects.Count];
			//如果有一个数字识别不出来，那么就t_number[0]标记为'e'
			for (var i = 0; i < Rects.Count; i++)
			{
				Numbers[i] = myIdentification(ImgParts[i]);
				// Console.WriteLine("Number" + (i + 1) + " = " + Numbers[i]);
			}

			// 将画面停留以便观察结果
			//Cv2.WaitKey();

			return new string(Numbers);
		}

		public static void Main(string[] Args)
		{
			if (Args.Length > 0) {
				// 图片文件名
				string Img = Args[0];
				// 是否保存中间结果
				bool needSave = false;
				if (Args.Length > 1 && (Args[1].Equals("1") || Args[1].ToUpper().Equals("TRUE")))
				{
					needSave = true;
				}
				string result = Process(Img, needSave);
				Console.WriteLine(Img + " ==> " + result);
				if (needSave)
				{
					var Writer = new StreamWriter(Img + ".txt", false, Encoding.UTF8);
					Writer.WriteLine(result);
				}

			} else {
				Console.WriteLine("Error : At Least One Parameter");
			}
		}
	}
}
