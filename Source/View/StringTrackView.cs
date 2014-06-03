﻿using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;

using Posh;
using Svg;
using Svg.Transforms;
using VVVV.Core;
using VVVV.Core.Collections;
using VVVV.Core.Collections.Sync;
using VVVV.Core.Commands;

namespace Timeliner
{
	public class StringTrackView: TrackView
	{
		public EditableList<StringKeyframeView> Keyframes = new EditableList<StringKeyframeView>();
		
		public SvgLine KeyframeDefinition = new SvgLine();
        public SvgLine CollapsedKeyframeDefinition = new SvgLine();
		public SvgGroup KeyframeGroup = new SvgGroup();
		
		private Synchronizer<StringKeyframeView, TLStringKeyframe> KFSyncer;
		
		public SvgStringWidget StringEdit;
		private SvgText CurrentValue = new SvgText();
		
		public new TLStringTrack Model
        {
            get
            {
                return (TLStringTrack)base.Model;
            }
            protected set
            {
                base.Model = value;
            }
        }
		
		public StringTrackView(TLStringTrack track, TimelineView tv, RulerView rv)
			: base(track, tv, rv)
		{
			KFSyncer = Keyframes.SyncWith(Model.Keyframes,
			                              kf =>
			                              {
			                              	var kv = new StringKeyframeView(kf, this);
			                              	kv.AddToSceneGraphAt(KeyframeGroup);
			                              	return kv;
			                              },
			                              kv =>
			                              {
			                              	kv.Dispose();
			                              });
			
			
			Background.Click += Background_MouseClick;
			
			KeyframeDefinition.StartX = 0;
            KeyframeDefinition.StartY = -25f;
            KeyframeDefinition.EndX = 0;
            KeyframeDefinition.EndY = 25f;
			KeyframeDefinition.ID = Model.GetID() + "_KF";
			KeyframeDefinition.Transforms = new SvgTransformCollection();
			KeyframeDefinition.Transforms.Add(new SvgScale(1, 1));
            
            CollapsedKeyframeDefinition.ID = Model.GetID() + "_CKF";
            CollapsedKeyframeDefinition.StartX = 0;
            CollapsedKeyframeDefinition.StartY = -25f;
            CollapsedKeyframeDefinition.EndX = 0;
            CollapsedKeyframeDefinition.EndY = 25f;
            CollapsedKeyframeDefinition.Transforms = new SvgTransformCollection();
			CollapsedKeyframeDefinition.Transforms.Add(new SvgScale(1, 1));

			KeyframeGroup.ID = "Keyframes";
			
			CurrentValue.FontSize = 20;
            CurrentValue.X = 5;
            CurrentValue.CustomAttributes["class"] = "trackfont";
            CurrentValue.CustomAttributes["pointer-events"] = "none";
			CurrentValue.Y = 40;
						
			UpdateScene();
		}
		
		public override void Dispose()
		{
			Background.Click -= Background_MouseClick;
			

			StringEdit.OnValueChanged -= ChangeKeyframeText;
			
			base.Dispose();
		}
		
		#region build scenegraph		
		protected override void BuildSVG()
		{
			base.BuildSVG();
				
			KeyframeGroup.Children.Clear();
			
			Definitions.Children.Add(KeyframeDefinition);
            Definitions.Children.Add(CollapsedKeyframeDefinition);
			PanZoomGroup.Children.Add(KeyframeGroup);
			
			MainGroup.Children.Add(CurrentValue);
			
			
			//draw keyframes
			foreach (var keyframe in Keyframes)
				keyframe.AddToSceneGraphAt(KeyframeGroup);
		}
		#endregion
		
		#region update scenegraph
		public override void UpdateScene()
		{
			base.UpdateScene();
			
			CollapsedKeyframeDefinition.StartY = - Model.CollapsedHeight * PanZoomGroup.Transforms[1].Matrix.Elements[5];
			CollapsedKeyframeDefinition.EndY = CollapsedKeyframeDefinition.StartY + Model.Height.Value;
			
			foreach (var kf in Keyframes)
				kf.UpdateScene();
			
		}

		
		protected override void ApplyInverseScaling()
		{
			//apply inverse scaling to keyframes
			
			//pan/zoom
			var m = PanZoomGroup.Transforms[0].Matrix;
			var s1 = new SvgScale(m.Elements[0], m.Elements[3]);
			
			//min/max
			m = PanZoomGroup.Transforms[1].Matrix;
			var s2 = new SvgScale(m.Elements[0], m.Elements[3]);
			
			//trackheight
			m = TrackGroup.Transforms[0].Matrix;
			
			m.Multiply(s2.Matrix);
			m.Multiply(s1.Matrix);
			m.Invert();
			
			KeyframeDefinition.Transforms[0] = new SvgMatrix(new List<float>(m.Elements));
            CollapsedKeyframeDefinition.Transforms[0] = KeyframeDefinition.Transforms[0];
		}
		#endregion
		
		#region scenegraph eventhandler
		
		protected override void ChangeKeyframeTime()
		{
			History.Insert(Command.Set(Keyframes.ToList().First(x => x.Model.Selected.Value).Model.Time, TimeEdit.Value));
		}
		
		void ChangeKeyframeText(string newText)
		{
			var cmd = new CompoundCommand();
			
			foreach(var kf in Keyframes)
			{
				if (kf.Model.Selected.Value)
					cmd.Append(Command.Set(kf.Model.Text, newText));
			}
					
			History.Insert(cmd);
		}
		
		void Background_MouseClick(object sender, MouseArg e)
		{
			if(e.ClickCount >= 2)
			{
				var x = FRuler.XPosToTime(e.x);
				var y = YPosToValue(e.y);
				
				var kf = new TLStringKeyframe(x, "text");
				var cmd = Command.Add(this.Model.Keyframes, kf);
				History.Insert(cmd);
			}
		}
		#endregion
        
        protected override void FillTrackMenu()
        {
        }
        
        protected override void FillKeyframeMenu()
        {
            StringEdit = new SvgStringWidget(0, 20, "Text");
			StringEdit.OnValueChanged += ChangeKeyframeText;
			KeyframeMenu.AddItem(StringEdit);
        }
        
		public override void Evaluate()
		{
			CurrentValue.Text = Model.CurrentText;
		}
	}
}