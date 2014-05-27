﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

using Posh;
using Svg;
using Svg.Transforms;
using VVVV.Core;

namespace Timeliner
{
    /// <summary>
    /// Description of RulerView.
    /// </summary>
    public class RulerView: TLViewBase
    {
        //MainGroup has a clippath set 
        public SvgClipPath RulerClipPath = new SvgClipPath();
        protected SvgRectangle ClipRect = new SvgRectangle();
		
        //MainGroup holds
        protected SvgRectangle Background = new SvgRectangle();
        private SvgText Label = new SvgText();
        private SvgRectangle LabelBackground = new SvgRectangle();
		
        public SvgGroup NumberGroup = new SvgGroup();
        public SvgGroup PanZoomGroup = new SvgGroup();
		
        public SvgRectangle LoopStart = new SvgRectangle();
        public SvgRectangle LoopEnd = new SvgRectangle();
        public SvgRectangle LoopRegion = new SvgRectangle();
		
        private const float CLeftOffset = 220;
        private const float CHandlerWidth = 20;
		
        private Matrix FView = new Matrix(Timer.PPS, 0, 0, 1, CLeftOffset, 0);
        private bool FViewChanged = true;
        private float FLastZoom;
		
        public SvgMatrix PanZoomMatrix;
		
        public new TLRuler Model
        {
            get
            {
                return (TLRuler)base.Model;
            }
            protected set
            {
                base.Model = value;
            }
        }
        
        public new TimelineView Parent
        {
            get
            {
                return (TimelineView)base.Parent;
            }
            protected set
            {
                base.Parent = value;
            }
        }
        
        public float Height
        {
            get {return Background.Height;}
        }
		
        public RulerView(TLRuler ruler, TimelineView tv)
            : base(ruler, tv)
        {
            Model = ruler;
            Parent = tv;
			
            PanZoomMatrix = new SvgMatrix(FView.Elements.ToList());
			
            MainGroup.Transforms = new SvgTransformCollection();
            MainGroup.Transforms.Add(new SvgTranslate(0, 0));
			
            var width = new SvgUnit(SvgUnitType.Percentage, 100);
			
            Background.Width = width;
            Background.Height = 25; 
            Background.ID = "bg";
            Background.CustomAttributes["class"] = "ruler";
			
            //register event handlers
            Background.MouseDown += Background_MouseDown;
            Background.MouseUp += Background_MouseUp;
            Background.MouseMove += Background_MouseMove;
			
            LabelBackground.Width = CLeftOffset;
            LabelBackground.Height = Background.Height;
            LabelBackground.CustomAttributes["class"] = "ruler";
						
            Label.FontSize = 20;
            Label.X = 55;
            Label.Y = Label.FontSize;
            Label.CustomAttributes["class"] = "time";
            Label.ID = Model.GetID() + "_label";
            Label.Text = "00:00:00:000";
            Label.CustomAttributes["class"] = "time";
			
            ClipRect.Width = width;
            ClipRect.Height = Background.Height;
            ClipRect.ID = "ClipRect";
			
            //document roots id is "svg". this is where the trackclips are added to
            RulerClipPath.ID = "svg_clip" + IDGenerator.NewID;
            RulerClipPath.Children.Add(ClipRect);
			
            var uri = new Uri("url(#" + RulerClipPath.ID + ")", UriKind.Relative);
            MainGroup.ClipPath = uri;
			
            NumberGroup.ID = "Ticks";
            NumberGroup.Transforms = new SvgTransformCollection();
            NumberGroup.Transforms.Add(PanZoomMatrix);
			
            PanZoomGroup.ID = "PanZoom";
            PanZoomGroup.Transforms = new SvgTransformCollection();
            PanZoomGroup.Transforms.Add(PanZoomMatrix);
			
            for (int i=0; i<70; i++)
            {
                var num = new SvgText(i.ToString());
                num.FontSize = 20;
                num.Y = num.FontSize;
                num.CustomAttributes["class"] = "time hair";
                num.Transforms = new SvgTransformCollection();
                num.Transforms.Add(new SvgTranslate(i));
                num.Transforms.Add(new SvgScale(1/Timer.PPS, 1));
                NumberGroup.Children.Add(num);
            }
			
            LoopStart.ID = "LoopStart";
            LoopStart.Width = 1/Timer.PPS * CHandlerWidth;
            LoopStart.Height = Background.Height;
            PanZoomGroup.Children.Add(LoopStart);
            LoopStart.MouseDown += (s, e) => Parent.Default_MouseDown(LoopStart, e);
            LoopStart.MouseUp += (s, e) => Parent.Default_MouseUp(LoopStart, e);
            LoopStart.MouseMove += (s, e) => Parent.Default_MouseMove(LoopStart, e);
            LoopStart.CustomAttributes["pointer-events"] = "fill";
            LoopStart.CustomAttributes["class"] = "loopcap";
			
            LoopEnd.ID = "LoopEnd";
            LoopEnd.Width = 1/Timer.PPS * CHandlerWidth;
            LoopEnd.Height = Background.Height;
            PanZoomGroup.Children.Add(LoopEnd);
            LoopEnd.MouseDown += (s, e) => Parent.Default_MouseDown(LoopEnd, e);
            LoopEnd.MouseUp += (s, e) => Parent.Default_MouseUp(LoopEnd, e);
            LoopEnd.MouseMove += (s, e) => Parent.Default_MouseMove(LoopEnd, e);
            LoopEnd.CustomAttributes["pointer-events"] = "fill";
            LoopEnd.CustomAttributes["class"] = "loopcap";
			
            LoopRegion.ID = "LoopRegion";
            LoopRegion.Y = Background.Height / 4;
            LoopRegion.Height = Background.Height / 2;
            LoopRegion.FillOpacity = 0.7f;
            LoopRegion.CustomAttributes["pointer-events"] = "none";
            LoopRegion.CustomAttributes["class"] = "loop";
            PanZoomGroup.Children.Add(LoopRegion);
            
            //init scalings
            PanZoom(0, 0, 0);
            UpdateScene();
        }
        
