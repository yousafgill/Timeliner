﻿using System;
using System.Drawing;

using Svg;

namespace Timeliner
{
	/// <summary>
	/// Parses a svg document and provides events.
	/// </summary>
	public class SvgValueWidget: SvgWidget
	{
		private SvgText Label;
		public Action OnValueChanged;	
		private bool FMouseDown = false;
		private PointF FMouseDownPos;
		private PointF FLastMousePos;
		public float Value
		{
			get; set;
		}
		
		public string Caption
		{
			get; set;
		}
		
		public SvgValueWidget(string label, float value): base()
		{
			Caption = label;
			
			Background.MouseScroll += Background_MouseScroll;
			Background.MouseOver += Background_MouseOver;
			Background.MouseOut += Background_MouseOut;
			//Background.CustomAttributes["capturemouse"] = "true";
			
			Label = new SvgText();
			Label.FontSize = 12;
			Label.X = 2;
			Label.Y = Label.FontSize + 2;
            Label.FontFamily = "Lucida Sans Unicode";
            //Label.ID ="/label";
            Label.CustomAttributes["pointer-events"] = "none";
            
            Value = value;
            
            UpdateLabel();
            
            this.Children.Add(Label);
		}

		void UpdateLabel()
		{
			Label.Text = Caption + ": " + string.Format("{0:0.00}", Value);
		}
		
		void Background_MouseScroll(object sender, MouseScrollArg e)
		{
			Value += (e.Scroll) / (120*10f);
				
			UpdateLabel();
			OnValueChanged();
		}
		
		void Background_MouseOver(object sender, EventArgs e)
		{
			Background.Fill = TimelinerColors.DarkGray;
		}
		
		void Background_MouseOut(object sender, EventArgs e)
		{
			Background.Fill = TimelinerColors.LightGray;
		}
		
		void Background_MouseUp(object sender, MouseArg e)
		{
			FMouseDown = false;
		}
	}
}
