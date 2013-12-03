using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes 
{
	public class WriterInstance : IDestinationInstance
	{
		bool FSave = false;
		public void Save()
		{
			FSave = true;
		}

		private string FFilename = "save.png";
		public string Filename
		{
			get
			{
				lock (FFilename)
				{
					return FFilename;
				}
			}
			set
			{
				lock (FFilename)
				{
					FFilename = value;
				}
			}
		}

		public bool Success = false;

		public override void Allocate()
		{

		}

		public override void Process()
		{
			if (FSave)
			{
				FSave = false;

				try
				{
					FInput.LockForReading();
					FInput.Image.SaveFile(FFilename);
					FInput.ReleaseForReading();
					this.Status = "OK";
					this.Success = true;
				}
				catch (Exception e)
				{
					this.Status = e.Message;
				}
			}
		}
	}


	#region PluginInfo
	[PluginInfo(Name = "Writer", Category = "CV.Image", Help = "Save a still image file", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class WriterImageNode : IDestinationNode<WriterInstance>
	{
		[Input("Filename", DefaultString="save.png", StringType=StringType.Filename)]
		IDiffSpread<string> FFilename;

		[Input("Do Save", IsBang=true)]
		ISpread<bool> FDo;

		[Output("Status")]
		ISpread<string> FStatus;

		[Output("Success")]
		ISpread<bool> FSuccess;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			FStatus.SliceCount = InstanceCount;
			FSuccess.SliceCount = InstanceCount;

			if (FFilename.IsChanged)
			for (int i = 0; i < InstanceCount; i++)
			{
				FProcessor[i].Filename = FFilename[i];
			}

			for (int i = 0; i < InstanceCount; i++)
			{
				if (FDo[i])
					FProcessor[i].Save();
			}

			for (int i = 0; i < InstanceCount; i++)
			{
				FSuccess[i] = FProcessor[i].Success;
				FStatus[i] = FProcessor[i].Status;
			}
		}
	}
}