        public override void Dispose()
        {
            Background.MouseDown -= Background_MouseDown;
            Background.MouseMove -= Background_MouseMove;
            Background.MouseUp -= Background_MouseUp;
            
            base.Dispose();
        }
		
        #region update view
        public void PanZoom(float delta, float scale, float xPos)
        {
            var scaleX = 1 + scale*0.003f;
			
            //update view matrix
            var m = new Matrix(scaleX, 0, 0, 1, xPos - xPos*scaleX, 0);
            m.Multiply(FView);
            m.Translate(delta / m.Elements[0], 0);
            FView = m;
            PanZoomMatrix = new SvgMatrix(FView.Elements.ToList());
			
            FViewChanged = true;
        }
        #endregion
		
        #region build scenegraph
        protected override void BuildSVG()
        {
            Parent.SvgRoot.Children.Add(RulerClipPath);
			
            MainGroup.Children.Add(Background);
            MainGroup.Children.Add(PanZoomGroup);
            MainGroup.Children.Add(NumberGroup);
            MainGroup.Children.Add(LabelBackground);
            MainGroup.Children.Add(Label);
        }
		
        protected override void UnbuildSVG()
        {
            Parent.SvgRoot.Children.Remove(RulerClipPath);
            Parent.FTrackGroup.Children.Remove(MainGroup);
        }
        #endregion
		
