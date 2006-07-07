﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;

using Debugger.Wrappers.CorDebug;

namespace Debugger 
{
	/// <summary>
	/// Delegate that is used to get eval. This delegate may be called at any time and may return null.
	/// </summary>
	public delegate Eval EvalCreator();
	
	public class PropertyVariable: ClassVariable
	{
		EvalCreator evalCreator;
		Eval cachedEval;
		
		internal PropertyVariable(NDebugger debugger, string name, bool isStatic, bool isPublic, EvalCreator evalCreator):base(debugger, name, isStatic, isPublic, null)
		{
			this.evalCreator = evalCreator;
			this.pValue = new PersistentValue(debugger, delegate { return GetValueOfResult(); });
		}
		
		Value GetValueOfResult()
		{
			if (Eval != null) {
				return Eval.Result;
			} else {
				return new UnavailableValue(debugger, "Property unavailable");
			}
		}
		
		bool IsEvaluated {
			get {
				if (Eval != null) {
					return Eval.Evaluated;
				} else {
					return true;
				}
			}
		}
		
		Eval Eval {
			get {
				if (cachedEval == null || cachedEval.HasExpired) {
					cachedEval = evalCreator();
					if (cachedEval != null) {
						cachedEval.EvalStarted += delegate { OnValueChanged(this, null); };
						cachedEval.EvalComplete += delegate { OnValueChanged(this, null); };
					}
				}
				return cachedEval;
			}
		}
	}
}
