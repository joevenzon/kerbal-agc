using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

namespace AGC
{
	public class AGCPart : PartModule
	{
		private Rect windowPos;

		private MicroLisp.LispValue stdlib = null;
		private string stdlib_contents = "";

		public class Process
		{
			public string name = "";
			public string input = "";
			public string contents = "";
			public string status = "";
			public MicroLisp.LispValue program = null;
		}

		private Process[] processes = new Process[4];

		[KSPField(isPersistant = false, guiActive = true, guiName = "Electricity Use")]
		public float electricity_draw = 0;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Computer On")]
		public bool is_active = false;

		[KSPField(isPersistant = false, guiActive = false)]
		public bool show_ui = false;

		private int tick_count = 0;
		private double runtime = 0; // in seconds

		private MicroLisp.MicroLisp lisp_context = null;

		public string serialized_environment = "";

		DeltaFlightCtrlState deltaFlightCtrlState = new DeltaFlightCtrlState();
		DeltaFlightCtrlState lastDeltaFlightCtrlState = new DeltaFlightCtrlState();
		Vessel attachedVessel = null;

		class DeltaFlightCtrlState
		{
			public Single deltaMainThrottle = 0;
			public Single deltaPitch = 0;
			public Single deltaRoll = 0;
			public Single deltaYaw = 0;
			public Single deltaX = 0;
			public Single deltaY = 0;
			public Single deltaZ = 0;
			public Single deltaKillRot = 0; // zero means no change, 1 means force enable, -1 means force disable
		}

		public AGCPart()
		{
			for (int i = 0; i < processes.Length; i++)
			{
				processes[i] = new Process();
			}
		}

		static private void debug (String msg)
		{
			print ("AGC: " + msg);
		}