        #region update scenegraph
        public override void UpdateScene()
        {
            base.UpdateScene();
			
            //PanZoomGroup
            PanZoomGroup.Transforms[0] = PanZoomMatrix;
			
            //TickNumGroup
            var unit = FView.Elements[0];
            //make sure ticks are only shifted within one unit range
            var off = FView.Elements[4] % unit;
            //make a copy of the above elements and just replace translation
            var el = new List<float>(FView.Elements);
            el[4] = off;
            NumberGroup.Transforms[0] = new SvgMatrix(el);
			
            //update number labels
            var p = new PointF[]{new PointF(0, 0)};
            FView.TransformPoints(p);
            var start = -(int) Math.Truncate(p[0].X / unit);
            //			System.Diagnostics.Debug.WriteLine(start + " + " + p[0].X + " + " + off + " + " + m.Elements[4]);
			
            var nums = NumberGroup.Children.Where(x => x is SvgText);
			
            foreach (SvgText num in nums)
            {
                var showMinus = start < 0 ? true : false;
                var time = Math.Abs(start);
			
                var ss = (int) (time % 60);
                var mm = (int) (time / 60 % 60);
                var h = (int) (time / 60 / 60 % 60);
                DateTime dt = new DateTime(2008, 1, 1, h + 1, mm, ss, 0);
				
                if (showMinus)
                    num.Text = "-" + dt.ToString("%s"); 
                else
                    num.Text = " " + dt.ToString("%s");
				
                start++;
            }
			
            if (FLastZoom != FView.Elements[0])
            {
                ApplyInverseScaling();
                FLastZoom = FView.Elements[0];
            }
			
            LoopStart.X = Model.LoopStart.Value;
            LoopEnd.X = Model.LoopEnd.Value - LoopEnd.Width;
            LoopRegion.X = LoopStart.X;
            LoopRegion.Width = LoopEnd.X - LoopRegion.X + LoopEnd.Width;
        }
		
        protected virtual void ApplyInverseScaling()
        {
            //apply inverse scaling to numbers
            
            //pan/zoom
			var m = PanZoomGroup.Transforms[0].Matrix;
			var inv = new SvgScale(1/m.Elements[0], 1);
			
            var nums = NumberGroup.Children.Where(x => x is SvgText);
            foreach (var num in nums)
                num.Transforms[1] = inv;
            
            //..to loopregion handler
            LoopStart.Width = 1/m.Elements[0] * CHandlerWidth;
            LoopEnd.Width = 1/m.Elements[0] * CHandlerWidth;
        }
        #endregion
		
        #region scenegraph eventhandler
        //dispatch events to parent
        void Background_MouseDown(object sender, MouseArg e)
        {
            Parent.Default_MouseDown(this, e);
        }
		
        void Background_MouseUp(object sender, MouseArg e)
        {
            Parent.Default_MouseUp(this, e);
        }
        
        void Background_MouseMove(object sender, MouseArg e)
        {
            Parent.Default_MouseMove(this, e);
        }
        #endregion
		
        public float XPosToTime(float x)
        {
            return (x - PanZoomMatrix.Matrix.Elements[4]) / PanZoomMatrix.Matrix.Elements[0];
        }
		
        public float TimeToXPos(float time)
        {
            return time * PanZoomMatrix.Matrix.Elements[0] + PanZoomMatrix.Matrix.Elements[4];
        }
		
        public float XDeltaToTime(float x)
        {
            return x * 1 / PanZoomMatrix.Matrix.Elements[0];
        }
		
        public RectangleF ToTrackRect(RectangleF rect)
        {
            //			var x1 = XPosToTime(rect.X);
            //			var y1 = YPosToValue(rect.Y);
            //			var width = XDeltaToTime(rect.Width);
            //			var height = YDeltaToValue(-rect.Height);
            //			
            return new RectangleF(); //x1, y1 - height, width, height);
        }
		
        public void Evaluate()
        {
            if (Parent.Timer.TimeDelta != 0 || FViewChanged)
            {
                Parent.TimeBar.X = TimeToXPos(Parent.Timer.Time);
            }
			
            if (Parent.Timer.TimeDelta != 0)
            {
                Label.Text = Parent.Timer.ToString();	
            }
			
            FViewChanged = false;
        }
    }
}
