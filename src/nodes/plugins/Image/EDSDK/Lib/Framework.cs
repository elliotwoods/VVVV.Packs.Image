using Canon.Eos.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.EDSDK
{
	class Context : IDisposable
	{
		static EosFramework FFramework = new EosFramework();
		public EosFramework Framework
		{
			get
			{
				return FFramework;
			}
		}

		public void Dispose()
		{
			FFramework.Dispose();
		}
	}
}
