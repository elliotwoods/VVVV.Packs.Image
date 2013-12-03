using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;

namespace VVVV.CV.Nodes.StructuredLight
{
	public class ScanSet
	{
		/// <summary>
		/// Encoded data that has been scanned.
		/// EncodedData[iCameraPixel]
		/// </summary>
		public ulong[] EncodedData;

		/// <summary>
		/// Raw data result of scan
		/// ProjectorInCamera[iCameraPixel] = iProjectorPixel
		/// </summary>
		public ulong[] ProjectorInCamera;

		/// <summary>
		/// Inverted Raw data result of scan
		/// RawData[iProjectorPixel] = iCameraPixel
		/// </summary>
		public ulong[] CameraInProjector;

		/// <summary>
		/// How far on average the pixel value stepped
		/// </summary>
		public float[] Distance;

		/// <summary>
		/// The lumiance of the camera pixel averaged across the scan
		/// </summary>
		public byte[] Luminance;

		public IPayload Payload;
		public Size CameraSize;
		public int CameraPixelCount
		{
			get
			{
				lock (this)
					return CameraSize.Width * CameraSize.Height;
			}
		}
		public int ProjectorPixelCount
		{
			get
			{
				lock (this)
					return Payload.PixelCount;
			}
		}
		public Size ProjectorSize
		{
			get
			{
				lock (this)
					return Payload.Size;
			}
		}

		public event EventHandler UpdateAttributes;
		public void OnUpdateAttributes()
		{
			FInitialised = true;
			if (UpdateAttributes != null)
				UpdateAttributes(this, EventArgs.Empty);
		}

		public event EventHandler UpdateData;
		public void OnUpdateData()
		{
			FDataAvailable = true;
			if (UpdateData != null)
				UpdateData(this, EventArgs.Empty);
		}

		bool FDataAvailable = false;
		public bool DataAvailable
		{
			get
			{
				return FDataAvailable;
			}
		}

		bool FInitialised = false;
		public bool Allocated
		{
			get
			{
				return FInitialised && Payload != null;
			}
		}

		public void Allocate(Size CameraSize)
		{
			lock (this)
			{
				if (this.Payload != null)
				{
					this.CameraSize = CameraSize;
					this.EncodedData = new ulong[CameraPixelCount];
					this.ProjectorInCamera = new ulong[CameraPixelCount];
					this.CameraInProjector = new ulong[ProjectorPixelCount];
					this.Distance = new float[CameraPixelCount];
					this.Luminance = new byte[CameraPixelCount];
					this.OnUpdateAttributes();
					FInitialised = true;
				}
			}
		}

		[DllImport("msvcrt.dll")]
		private static unsafe extern void memset(void* dest, int c, int count);

		public unsafe void Clear()
		{
			lock (this)
			{
				if (!this.Allocated)
					return;

				fixed (ulong* dataFixed = &EncodedData[0])
					memset((void*)dataFixed, 0, sizeof(ulong) * CameraPixelCount);
			
				fixed (ulong* projInCameraFixed = &ProjectorInCamera[0])
					memset((void*)projInCameraFixed, 0, sizeof(ulong) * CameraPixelCount);
				fixed (ulong* camInProjectorFixed = &CameraInProjector[0])
					memset((void*)camInProjectorFixed, 0, sizeof(ulong) * ProjectorPixelCount);

				fixed (float* distanceFixed = &Distance[0])
					memset((void*)distanceFixed, 0, sizeof(float) * CameraPixelCount);
				fixed (byte* luminanceFixed = &Luminance[0])
					memset((void*)luminanceFixed , 0, sizeof(byte) * CameraPixelCount);

				FDataAvailable = false;
			}
		}

		public bool GetValue(ulong index, ref ulong output)
		{
			lock (this)
			{
				if (index > Payload.MaxIndexCached)
					return false;

				output = Payload.DataInverse[index];

				return true;
			}
		}

		/// <summary>
		/// Calculates the maps
		/// </summary>
		public unsafe void Evaluate()
		{
			fixed (ulong* encodedFixed = &EncodedData[0])
			{
				fixed (ulong* rawFixed = &ProjectorInCamera[0])
				{
					ulong* encoded = encodedFixed;
					ulong* raw = rawFixed;
					ulong projCount = (ulong) ProjectorPixelCount;
					ulong cameraCount = (ulong)CameraPixelCount;

					for (ulong i = 0; i < cameraCount; i++)
					{
						*raw = Payload.DataInverse[*encoded++];

						if (*raw < projCount)
							CameraInProjector[*raw] = i;

						raw++;
					}
				}
			}

			OnUpdateData();
		}
	}
}
