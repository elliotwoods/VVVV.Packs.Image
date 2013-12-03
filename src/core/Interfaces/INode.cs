using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public enum ThreadMode { Independant, UpstreamThread_experimental }

	public abstract class INode : IPluginEvaluate
	{
		/// <summary>
		/// The call from VVVV. This is handled by IGeneratorNode, IFilterNode, IDestinationNode, etc
		/// </summary>
		/// <param name="SpreadMax"></param>
		public abstract void Evaluate(int SpreadMax);

		/// <summary>
		/// The internal call to update. You need to override this in your node definition
		/// </summary>
		/// <param name="InstanceCount">SliceCount of FProcessor</param>
		/// <param name="SpreadChanged">true if instances in FProcessor have changed</param>
		protected abstract void Update(int InstanceCount, bool SpreadChanged);

		/// <summary>
		/// Returns whether we should count only the images input (i.e. not Evaluate's SpreadMax).
		/// This is set to true for nodes such as Pipet, where a spread of input values are applied to all images
		/// </summary>
		/// <returns>Only have 1 processing instance per image, regardless of other inputs</returns>
		protected virtual bool OneInstancePerImage()
		{
			return false;
		}
	}
}
