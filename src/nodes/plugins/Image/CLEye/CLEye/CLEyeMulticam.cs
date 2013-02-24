//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// This file is part of CL-EyeMulticam SDK
//
// WPF C# CLEyeMulticamWPFTest Sample Application
//
// It allows the use of multiple CL-Eye cameras in your own applications
//
// For updates and file downloads go to: http://codelaboratories.com
//
// Copyright 2008-2010 (c) Code Laboratories, Inc. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace CLEyeMulticam
{
	#region [ Camera Parameters ]
	// camera color mode
	public enum CLEyeCameraColorMode
	{
		CLEYE_MONO_PROCESSED,
		CLEYE_COLOR_PROCESSED,
		CLEYE_MONO_RAW,
		CLEYE_COLOR_RAW,
		CLEYE_BAYER_RAW
	};

	// camera resolution
	public enum CLEyeCameraResolution
	{
		CLEYE_QVGA,
		CLEYE_VGA
	};
	// camera parameters
	public enum CLEyeCameraParameter
	{
		// camera sensor parameters
		CLEYE_AUTO_GAIN,			// [false, true]
		CLEYE_GAIN,					// [0, 79]
		CLEYE_AUTO_EXPOSURE,		// [false, true]
		CLEYE_EXPOSURE,				// [0, 511]
		CLEYE_AUTO_WHITEBALANCE,	// [false, true]
		CLEYE_WHITEBALANCE_RED,		// [0, 255]
		CLEYE_WHITEBALANCE_GREEN,	// [0, 255]
		CLEYE_WHITEBALANCE_BLUE,	// [0, 255]
		// camera linear transform parameters
		CLEYE_HFLIP,				// [false, true]
		CLEYE_VFLIP,				// [false, true]
		CLEYE_HKEYSTONE,			// [-500, 500]
		CLEYE_VKEYSTONE,			// [-500, 500]
		CLEYE_XOFFSET,				// [-500, 500]
		CLEYE_YOFFSET,				// [-500, 500]
		CLEYE_ROTATION,				// [-500, 500]
		CLEYE_ZOOM,					// [-500, 500]
		// camera non-linear transform parameters
		CLEYE_LENSCORRECTION1,		// [-500, 500]
		CLEYE_LENSCORRECTION2,		// [-500, 500]
		CLEYE_LENSCORRECTION3,		// [-500, 500]
		CLEYE_LENSBRIGHTNESS		// [-500, 500]
	};
	#endregion

	public class CLEyeCameraDevice : IDisposable
	{
		#region [ CLEyeMulticam Imports ]
		[DllImport("CLEyeMulticam.dll")]
		public static extern int CLEyeGetCameraCount();
		[DllImport("CLEyeMulticam.dll")]
		public static extern Guid CLEyeGetCameraUUID(int camId);
		[DllImport("CLEyeMulticam.dll")]
		public static extern IntPtr CLEyeCreateCamera(Guid camUUID, CLEyeCameraColorMode mode, CLEyeCameraResolution res, float frameRate);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeDestroyCamera(IntPtr camera);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeCameraStart(IntPtr camera);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeCameraStop(IntPtr camera);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeCameraLED(IntPtr camera, bool on);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeSetCameraParameter(IntPtr camera, CLEyeCameraParameter param, int value);
		[DllImport("CLEyeMulticam.dll")]
		public static extern int CLEyeGetCameraParameter(IntPtr camera, CLEyeCameraParameter param);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeCameraGetFrameDimensions(IntPtr camera, ref int width, ref int height);
		[DllImport("CLEyeMulticam.dll")]
		public static extern bool CLEyeCameraGetFrame(IntPtr camera, IntPtr pData, int waitTimeout);

		#region [ Private ]
		private IntPtr _camera = IntPtr.Zero;
		#endregion

		#region [ Properties ]
		public float Framerate { get; set; }
		public CLEyeCameraColorMode ColorMode { get; set; }
		public CLEyeCameraResolution Resolution { get; set; }

		public void SetParameter(CLEyeCameraParameter parameter, int value)
		{
			if (_camera == null) return;
			CLEyeSetCameraParameter(_camera, parameter, value);
		}

		public bool AutoGain
		{
			get
			{
				if (_camera == null) return false;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_GAIN) != 0;
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_GAIN, value ? 1 : 0);
			}
		}
		public int Gain
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_GAIN);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_GAIN, value);
			}
		}
		public bool AutoExposure
		{
			get
			{
				if (_camera == null) return false;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE) != 0;
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE, value ? 1 : 0);
			}
		}
		public int Exposure
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_EXPOSURE);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_EXPOSURE, value);
			}
		}
		public bool AutoWhiteBalance
		{
			get
			{
				if (_camera == null) return true;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE) != 0;
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE, value ? 1 : 0);
			}
		}
		public int WhiteBalanceRed
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED, value);
			}
		}
		public int WhiteBalanceGreen
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN, value);
			}
		}
		public int WhiteBalanceBlue
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE, value);
			}
		}
		public bool HorizontalFlip
		{
			get
			{
				if (_camera == null) return false;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HFLIP) != 0;
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HFLIP, value ? 1 : 0);
			}
		}
		public bool VerticalFlip
		{
			get
			{
				if (_camera == null) return false;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VFLIP) != 0;
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VFLIP, value ? 1 : 0);
			}
		}
		public int HorizontalKeystone
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HKEYSTONE);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HKEYSTONE, value);
			}
		}
		public int VerticalKeystone
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VKEYSTONE);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VKEYSTONE, value);
			}
		}
		public int XOffset
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_XOFFSET);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_XOFFSET, value);
			}
		}
		public int YOffset
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_YOFFSET);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_YOFFSET, value);
			}
		}
		public int Rotation
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ROTATION);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ROTATION, value);
			}
		}
		public int Zoom
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ZOOM);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ZOOM, value);
			}
		}
		public int LensCorrection1
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION1);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION1, value);
			}
		}
		public int LensCorrection2
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION2);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION2, value);
			}
		}
		public int LensCorrection3
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION3);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION3, value);
			}
		}
		public int LensBrightness
		{
			get
			{
				if (_camera == null) return 0;
				return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS);
			}
			set
			{
				if (_camera == null) return;
				CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS, value);
			}
		}
		#endregion

		#region [ Static ]
		public static int CameraCount { get { return CLEyeGetCameraCount(); } }
		public static Guid CameraUUID(int idx) { return CLEyeGetCameraUUID(idx); }
		#endregion

		#region [ Methods ]
		public CLEyeCameraDevice(CLEyeCameraResolution resolution,
			CLEyeCameraColorMode colorMode, int fps)
		{
			// set default values
			Framerate = fps;
			ColorMode = colorMode;
			Resolution = resolution;
		}
		public CLEyeCameraDevice() :
			this(CLEyeMulticam.CLEyeCameraResolution.CLEYE_VGA, CLEyeMulticam.CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED, 30)
		{

		}

		~CLEyeCameraDevice()
		{
			// Finalizer calls Dispose
			Dispose();
		}

		public void Dispose()
		{
			Stop();
			GC.SuppressFinalize(this);
		}

		public bool Start(Guid cameraGuid)
		{
			int w = 0, h = 0;
			_camera = CLEyeCreateCamera(cameraGuid, ColorMode, Resolution, Framerate);

			if (_camera == IntPtr.Zero)
				return false;

			CLEyeCameraGetFrameDimensions(_camera, ref w, ref h);
			if (ColorMode == CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED || ColorMode == CLEyeCameraColorMode.CLEYE_COLOR_RAW)
			{
				uint imageSize = (uint)w * (uint)h * 4;
			}
			else
			{
				uint imageSize = (uint)w * (uint)h;
			}
			CLEyeCameraStart(_camera);
			return true;
		}

		public void Stop()
		{
			if (_camera != IntPtr.Zero)
			{
				CLEyeCameraStop(_camera);
				CLEyeDestroyCamera(_camera);
				_camera = IntPtr.Zero;
			}
		}

		public bool getPixels(IntPtr pixels, int timeout)
		{
			if (_camera != IntPtr.Zero)
				return CLEyeCameraGetFrame(_camera, pixels, timeout);
			return false;
		}

		public void setLED(bool isOn)
		{
			CLEyeCameraLED(_camera, isOn);
		}
		#endregion
	}
}
		#endregion