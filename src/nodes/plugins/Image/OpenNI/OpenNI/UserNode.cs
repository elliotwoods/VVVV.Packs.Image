#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using OpenNI;
using System.Runtime.InteropServices;

using VVVV.CV.Core;

#endregion usings

namespace VVVV.Nodes.OpenCV.OpenNI
{
	#region PluginInfo
	[PluginInfo(Name = "Users", Category = "CV.Image", Version = "OpenNI", Help = "OpenNI User Generator", Tags = "")]
	#endregion PluginInfo
	public class UserNode : IPluginEvaluate, IDisposable
	{
		[DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		#region fields & pins
		[Input("Context")]
		ISpread<Device> FPinInContext;

		[Output("ID")]
		ISpread<int> FPinOutID;

		[Output("Present")]
		ISpread<bool> FPinOutPresent;

		[Output("Position")]
		ISpread<Vector3D> FPinOutPosition;

		[Output("Mask")]
		ISpread<CVImageLink> FPinOutMask;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		CVImageOutput FImageMask = new CVImageOutput();

		#endregion fields & pins

		#region OpenNI
		
		Device FState;
		UserGenerator FUserGenerator;
		bool FStarted = false;
		#endregion

		#region Data
		private class UserData
		{
			public UserData()
			{
				this.Present = true;
				this.Position = new Vector3D();
			}

			public UserData(bool present, Vector3D position)
			{
				this.Present = present;
				this.Position = position;
			}
			public bool Present;
			public Vector3D Position;
		}

		Dictionary<int, UserData> FUserData = new Dictionary<int, UserData>();
		Object FLockUserData = new Object();
		Spread<Vector3D> FUserPositions = new Spread<Vector3D>(0);
		
		#endregion

		[ImportingConstructor]
		public UserNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
			Close();
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInContext[0] != FState)
			{
				FState = FPinInContext[0];
				FState.Initialised += new EventHandler(FState_Initialised);
			}

			if (FState == null)
			{
				Close();
				return;
			}

			if (FStarted)
				GiveOutputs();
			else
			{
				FPinOutID.SliceCount = 0;
				FPinOutPresent.SliceCount = 0;
			}
		}

		void FState_Initialised(object sender, EventArgs e)
		{
			try
			{
				//initialise
				FUserGenerator = new UserGenerator(FState.Context);
				FUserGenerator.NewUser += new EventHandler<NewUserEventArgs>(FUserGenerator_NewUser);
				FUserGenerator.LostUser += new EventHandler<UserLostEventArgs>(FUserGenerator_LostUser);
				FUserGenerator.UserReEnter += new EventHandler<UserReEnterEventArgs>(FUserGenerator_UserReEnter);
				FUserGenerator.UserExit += new EventHandler<UserExitEventArgs>(FUserGenerator_UserExit);
				FUserGenerator.StartGenerating();

				FPinOutMask[0] = FImageMask.Link;
				FImageMask.Image.Initialise(new Size(640, 480), TColorFormat.L16);

				FState.Update += new EventHandler(FState_Update);

				FStarted = true;
				FPinOutStatus[0] = "OK";
			}
			catch (StatusException err)
			{
				Close();
				FPinOutStatus[0] = err.Message;
			}
		}

		void FState_Update(object sender, EventArgs e)
		{
			Update();
		}

		void Update()
		{
			if (FStarted)
			{
				IntPtr userPixels = FUserGenerator.GetUserPixels(0).LabelMapPtr;
				FImageMask.Image.SetPixels(userPixels);
				FImageMask.Send();

				lock (FLockUserData)
				{
					FUserPositions.SliceCount = FUserData.Count;
					foreach (var u in FUserData)
					{
						Point3D p = FUserGenerator.GetCoM(u.Key);
						FUserData[u.Key].Position = new Vector3D(p.X / 1000.0d, p.Y / 1000.0d, p.Z / 1000.0d);
					}
				}
			}
		}

		void GiveOutputs()
		{
			int userCount = FUserData.Count;

			lock (FLockUserData)
			{
				FPinOutID.SliceCount = userCount;
				FPinOutPresent.SliceCount = userCount;
				FPinOutPosition.SliceCount = userCount;

				int slice=0;
				foreach (int id in FUserData.Keys)
				{
					FPinOutID[slice] = id;
					FPinOutPresent[slice] = FUserData[id].Present;
					FPinOutPosition[slice] = FUserData[id].Position;
					slice++;
				}
			}
		}

		void Close()
		{
			if (FStarted)
			{
				if (FUserGenerator != null)
					FUserGenerator.Dispose();
				FUserGenerator = null;
				FStarted = false;
			}
		}

		void FUserGenerator_NewUser(object sender, NewUserEventArgs e)
		{
			lock (FLockUserData)
				FUserData.Add(e.ID, new UserData());
		}

		void FUserGenerator_LostUser(object sender, UserLostEventArgs e)
		{
			lock (FLockUserData)
				if (FUserData.ContainsKey(e.ID))
					FUserData.Remove(e.ID);
		}

		void FUserGenerator_UserExit(object sender, UserExitEventArgs e)
		{
			FUserData[e.ID].Present = false;
		}

		void FUserGenerator_UserReEnter(object sender, UserReEnterEventArgs e)
		{
			FUserData[e.ID].Present = true;
		}

	}
}