		private void AddProcessGUI(string label, ref string process_name, ref string process_input, string process_info)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width (100.0F));
			process_name = GUILayout.TextField(process_name, GUILayout.Width(100.0F));
			process_input = GUILayout.TextField(process_input, GUILayout.Width(100.0F));
			GUILayout.Label(process_info, GUILayout.Width (200.0F));
			GUILayout.EndHorizontal();
		}
		
		private void WindowGUI(int windowID)
		{
			GUIStyle mySty = new GUIStyle(GUI.skin.button); 
			mySty.normal.textColor = mySty.focused.textColor = Color.white;
			mySty.hover.textColor = mySty.active.textColor = Color.yellow;
			mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
			mySty.padding = new RectOffset(8, 8, 8, 8);
			
			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width (100.0F));
			GUILayout.Label("Program", GUILayout.Width(100.0F));
			GUILayout.Label("Input", GUILayout.Width(100.0F));
			GUILayout.Label("Status", GUILayout.Width (200.0F));
			GUILayout.EndHorizontal();

			for (int i = 0; i < processes.Length; i++)
			{
				AddProcessGUI ("Process " + (i+1) + ":", ref processes[i].name, ref processes[i].input, processes[i].status);
			}

			string infostr = is_active ? "Running: " + tick_count.ToString () : "Off";
			GUILayout.Label (infostr);

			if (GUILayout.Button("Reboot",mySty,GUILayout.ExpandWidth(true)) && is_active)//GUILayout.Button is "true" when clicked
			{
				Shutdown ();
				Boot ();
			}
			if (GUILayout.Button("Debug",mySty,GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
			{
				/*KSP.IO.File.WriteAllText<AGC.AGCPart>(stdlib_contents, "debug-stdlib.txt", null);

				for (int i = 0; i < processes.Length; i++)
				{
					KSP.IO.File.WriteAllText<AGC.AGCPart>(processes[i].contents, "debug-process" + i.ToString() + ".txt", null);
				}*/

				if (lisp_context != null && lisp_context.globalEnv != null)
				{
					KSP.IO.File.WriteAllText<AGC.AGCPart>(lisp_context.globalEnv.ToString(), "debug.txt", null);
				}
			}
			GUILayout.EndVertical();
			
			GUI.DragWindow();
		}

		private void OnGUI()
		{
			if (vessel == null || !vessel.isActiveVessel || !show_ui) return;

			GUI.skin = HighLogic.Skin;
			windowPos = GUILayout.Window(1, windowPos, WindowGUI, "AGC", GUILayout.MinWidth(100));	 
		}

		[KSPEvent(guiActive = true, guiName = "Toggle Computer", active = true)]
		public void ToggleComputer ()
		{
			debug ("ToggleComputer");
			is_active = !is_active;

			if (is_active) Boot(); else Shutdown();
		}

		[KSPEvent(guiActive = true, guiName = "Toggle UI", active = true)]
		public void ToggleUI ()
		{
			debug ("ToggleUI");
			show_ui = !show_ui;
			if (show_ui) ShowUI(); else HideUI();
		}

		public void ShowUI ()
		{
			debug ("ShowUI");
			//RenderingManager.AddToPostDrawQueue (3, new Callback (drawGUI));//start the GUI

			if ((windowPos.x == 0) && (windowPos.y == 0)) 
			{
				//windowPos is used to position the GUI window, lets set it in the center of the screen
				windowPos = new Rect (50, 50, 10, 10);
			}
		}

		public void HideUI() 
		{
			debug ("HideUI");
			//RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI
		}

		public delegate void FuncVoid();
		public delegate TResult FuncWorkaround<T, TResult>(T arg);
		public delegate TResult FuncWorkaround<T1, T2, TResult>(T1 arg1, T2 arg2);
		public delegate TResult FuncWorkaround<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
		public delegate TResult FuncWorkaround<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

		MicroLisp.LispValue wrap_void_activeonly(FuncVoid func, List<MicroLisp.LispValue> args)
		{
			if (vessel.isActiveVessel)
			{
				func();
				return new MicroLisp.LispBool(true);
			}
			else
			{
				return new MicroLisp.LispBool(false);
			}
		}

		MicroLisp.LispValue Staging_ActivateStage(List<MicroLisp.LispValue> args)
		{
			if (args.Count != 1) throw new MicroLisp.EvalException("Staging.ActivateStage expected 1 argument");
			if (!(args[0] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException("Staging.ActivateStage expected a numeric argument");
			if (vessel.isActiveVessel)
			{
				Staging.ActivateStage(Convert.ToInt32((args[0] as MicroLisp.LispNumber).n));
				return new MicroLisp.LispBool(true);
			}
			else
			{
				return new MicroLisp.LispBool(false);
			}
		}

		MicroLisp.LispValue ActionGroups_SetGroup(KSPActionGroup group, List<MicroLisp.LispValue> args)
		{
			if (args.Count != 1) throw new MicroLisp.EvalException("ActionGroups.SetGroup expected 1 argument");
			if (!(args[0] is MicroLisp.LispBool)) throw new MicroLisp.EvalException("ActionGroups.SetGroup expected a boolean argument");
			vessel.ActionGroups.SetGroup(group, (args[0] as MicroLisp.LispBool).b);
			return new MicroLisp.LispBool(true);
		}

		// sets num if args holds an argument
		// always returns num
		MicroLisp.LispValue GetSetNumber(ref Single num, List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count > 1) throw new MicroLisp.EvalException(str + ": expected zero to one arguments");
			if (args.Count == 1)
			{
				if (!(args[0] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a numeric argument");
				num = (Single)(args[0] as MicroLisp.LispNumber).n;
			}

			return new MicroLisp.LispNumber((double) num);
		}

		MicroLisp.LispValue GetSetNumberDelta(Single input, Single lastDelta, ref Single deltaOut, List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count > 1) throw new MicroLisp.EvalException(str + ": expected zero to one arguments");
			if (args.Count == 1)
			{
				if (!(args[0] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a numeric argument");
				deltaOut = (Single)(args[0] as MicroLisp.LispNumber).n - (input - lastDelta);
			}

			// not sure if I should return input or input-lastDelta...
			return new MicroLisp.LispNumber((double) input-lastDelta);
		}

		MicroLisp.LispValue GetSetBoolDelta(Boolean input, Single lastDelta, ref Single deltaOut, List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count > 1) throw new MicroLisp.EvalException(str + ": expected zero to one arguments");
			if (args.Count == 1)
			{
				if (!(args[0] is MicroLisp.LispBool)) throw new MicroLisp.EvalException(str + ": expected a boolean argument");
				deltaOut = (args[0] as MicroLisp.LispBool).b ? 1 : -1;
			}

			return new MicroLisp.LispBool((bool) input);
		}

		MicroLisp.LispValue GetVector3(Vector3 v)
		{
			MicroLisp.LispList result = new MicroLisp.LispList();
			result.list.Add(new MicroLisp.LispNumber(v.x));
			result.list.Add(new MicroLisp.LispNumber(v.y));
			result.list.Add(new MicroLisp.LispNumber(v.z));
			return result;
		}

		MicroLisp.LispValue GetVector3d(Vector3d v)
		{
			MicroLisp.LispList result = new MicroLisp.LispList();
			result.list.Add(new MicroLisp.LispNumber(v.x));
			result.list.Add(new MicroLisp.LispNumber(v.y));
			result.list.Add(new MicroLisp.LispNumber(v.z));
			return result;
		}

		double UnwrapNumber(List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count != 1) throw new MicroLisp.EvalException(str + ": expected one argument");
			if (!(args[0] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a numeric argument");
			return (args[0] as MicroLisp.LispNumber).n;
		}

		double UnwrapNumberN(List<MicroLisp.LispValue> args, int index, string str)
		{
			if (args.Count <= index) throw new MicroLisp.EvalException(str + ": expected at least " + (index+1) + " arguments");
			if (!(args[index] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a numeric argument");
			return (args[index] as MicroLisp.LispNumber).n;
		}

		Vector3 UnwrapVector3(List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count != 1) throw new MicroLisp.EvalException(str + ": expected one argument");
			if (!(args[0] is MicroLisp.LispList)) throw new MicroLisp.EvalException(str + ": expected a list argument");
			List<MicroLisp.LispValue> list = (args[0] as MicroLisp.LispList).list;
			if (list.Count != 3) throw new MicroLisp.EvalException(str + ": expected a 3-element list");
			if (!(list[0] is MicroLisp.LispNumber) || !(list[1] is MicroLisp.LispNumber) || !(list[2] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a 3-element numeric list");
			return new Vector3((float)(list[0] as MicroLisp.LispNumber).n, (float)(list[1] as MicroLisp.LispNumber).n, (float)(list[2] as MicroLisp.LispNumber).n);
		}

		Vector3 UnwrapVector3at(List<MicroLisp.LispValue> args, int index, string str)
		{
			if (args.Count <= index) throw new MicroLisp.EvalException(str + ": expected at least " + (index+1) + " arguments");
			if (!(args[index] is MicroLisp.LispList)) throw new MicroLisp.EvalException(str + ": expected a list argument");
			List<MicroLisp.LispValue> list = (args[index] as MicroLisp.LispList).list;
			if (list.Count != 3) throw new MicroLisp.EvalException(str + ": expected a 3-element list");
			if (!(list[0] is MicroLisp.LispNumber) || !(list[1] is MicroLisp.LispNumber) || !(list[2] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a 3-element numeric list");
			return new Vector3((float)(list[0] as MicroLisp.LispNumber).n, (float)(list[1] as MicroLisp.LispNumber).n, (float)(list[2] as MicroLisp.LispNumber).n);
		}

		Vector3d UnwrapVector3d(List<MicroLisp.LispValue> args, string str)
		{
			if (args.Count != 1) throw new MicroLisp.EvalException(str + ": expected one argument");
			if (!(args[0] is MicroLisp.LispList)) throw new MicroLisp.EvalException(str + ": expected a list argument");
			List<MicroLisp.LispValue> list = (args[0] as MicroLisp.LispList).list;
			if (list.Count != 3) throw new MicroLisp.EvalException(str + ": expected a 3-element list");
			if (!(list[0] is MicroLisp.LispNumber) || !(list[1] is MicroLisp.LispNumber) || !(list[2] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a 3-element numeric list");
			return new Vector3d((list[0] as MicroLisp.LispNumber).n, (list[1] as MicroLisp.LispNumber).n, (list[2] as MicroLisp.LispNumber).n);
		}

		Quaternion UnwrapQuaternion(List<MicroLisp.LispValue> args, int index, string str)
		{
			if (args.Count <= index) throw new MicroLisp.EvalException(str + ": expected at least " + (index+1) + " arguments");
			if (!(args[index] is MicroLisp.LispList)) throw new MicroLisp.EvalException(str + ": expected a list argument");
			List<MicroLisp.LispValue> list = (args[index] as MicroLisp.LispList).list;
			if (list.Count != 4) throw new MicroLisp.EvalException(str + ": expected a 4-element list");
			if (!(list[0] is MicroLisp.LispNumber) || !(list[1] is MicroLisp.LispNumber) || !(list[2] is MicroLisp.LispNumber) || !(list[3] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected a 4-element numeric list");
			return new Quaternion((float)(list[0] as MicroLisp.LispNumber).n, (float)(list[1] as MicroLisp.LispNumber).n, (float)(list[2] as MicroLisp.LispNumber).n, (float)(list[3] as MicroLisp.LispNumber).n);
		}

		MicroLisp.LispValue WrapQuaternion(Quaternion q)
		{
			MicroLisp.LispList result = new MicroLisp.LispList();
			result.list.Add(new MicroLisp.LispNumber(q.x));
			result.list.Add(new MicroLisp.LispNumber(q.y));
			result.list.Add(new MicroLisp.LispNumber(q.z));
			result.list.Add(new MicroLisp.LispNumber(q.w));
			return result;
		}

		MicroLisp.LispValue WrapTwoArgNumeric(FuncWorkaround<double,double,double> func, string str, List<MicroLisp.LispValue> args)
		{
			if (args.Count != 2) throw new MicroLisp.EvalException(str + ": expected two arguments");
			if (!(args[0] is MicroLisp.LispNumber) || !(args[1] is MicroLisp.LispNumber)) throw new MicroLisp.EvalException(str + ": expected two numeric arguments");
			return new MicroLisp.LispNumber(func((args[0] as MicroLisp.LispNumber).n, (args[1] as MicroLisp.LispNumber).n));
		}

		void SetupOrbitReadonlyCallbacks(MicroLisp.Environment env, string prefix, Orbit o)
		{
			if (o == null) throw new MicroLisp.EvalException("orbit is null");

			// Vector3d -> Double
			env.Add(prefix+".getOrbitalSpeedAtPos", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getOrbitalSpeedAtPos(UnwrapVector3d(args, "getOrbitalSpeedAtPos")))));
			env.Add(prefix+".getOrbitalSpeedAtRelativePos", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getOrbitalSpeedAtRelativePos(UnwrapVector3d(args, "getOrbitalSpeedAtRelativePos")))));
			env.Add(prefix+".GetTrueAnomalyOfZupVector", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.GetTrueAnomalyOfZupVector(UnwrapVector3d(args, "GetTrueAnomalyOfZupVector")))));

			// Double -> Double
			env.Add(prefix+".getObtAtUT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getObtAtUT(UnwrapNumber(args, "getObtAtUT")))));
			env.Add(prefix+".getObTAtMeanAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getObTAtMeanAnomaly(UnwrapNumber(args, "getObTAtMeanAnomaly")))));
			env.Add(prefix+".GetEccentricAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.GetEccentricAnomaly(UnwrapNumber(args, "GetEccentricAnomaly")))));
			env.Add(prefix+".RadiusAtTrueAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.RadiusAtTrueAnomaly(UnwrapNumber(args, "RadiusAtTrueAnomaly")))));
			env.Add(prefix+".TrueAnomalyAtRadius", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.TrueAnomalyAtRadius(UnwrapNumber(args, "TrueAnomalyAtRadius")))));
			env.Add(prefix+".TrueAnomalyAtT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.TrueAnomalyAtT(UnwrapNumber(args, "TrueAnomalyAtT")))));
			env.Add(prefix+".getOrbitalSpeedAt", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getOrbitalSpeedAt(UnwrapNumber(args, "getOrbitalSpeedAt")))));
			env.Add(prefix+".getOrbitalSpeedAtDistance", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getOrbitalSpeedAtDistance(UnwrapNumber(args, "getOrbitalSpeedAtDistance")))));
			env.Add(prefix+".getTrueAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.getTrueAnomaly(UnwrapNumber(args, "getTrueAnomaly")))));

			// Double -> Vector3d
			env.Add(prefix+".getPositionAtT", new MicroLisp.ExternalFunc(args => GetVector3d(o.getPositionAtT(UnwrapNumber(args, "getPositionAtT")))));
			env.Add(prefix+".getPositionFromEccAnomaly", new MicroLisp.ExternalFunc(args => GetVector3d(o.getPositionFromEccAnomaly(UnwrapNumber(args, "getPositionFromEccAnomaly")))));
			env.Add(prefix+".getPositionFromMeanAnomaly", new MicroLisp.ExternalFunc(args => GetVector3d(o.getPositionFromMeanAnomaly(UnwrapNumber(args, "getPositionFromMeanAnomaly")))));
			env.Add(prefix+".getPositionFromTrueAnomaly", new MicroLisp.ExternalFunc(args => GetVector3d(o.getPositionFromTrueAnomaly(UnwrapNumber(args, "getPositionFromTrueAnomaly")))));
			env.Add(prefix+".getRelativePositionAtT", new MicroLisp.ExternalFunc(args => GetVector3d(o.getRelativePositionAtT(UnwrapNumber(args, "getRelativePositionAtT")))));
			env.Add(prefix+".GetFrameVelAtUT", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetFrameVelAtUT(UnwrapNumber(args, "GetFrameVelAtUT")))));
			env.Add(prefix+".getOrbitalVelocityAtObT", new MicroLisp.ExternalFunc(args => GetVector3d(o.getOrbitalVelocityAtObT(UnwrapNumber(args, "getOrbitalVelocityAtObT")))));
			env.Add(prefix+".getTruePositionAtUT", new MicroLisp.ExternalFunc(args => GetVector3d(o.getOrbitalVelocityAtObT(UnwrapNumber(args, "getTruePositionAtUT")))));

			// (Double -> Double) -> Double
			env.Add(prefix+".GetDTforTrueAnomaly", new MicroLisp.ExternalFunc(args => WrapTwoArgNumeric(o.GetDTforTrueAnomaly, "GetDTforTrueAnomaly", args)));
			env.Add(prefix+".GetUTforTrueAnomaly", new MicroLisp.ExternalFunc(args => WrapTwoArgNumeric(o.GetUTforTrueAnomaly, "GetUTforTrueAnomaly", args)));
			env.Add(prefix+".GetMeanAnomaly", new MicroLisp.ExternalFunc(args => WrapTwoArgNumeric(o.GetMeanAnomaly, "GetMeanAnomaly", args)));

			env.Add(prefix+".ApA", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ApA))); // apoapsis altitude
			env.Add(prefix+".ApR", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ApR))); // apoapsis radius
			env.Add(prefix+".PeA", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.PeA)));
			env.Add(prefix+".PeR", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.PeR)));
			env.Add(prefix+".semiLatusRectum", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.semiLatusRectum)));
			env.Add(prefix+".semiMinorAxis", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.semiMinorAxis)));
			env.Add(prefix+".altitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.altitude)));
			env.Add(prefix+".argumentOfPeriapsis", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.argumentOfPeriapsis)));
			env.Add(prefix+".ClAppr", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ClAppr)));
			env.Add(prefix+".ClEctr1", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ClEctr1)));
			env.Add(prefix+".ClEctr2", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ClEctr2)));
			env.Add(prefix+".closestTgtApprUT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.closestTgtApprUT)));
			env.Add(prefix+".CrAppr", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.CrAppr)));
			env.Add(prefix+".E", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.E)));
			env.Add(prefix+".eccentricAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.eccentricAnomaly)));
			env.Add(prefix+".eccentricity", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.eccentricity)));
			env.Add(prefix+".EndUT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.EndUT)));
			env.Add(prefix+".epoch", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.epoch)));
			env.Add(prefix+".FEVp", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.FEVp)));
			env.Add(prefix+".FEVs", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.FEVs)));
			env.Add(prefix+".fromE", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.fromE)));
			env.Add(prefix+".fromV", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.fromV)));
			env.Add(prefix+".inclination", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.inclination)));
			env.Add(prefix+".LAN", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.LAN)));
			env.Add(prefix+".mag", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.mag)));
			env.Add(prefix+".meanAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.meanAnomaly)));
			env.Add(prefix+".meanAnomalyAtEpoch", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.meanAnomalyAtEpoch)));
			env.Add(prefix+".nearestTT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.nearestTT)));
			env.Add(prefix+".nextTT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.nextTT)));
			env.Add(prefix+".ObT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ObT)));
			env.Add(prefix+".ObTAtEpoch", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.ObTAtEpoch)));
			env.Add(prefix+".orbitalEnergy", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.orbitalEnergy)));
			env.Add(prefix+".orbitalSpeed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.orbitalSpeed)));
			env.Add(prefix+".orbitPercent", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.orbitPercent)));
			env.Add(prefix+".period", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.period)));
			env.Add(prefix+".radius", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.radius)));
			env.Add(prefix+".sampleInterval", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.sampleInterval)));
			env.Add(prefix+".semiMajorAxis", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.semiMajorAxis)));
			env.Add(prefix+".SEVp", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.SEVp)));
			env.Add(prefix+".SEVs", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.SEVs)));
			env.Add(prefix+".StartUT", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.StartUT)));
			env.Add(prefix+".timeToAp", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.timeToAp)));
			env.Add(prefix+".timeToPe", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.timeToPe)));
			env.Add(prefix+".timeToTransition1", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.timeToTransition1)));
			env.Add(prefix+".timeToTransition2", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.timeToTransition2)));
			env.Add(prefix+".toE", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.toE)));
			env.Add(prefix+".toV", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.toV)));
			env.Add(prefix+".trueAnomaly", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.trueAnomaly)));
			env.Add(prefix+".UTappr", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.UTappr)));
			env.Add(prefix+".UTsoi", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.UTsoi)));
			env.Add(prefix+".V", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(o.V)));

			env.Add(prefix+".an", new MicroLisp.ExternalFunc(args => GetVector3d(o.an)));
			env.Add(prefix+".eccVec", new MicroLisp.ExternalFunc(args => GetVector3d(o.eccVec)));
			env.Add(prefix+".h", new MicroLisp.ExternalFunc(args => GetVector3d(o.h)));
			env.Add(prefix+".pos", new MicroLisp.ExternalFunc(args => GetVector3d(o.pos)));
			env.Add(prefix+".secondaryPosAtTransition1", new MicroLisp.ExternalFunc(args => GetVector3d(o.secondaryPosAtTransition1)));
			env.Add(prefix+".secondaryPosAtTransition2", new MicroLisp.ExternalFunc(args => GetVector3d(o.secondaryPosAtTransition2)));
			env.Add(prefix+".vel", new MicroLisp.ExternalFunc(args => GetVector3d(o.vel)));
			env.Add(prefix+".GetANVector", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetANVector())));
			env.Add(prefix+".GetEccVector", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetEccVector())));
			env.Add(prefix+".GetFrameVel", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetFrameVel())));
			env.Add(prefix+".GetOrbitNormal", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetOrbitNormal())));
			env.Add(prefix+".GetRelativeVel", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetRelativeVel())));
			env.Add(prefix+".GetVel", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetVel())));
			env.Add(prefix+".GetWorldSpaceVel", new MicroLisp.ExternalFunc(args => GetVector3d(o.GetWorldSpaceVel())));
		}

		void SetupVesselReadonlyCallbacks(MicroLisp.Environment env, string prefix, Vessel v)
		{
			if (v != null)
			{
				env.Add(prefix+".verticalSpeed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.verticalSpeed)));
				env.Add(prefix+".staticPressure", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.staticPressure)));
				env.Add(prefix+".geeForce", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.geeForce)));
				env.Add(prefix+".currentStage", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.currentStage)));
				env.Add(prefix+".specificAcceleration", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.specificAcceleration)));
				env.Add(prefix+".heightFromTerrain", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.heightFromTerrain)));
				env.Add(prefix+".pqsAltitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.pqsAltitude)));
				env.Add(prefix+".terrainAltitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.terrainAltitude)));
				env.Add(prefix+".heightFromSurface", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.heightFromSurface)));
				env.Add(prefix+".Landed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispBool(v.Landed)));
				env.Add(prefix+".missionTime", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.missionTime)));
				env.Add(prefix+".longitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.longitude)));
				env.Add(prefix+".latitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.latitude)));
				env.Add(prefix+".altitude", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.altitude)));
				env.Add(prefix+".GetTotalMass", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.GetTotalMass())));
				env.Add(prefix+".obt_speed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.obt_speed)));
				env.Add(prefix+".srfSpeed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.srfSpeed)));
				env.Add(prefix+".horizontalSrfSpeed", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.horizontalSrfSpeed)));
				env.Add(prefix+".GetTotalMass", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(v.GetTotalMass())));

				env.Add(prefix+".acceleration", new MicroLisp.ExternalFunc(args => GetVector3d(v.acceleration)));
				env.Add(prefix+".angularMomentum", new MicroLisp.ExternalFunc(args => GetVector3(v.angularMomentum)));
				env.Add(prefix+".angularVelocity", new MicroLisp.ExternalFunc(args => GetVector3(v.angularVelocity)));
				env.Add(prefix+".CoM", new MicroLisp.ExternalFunc(args => GetVector3(v.CoM)));
				env.Add(prefix+".MOI", new MicroLisp.ExternalFunc(args => GetVector3(v.MOI)));
				env.Add(prefix+".upAxis", new MicroLisp.ExternalFunc(args => GetVector3d(v.upAxis)));
				env.Add(prefix+".vesselTransform.eulerAngles", new MicroLisp.ExternalFunc(args => GetVector3(v.vesselTransform.eulerAngles)));
				env.Add(prefix+".obt_velocity", new MicroLisp.ExternalFunc(args => GetVector3(v.obt_velocity)));
				env.Add(prefix+".srf_velocity", new MicroLisp.ExternalFunc(args => GetVector3(v.srf_velocity)));
				env.Add(prefix+".vesselTransform.forward", new MicroLisp.ExternalFunc(args => GetVector3(v.vesselTransform.forward)));
				env.Add(prefix+".vesselTransform.rotation", new MicroLisp.ExternalFunc(args => WrapQuaternion(v.vesselTransform.rotation)));

				SetupOrbitReadonlyCallbacks(env, prefix+".orbit", v.orbit);
			}
		}

		void SetupLispEnvironment(MicroLisp.Environment env)
		{
			// Quaternions
			env.Add("quatEulerAngles", new MicroLisp.ExternalFunc(args => GetVector3(UnwrapQuaternion(args, 0, "quatEulerAngles").eulerAngles)));
			env.Add("quatMul", new MicroLisp.ExternalFunc(args => WrapQuaternion(UnwrapQuaternion(args, 0, "quatMul") * UnwrapQuaternion(args, 1, "quatMul"))));
			env.Add("quatAngle", new MicroLisp.ExternalFunc(args => new MicroLisp.LispNumber(Quaternion.Angle(UnwrapQuaternion(args, 0, "quatAngle"), UnwrapQuaternion(args, 1, "quatAngle")))));
			env.Add("quatEuler", new MicroLisp.ExternalFunc(args => WrapQuaternion(Quaternion.Euler(UnwrapVector3(args, "quatEuler")))));
			env.Add("quatInverse", new MicroLisp.ExternalFunc(args => WrapQuaternion(Quaternion.Inverse(UnwrapQuaternion(args, 0, "quatInverse")))));
			env.Add("quatSlerp", new MicroLisp.ExternalFunc(args => WrapQuaternion(Quaternion.Slerp(UnwrapQuaternion(args,0,"quatSlerp"),UnwrapQuaternion(args,1,"quatSlerp"),(float)UnwrapNumberN(args,2,"quatSlerp")))));
			env.Add("quatMulVec", new MicroLisp.ExternalFunc(args => GetVector3(UnwrapQuaternion(args, 0, "quatMulVec") * UnwrapVector3at(args, 1, "quatMulVec"))));

			// Staging
			env.Add("Staging.ActivateNextStage", new MicroLisp.ExternalFunc(args => wrap_void_activeonly(Staging.ActivateNextStage, args)));
			env.Add("Staging.ActivateStage", new MicroLisp.ExternalFunc(Staging_ActivateStage));

			// Action groups
			env.Add("ActionGroups.SetGroupStage", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Stage, args)));
			env.Add("ActionGroups.SetGroupGear", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Gear, args)));
			env.Add("ActionGroups.SetGroupLight", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Light, args)));
			env.Add("ActionGroups.SetGroupRCS", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.RCS, args)));
			env.Add("ActionGroups.SetGroupSAS", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.SAS, args)));
			env.Add("ActionGroups.SetGroupBrakes", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Brakes, args)));
			env.Add("ActionGroups.SetGroupAbort", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Abort, args)));
			env.Add("ActionGroups.SetGroupCustom01", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom01, args)));
			env.Add("ActionGroups.SetGroupCustom02", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom02, args)));
			env.Add("ActionGroups.SetGroupCustom03", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom03, args)));
			env.Add("ActionGroups.SetGroupCustom04", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom04, args)));
			env.Add("ActionGroups.SetGroupCustom05", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom05, args)));
			env.Add("ActionGroups.SetGroupCustom06", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom06, args)));
			env.Add("ActionGroups.SetGroupCustom07", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom07, args)));
			env.Add("ActionGroups.SetGroupCustom08", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom08, args)));
			env.Add("ActionGroups.SetGroupCustom09", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom09, args)));
			env.Add("ActionGroups.SetGroupCustom10", new MicroLisp.ExternalFunc(args => ActionGroups_SetGroup(KSPActionGroup.Custom10, args)));

			// Vessel
			SetupVesselReadonlyCallbacks(env, "vessel", vessel);
			env.Add("vessel.hasTarget", new MicroLisp.ExternalFunc(args => new MicroLisp.LispBool(vessel.isActiveVessel && FlightGlobals.fetch.VesselTarget != null && (FlightGlobals.fetch.VesselTarget is Vessel))));
			SetupVesselReadonlyCallbacks(env, "target", (FlightGlobals.fetch.VesselTarget != null && FlightGlobals.fetch.VesselTarget is Vessel) ? FlightGlobals.fetch.VesselTarget as Vessel : null);

			// FlightCtrlState
			env.Add("ctrlState.mainThrottle", new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.mainThrottle, lastDeltaFlightCtrlState.deltaMainThrottle, ref deltaFlightCtrlState.deltaMainThrottle, args, "ctrlState.mainThrottle")));
			env.Add("ctrlState.pitch", 	new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.pitch, 	lastDeltaFlightCtrlState.deltaPitch,	ref deltaFlightCtrlState.deltaPitch,	args, "ctrlState.pitch")));
			env.Add("ctrlState.roll", 	new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.roll, 	lastDeltaFlightCtrlState.deltaRoll, 	ref deltaFlightCtrlState.deltaRoll, 	args, "ctrlState.roll")));
			env.Add("ctrlState.yaw", 	new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.yaw, 		lastDeltaFlightCtrlState.deltaYaw, 		ref deltaFlightCtrlState.deltaYaw, 		args, "ctrlState.yaw")));
			env.Add("ctrlState.X", new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.X, lastDeltaFlightCtrlState.deltaX, ref deltaFlightCtrlState.deltaX, args, "ctrlState.X")));
			env.Add("ctrlState.Y", new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.Y, lastDeltaFlightCtrlState.deltaY, ref deltaFlightCtrlState.deltaY, args, "ctrlState.Y")));
			env.Add("ctrlState.Z", new MicroLisp.ExternalFunc(args => GetSetNumberDelta(vessel.ctrlState.Z, lastDeltaFlightCtrlState.deltaZ, ref deltaFlightCtrlState.deltaZ, args, "ctrlState.Z")));
			env.Add("ctrlState.killRot", new MicroLisp.ExternalFunc(args => GetSetBoolDelta(vessel.ctrlState.killRot, lastDeltaFlightCtrlState.deltaKillRot, ref deltaFlightCtrlState.deltaKillRot, args, "ctrlState.killRot")));
		}

		public void Boot()
		{
			debug ("Bootup");

			tick_count = 0;
			runtime = 0;

			foreach (var process in processes)
			{
				process.program = null;
			}

			lisp_context = new MicroLisp.MicroLisp();
			SetupLispEnvironment(lisp_context.globalEnv);

			// load the standard library file first
			string stdlib_status = "";
			stdlib = LoadAndParse("stdlib.txt", ref stdlib_status, ref stdlib_contents);

			foreach (var process in processes)
			{
				if (process.name != "")
				{
					process.program = LoadAndParse (process.name + ".txt", ref process.status, ref process.contents);
				}
			}
		}

		MicroLisp.LispValue LoadAndParse(string filename, ref string status, ref string contents)
		{
			if (filename != "" && lisp_context != null)
			{
				try
				{
					contents = KSP.IO.File.ReadAllText<AGC.AGCPart>(filename, null);
					return Parse(filename, contents, ref status);
				}
				catch (Exception e)
				{
					debug ("unable to read file " + filename);
					status = "file not found";
					return null;
				}
			}
			else
			{
				status = "no program loaded";
				return null;
			}
		}

		// name is used for error reporting only
		MicroLisp.LispValue Parse(string name, string contents, ref string status)
		{
			if (contents != "" && lisp_context != null)
			{
				MicroLisp.ParseResult result = lisp_context.Parse(contents);
				if (result.error == "")
				{
					debug (name + ": parse successful");
					status = "loaded";
					return result.value;
				}
				else
				{
					debug (name + ": parse error");
					debug ("error: " + result.error);
					status = "syntax error";
					return null;
				}
			}
			else
			{
				status = "no program loaded";
				return null;
			}
		}

		public void Shutdown()
		{
			debug ("Shutdown");

			foreach (var process in processes)
			{
				process.program = null;
				process.contents = "";
				process.status = "";
			}
			lisp_context = null;
		}

		public void OnFlyByWire(FlightCtrlState c)
		{
			c.mainThrottle += deltaFlightCtrlState.deltaMainThrottle;
			c.pitch += deltaFlightCtrlState.deltaPitch;
			c.roll += deltaFlightCtrlState.deltaRoll;
			c.yaw += deltaFlightCtrlState.deltaYaw;
			c.X += deltaFlightCtrlState.deltaX;
			c.Y += deltaFlightCtrlState.deltaY;
			c.Z += deltaFlightCtrlState.deltaZ;

			if (deltaFlightCtrlState.deltaKillRot == 1)
			{
				c.killRot = true;
			}
			else if (deltaFlightCtrlState.deltaKillRot == -1)
			{
				c.killRot = false;
			}

			// now that we've consumed them, reset deltas to zero
			lastDeltaFlightCtrlState = deltaFlightCtrlState;
			deltaFlightCtrlState = new DeltaFlightCtrlState();
		}

		public override void OnUpdate()
		{
			if (attachedVessel != vessel)
			{
				debug ("Vessel change");

				if (attachedVessel != null)
				{
					attachedVessel.OnFlyByWire -= OnFlyByWire;
					attachedVessel = null;
				}

				if (vessel != null)
				{
					attachedVessel = vessel;
					attachedVessel.OnFlyByWire += OnFlyByWire;
				}
			}
		}

		public void FixedUpdate()
		{
			float time = TimeWarp.fixedDeltaTime;
			float electricReq = 0.005f * time;
			float result = 0;
			electricity_draw = 0;

			if (is_active)
			{
				try
				{
					result = part.RequestResource("ElectricCharge", electricReq) / electricReq;
				}
				catch (Exception e)
				{
					debug ("RequestResource failed");
				}
				
				if (result < 0.5)
				{
					// no power
					if (is_active)
					{
						debug ("power starvation");
						is_active = false;
						Shutdown ();
					}
				}
				else
				{
					electricity_draw = electricReq;
				}
			}

			if (is_active && lisp_context == null)
			{
				// need to boot up!
				debug ("warm boot");
				Boot ();
			}

			if (is_active)
			{
				if (time > 0)
				{
					runtime += time;

					Tick(time);
				}
			}
		}

		void Tick(double dt)
		{
			if (lisp_context != null && stdlib != null)
			{
				tick_count++;

				try
				{
					lisp_context.Eval(stdlib);
				}
				catch (MicroLisp.EvalException e)
				{
					debug("error: stdlib: " + e.Message);
					debug("parse result follows: " + stdlib.ToString());
				}

				foreach (var process in processes)
				{
					TickProcess (process.program, process.input, ref process.status, dt);
				}
			}
			else
			{
				foreach (var process in processes)
				{
					process.status = "xxxxx";
				}
			}
		}

		bool TryGetVariable(string symbol, ref string out_var)
		{
			MicroLisp.LispValue value;
			if (lisp_context.globalEnv.TryGetValue(symbol, out value))
			{
				out_var = value.ToString ();
				return true;
			}
			else
			{
				return false;
			}
		}

		void TickProcess(MicroLisp.LispValue process, string input, ref string status, double dt)
		{
			if (lisp_context != null)
			{
				if (process == null)
				{
					status = "no program";
				}
				else
				{
					try
					{
						double input_number = ToDoubleOrZero(input);
						lisp_context.globalEnv.Add("AGC.Input", new MicroLisp.LispNumber(input_number));
						lisp_context.globalEnv.Add("AGC.dt", new MicroLisp.LispNumber(dt));
						lisp_context.globalEnv.Add("AGC.TickCount", new MicroLisp.LispNumber(tick_count));
						lisp_context.globalEnv.Add("AGC.Runtime", new MicroLisp.LispNumber(runtime));
						lisp_context.Eval(process);
						if (!TryGetVariable("AGC.Status", ref status))
						{
							status = "-----";
						}
					}
					catch (MicroLisp.EvalException e)
					{
						status = "error: " + e.Message;
						debug(status);
					}
				}
			}
		}

		static private double ToDoubleOrZero(string str)
		{
			double result = 0;
			try 
			{
				result = Convert.ToDouble(str);
			}
			catch (Exception e)
			{
				// who cares
			}
			return result;
		}

		public override void OnLoad(ConfigNode node)
		{
			if (vessel == null) return;

			debug("OnLoad");
			Shutdown();

			// read from the ConfigNode
			try
			{
				if (node.HasNode("AGC"))
				{
					ConfigNode root = node.GetNode("AGC");
					if (root != null)
					{
						stdlib_contents = ConfigNodeGetValue(root, "stdlib_contents");
						is_active = (ConfigNodeGetValue(root, "is_active") == "1");
						show_ui = (ConfigNodeGetValue(root, "show_ui") == "1");
						tick_count = (int)ToDoubleOrZero(ConfigNodeGetValue(root, "tick_count"));
						runtime = ToDoubleOrZero(ConfigNodeGetValue(root, "runtime"));
						serialized_environment = ConfigNodeGetValue(root, "serialized_environment");

						foreach (var procnode in root.GetNodes("Process"))
						{
							int i = (int)ToDoubleOrZero(ConfigNodeGetValue(procnode, "index"));
							Process process = processes[i];
							process.name = ConfigNodeGetValue(procnode, "name");
							debug("Loaded program " + i + ": " + process.name);
							process.input = ConfigNodeGetValue(procnode, "input");
							process.contents = ConfigNodeGetValue(procnode, "contents");
							process.status = ConfigNodeGetValue(procnode, "status");
						}
					}
				}
			}
			catch (Exception e)
			{
				debug("Load failed: " + e.Message);
			}

			// blow away any current state
			lisp_context = new MicroLisp.MicroLisp();

			/*KSP.IO.File.WriteAllText<AGC.AGCPart>(stdlib_contents, "debug-stdlib.txt", null);
			KSP.IO.File.WriteAllText<AGC.AGCPart>(processes[0].contents, "debug-process1.txt", null);
			KSP.IO.File.WriteAllText<AGC.AGCPart>(serialized_environment, "debug-env.txt", null);*/

			// load the standard library first
			// note that we are loading these from saved contents instead of re-reading from disk
			string stdlib_status = "";
			stdlib = Parse("stdlib", stdlib_contents, ref stdlib_status);

			foreach (var process in processes)
			{
				process.program = Parse(process.name, process.contents, ref process.status);
			}

			// load the environment
			try
			{
				lisp_context.globalEnv.FromString(serialized_environment);
			}
			catch (Exception e)
			{
				debug("Environment load failed: " + e.Message);
			}

			SetupLispEnvironment(lisp_context.globalEnv);

			if (show_ui) ShowUI(); else HideUI();
		}

		// sanitizes input
		static private void ConfigNodeAddValue(ConfigNode node, string key, string value)
		{
			node.AddValue(key, value.Replace("\n", "\\n"));
		}

		static private string ConfigNodeGetValue(ConfigNode node, string key)
		{
			return node.GetValue(key).Replace("\\n", "\n");
		}

		public override void OnSave(ConfigNode node)
		{
			if (vessel == null) return;

			debug("OnSave");

			try
			{
				if (lisp_context == null || lisp_context.globalEnv == null)
				{
					serialized_environment = "";
					//KSP.IO.File.WriteAllText<AGC.AGCPart>((lisp_context == null) ? "lisp null" : "globalEnv null", "debug-env-save.txt", null);
				}
				else
				{
					serialized_environment = lisp_context.globalEnv.ToString();
					//KSP.IO.File.WriteAllText<AGC.AGCPart>(serialized_environment, "debug-env-save.txt", null);
				}
			}
			catch (Exception e)
			{
				debug("Environment save failed: " + e.Message);
			}

			//KSP.IO.File.WriteAllText<AGC.AGCPart>(processes[0].contents, "debug-save-process1.txt", null);

			// Write to the ConfigNode
			try
			{
				ConfigNode root = node.HasNode("AGC") ? node.GetNode("AGC") : new ConfigNode("AGC");
				root.ClearNodes();
				root.ClearValues();
				ConfigNodeAddValue(root, "stdlib_contents", stdlib_contents);
				ConfigNodeAddValue(root, "is_active", is_active ? "1" : "0");
				ConfigNodeAddValue(root, "show_ui", show_ui ? "1" : "0");
				ConfigNodeAddValue(root, "tick_count", tick_count.ToString());
				ConfigNodeAddValue(root, "runtime", runtime.ToString());
				ConfigNodeAddValue(root, "serialized_environment", serialized_environment);

				for (int i = 0; i < processes.Length; i++)
				{
					Process process = processes[i];
					ConfigNode procnode = new ConfigNode("Process");
					ConfigNodeAddValue(procnode, "index", i.ToString());
					ConfigNodeAddValue(procnode, "name", process.name);
					ConfigNodeAddValue(procnode, "input", process.input);
					ConfigNodeAddValue(procnode, "contents", process.contents);
					ConfigNodeAddValue(procnode, "status", process.status);
					root.AddNode(procnode);
				}
				node.AddNode(root);
			}
			catch (Exception e)
			{
				debug("Failed to save to ConfigNode: " + e.Message);
			}
		}

		public override void OnActive()
		{
			debug("OnActive");
		}

		public override void OnInactive()
		{
			debug("OnActive");
		}
	}
}
