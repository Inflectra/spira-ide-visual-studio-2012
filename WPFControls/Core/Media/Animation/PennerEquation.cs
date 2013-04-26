﻿















using System;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms.ToolKit.Media.Animation
{
	public class PennerEquation : IterativeEquation<double>
	{
		#region Constructors

		internal PennerEquation(PennerEquationDelegate pennerImpl)
		{
			_pennerImpl = pennerImpl;
		}

		#endregion

		public override double Evaluate(TimeSpan currentTime, double from, double to, TimeSpan duration)
		{
			double t = currentTime.TotalSeconds;
			double b = from;
			double c = to - from;
			double d = duration.TotalSeconds;

			return _pennerImpl(t, b, c, d);
		}

		#region Private Fields

		private readonly PennerEquationDelegate _pennerImpl;

		#endregion

		#region PennerEquationDelegate Delegate

		internal delegate double PennerEquationDelegate(double t, double b, double c, double d);

		#endregion
	}
}