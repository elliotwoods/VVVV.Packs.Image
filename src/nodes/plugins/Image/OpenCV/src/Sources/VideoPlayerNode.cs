#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

using System.Threading;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV
{
	class PlayVideoInstance
	{
		public string Filename = "";

		double FCaptureFPS;
		public double Position;
		public double Length;
		int FCapturePeriod;
		bool HasCapture;

		/// <summary>
		/// Thread
		/// </summary>
		Thread CaptureThread;
		Object CaptureThreadLock = new Object();
		bool CaptureThreadRun = false;

		public ImageRGB Image = new ImageRGB();
		Capture _Capture;

		private bool _Changed = false;
		private bool _Play = false;
		private bool _Loop = false;
		private string _Status = "";

		public void Initialise(string filename)
		{
			Close();
			try
			{
				_Capture = new Capture(filename); //create a video player
				FCaptureFPS = _Capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS);
				Length = _Capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT) / FCaptureFPS;
				FCapturePeriod = (int)(1000.0d / FCaptureFPS);
			}
			catch
			{
				_Status = "Player open failed";
				return;
			}

			_Status = "Player open success";
			HasCapture = true;

			Image.FrameAttributesChanged = true;

			Filename = filename;
			CaptureThreadRun = true;
			CaptureThread = new Thread(fnCapture);
			CaptureThread.Start();
		}

		public void Close()
		{
			if (HasCapture)
			{
				CaptureThreadRun = false;
				CaptureThread.Join(100);
				_Status = "Capture thread closed";

				_Capture.Dispose();
				HasCapture = false;
			}
		}

		public bool Play
		{
			get
			{
				return _Play;
			}
			set
			{
				lock (CaptureThreadLock)
				{
					_Play = value;
				}
			}
		}

		public bool Loop
		{
			get
			{
				return _Loop;
			}
			set
			{
				lock (CaptureThreadLock)
				{
					_Loop = value;
				}
			}
		}

		public bool IsRunning
		{
			get
			{
				return CaptureThreadRun;
			}
		}

		public bool Changed
		{
			get
			{
				if (_Changed)
				{
					_Changed = false;
					return true;
				} else
					return false;
			}
		}

		public string Status
		{
			get
			{
				return _Status;
			}
		}

		private void fnCapture()
		{
			while (CaptureThreadRun)
			{
				lock (CaptureThreadLock)
				{
					if ((Image.Img == null && !_Play) || _Play)
					{
						lock (Image.Lock)
							Image.Img = _Capture.QueryFrame();
						_Changed = true;
					}

					if (_Loop)
						if (_Capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES) + 1 == _Capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT))
							_Capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES, 0.0d);

					Position = _Capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_MSEC) / 1000.0d;
				}
				Thread.Sleep(FCapturePeriod);
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoPlayer",
				Category = "OpenCV",
				Version = "",
				Help = "Plays AVI files into IPLImage, using libavcodec(?)",
				Tags = "")]
	#endregion PluginInfo
	public class PlayVideoNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins

		[Input("Filename", StringType = StringType.Filename)]
		IDiffSpread<string> FPinInFilename;

		[Input("Play")]
		IDiffSpread<bool> FPinInPlay;

		[Input("Loop")]
		IDiffSpread<bool> FPinInLoop;

		[Output("Image")]
		ISpread<ImageRGB> FPinOutImage;

		[Output("Position")]
		ISpread<double> FPinOutPosition;

		[Output("Length")]
		ISpread<double> FPinOutLength;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		IPluginHost FHost;

		Dictionary<int, PlayVideoInstance> FCaptures= new Dictionary<int, PlayVideoInstance>();

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public PlayVideoNode(IPluginHost host)
		{
			FHost = host;
		}

		public void Dispose()
		{
			foreach (KeyValuePair<int, PlayVideoInstance> player in FCaptures)
				player.Value.Close();

			GC.SuppressFinalize(this);
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (SpreadMax == 0)
			{
				FCaptures.Clear();
				ResizeOutput(0);
				return;
			}

			if (FPinInFilename.IsChanged)
			{
				if (FCaptures.Count != SpreadMax)
					ResizeOutput(SpreadMax);

				for (int i = 0; i < SpreadMax; i++)
				{
					if (!FCaptures.ContainsKey(i))
					{
						FCaptures.Add(i, new PlayVideoInstance());
					}
					if (FCaptures[i].Filename != FPinInFilename[i])
					{
						FCaptures[i].Initialise(FPinInFilename[i]);
					}
				}

				if (FCaptures.Count > SpreadMax)
				{
					for (int i = SpreadMax; i < FCaptures.Count; i++)
					{
						FCaptures.Remove(i);
					}
				}

				TakeInputs(SpreadMax);
			}

			if (FPinInPlay.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
				{
					FCaptures[i].Play = FPinInPlay[i];
				}
			}

			if (FPinInLoop.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
				{
					FCaptures[i].Loop = FPinInLoop[i];
				}
			}

			GiveOutputs();
		}

		void TakeInputs(int count)
		{
			for (int i = 0; i < count; i++)
			{
				FCaptures[i].Play = FPinInPlay[i];
				FCaptures[i].Loop = FPinInLoop[i];
			}
		}

		void GiveOutputs()
		{
			foreach (KeyValuePair<int, PlayVideoInstance> player in FCaptures)
			{
				if (player.Value.IsRunning)
				{
					FPinOutImage[player.Key] = player.Value.Image;
					FPinOutPosition[player.Key] = player.Value.Position;
					FPinOutLength[player.Key] = player.Value.Length;
				}
				FPinOutStatus[player.Key] = player.Value.Status;
			}
		}

		void ResizeOutput(int count)
		{
			FPinOutStatus.SliceCount = count;
			FPinOutImage.SliceCount = count;
			FPinOutLength.SliceCount = count;
			FPinOutPosition.SliceCount = count;

			for (int i = 0; i < count; i++)
			{
				if (FPinOutImage[i] == null)
				{
					FPinOutImage[i] = new ImageRGB();
				}
			}
		}
	
	}
}
