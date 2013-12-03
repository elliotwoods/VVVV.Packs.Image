using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	/// <summary>
	/// The link used between EmguCV nodes in the VVVV graph
	/// </summary>
	public class CVImageLink : CVImageDoubleBuffer
	{
		#region Events

		#region ImageUpdate
		public event EventHandler ImageUpdate;

		public override void OnImageUpdate()
		{
			if (ImageUpdate == null)
				return;
			ImageUpdate(this, EventArgs.Empty);
		}
		#endregion

		#region ImageAttributesUpdate
		public event EventHandler<ImageAttributesChangedEventArgs> ImageAttributesUpdate;

		public override void OnImageAttributesUpdate(CVImageAttributes attributes)
		{
			if (ImageAttributesUpdate == null)
				return;
			ImageAttributesUpdate(this, new ImageAttributesChangedEventArgs(attributes));
		}
		#endregion

		#endregion
	}
}
