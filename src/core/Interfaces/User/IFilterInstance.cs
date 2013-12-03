using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IFilterInstance : IInstance, IInstanceInput, IInstanceOutput
	{
		protected CVImageInput FInput;
		protected CVImageOutput FOutput;

		public void SetInput(CVImageInput input)
		{
			FInput = input;
			ReAllocate();
		}

		public bool HasInput(CVImageInput input)
		{
			return FInput == input;
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		/// <summary>
		/// You should call this inside your filter if your filter takes some time (> 1/framerate)
		/// before pulling the frame from FInput. Otherwise you can rely on the value that was called automatically.
		/// </summary>
		public void TransferTimestamp()
		{
			FOutput.Image.Timestamp = FInput.Image.Timestamp;
		}

		/// <summary>
		/// Override this with false if your filter
		/// doesn't need to run every frame
		/// </summary>
		/// <returns></returns>
		virtual public bool IsFast()
		{
			return true;
		}

		bool FFlaggedForProcess = false;
		public bool FlaggedForProcess
		{
			get
			{
				return FFlaggedForProcess;
			}
		}
		public void FlagForProcess()
		{
			FFlaggedForProcess = true;
		}
		public void ClearFlagForProcess()
		{
			FFlaggedForProcess = false;
		}
	}
}
