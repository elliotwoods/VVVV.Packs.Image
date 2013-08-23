using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
	class ModeRegister
	{
		public class ModeIndex
		{
			public ModeIndex(string Index)
			{
				this.Index = Index;
			}

			public string Index;
		}

		HashSet<_BMDPixelFormat> SupportedFormats = new HashSet<_BMDPixelFormat>();

		public class Mode
		{
			public IDeckLinkDisplayMode DisplayModeHandle;
			public _BMDPixelFormat PixelFormat;
			public _BMDVideoOutputFlags Flags;
			public int Width;
			public int Height;
			public double FrameRate;
			public int PixelGroupSizeOfSet { get; private set; }
			public int PixelGroupSizeInMemory { get; private set; }
			public int CompressedWidth
			{
				get
				{
					return Width * this.PixelGroupSizeInMemory / this.PixelGroupSizeOfSet / 4;
				}
			}

			public void CalcPixelBoundaries()
			{
				switch (this.PixelFormat)
				{
					case _BMDPixelFormat.bmdFormat8BitYUV:
						PixelGroupSizeOfSet = 2;
						PixelGroupSizeInMemory = 4;
						break;

					case _BMDPixelFormat.bmdFormat10BitYUV:
						PixelGroupSizeOfSet = 6;
						PixelGroupSizeInMemory = 16;
						break;

					case _BMDPixelFormat.bmdFormat8BitBGRA:
					case _BMDPixelFormat.bmdFormat8BitRGBA:
					case _BMDPixelFormat.bmdFormat8BitARGB:
						PixelGroupSizeOfSet = 1;
						PixelGroupSizeInMemory = 4;
						break;

					case _BMDPixelFormat.bmdFormat10BitRGB:
						PixelGroupSizeOfSet = 1;
						PixelGroupSizeInMemory = 4;
						break;

					case _BMDPixelFormat.bmdFormat12BitYUV:
						PixelGroupSizeOfSet = 4;
						PixelGroupSizeInMemory = 6;
						throw (new Exception("12bit modes are undocumented"));
						break;
				}
			}
		}

		public static ModeRegister Singleton = new ModeRegister();

		Dictionary<string, Mode> FModes = new Dictionary<string, Mode>();
		public Dictionary<string, Mode> Modes
		{
			get
			{
				return this.FModes;
			}
		}

		public ModeRegister()
		{
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat8BitYUV);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat10BitYUV);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat8BitBGRA);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat8BitRGBA);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat8BitARGB);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat10BitRGB);
			SupportedFormats.Add(_BMDPixelFormat.bmdFormat12BitYUV);
		}

		public void Refresh(_BMDVideoOutputFlags flags)
		{
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				foreach (var mode in FModes.Values)
					Marshal.ReleaseComObject(mode.DisplayModeHandle);
				FModes.Clear();

				DeviceRegister.Singleton.Refresh();
				foreach (var device in DeviceRegister.Singleton.Devices)
				{
					var output = (IDeckLinkOutput)device.DeviceHandle;
					if (output == null)
						throw (new Exception("Device has no outputs"));

					IDeckLinkDisplayModeIterator modeIterator;
					output.GetDisplayModeIterator(out modeIterator);

					while (true)
					{
						IDeckLinkDisplayMode mode;

						modeIterator.Next(out mode);
						if (mode == null)
							break;

						foreach (_BMDPixelFormat pixelFormat in Enum.GetValues(typeof(_BMDPixelFormat)))
						{
							if (true || SupportedFormats.Contains(pixelFormat))
							{
								try
								{
									_BMDDisplayModeSupport support;
									IDeckLinkDisplayMode displayMode;
									output.DoesSupportVideoMode(mode.GetDisplayMode(), pixelFormat, flags, out support, out displayMode);

									string modeString;
									mode.GetName(out modeString);
									int stripLength = "bmdFormat".Length;
									string pixelFormatString = pixelFormat.ToString();
									pixelFormatString = pixelFormatString.Substring(stripLength, pixelFormatString.Length - stripLength);

									modeString += " [" + pixelFormatString + "]";

									long duration, timescale;
									mode.GetFrameRate(out duration, out timescale);

									Mode inserter = new Mode()
									{
										DisplayModeHandle = mode,
										Flags = flags,
										PixelFormat = pixelFormat,
										Width = mode.GetWidth(),
										Height = mode.GetHeight(),
										FrameRate = (double)timescale / (double)duration
									};
									inserter.CalcPixelBoundaries();

									if (support == _BMDDisplayModeSupport.bmdDisplayModeSupported)
									{
										FModes.Add(modeString, inserter);
									}
									else if (support == _BMDDisplayModeSupport.bmdDisplayModeSupportedWithConversion)
									{
										modeString += " converted";
										FModes.Add(modeString, inserter);
									}
								}
								catch
								{
								}
							}
						}
					}
				}
			});
		}

		public string[] EnumStrings
		{
			get
			{
				return FModes.Keys.ToArray();
			}
		}
	}
}
