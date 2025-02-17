// Copyright 2004-2007 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.MonoRail.Framework.Internal
{
	using System;
	using System.Collections;

	/// <summary>
	/// Utility class for wizard related queries and operations
	/// </summary>
	public static class WizardUtils
	{
		/// <summary>
		/// Constructs the wizard namespace.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns></returns>
		public static String ConstructWizardNamespace(Controller controller)
		{
			if (controller is WizardStepPage)
			{
				return ConstructWizardNamespace( (controller as WizardStepPage).WizardController );
			}

			return String.Format("wizard.{0}", controller.Name);
		}

		/// <summary>
		/// Determines whether the current wizard has a previous step.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns>
		/// 	<c>true</c> if has previous step; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasPreviousStep(Controller controller)
		{
			IRailsEngineContext context = controller.Context;

			String wizardName = WizardUtils.ConstructWizardNamespace(controller);

			int currentIndex = (int) context.Session[wizardName + "currentstepindex"];

			return currentIndex > 0;
		}

		/// <summary>
		/// Determines whether the current wizard has a next step.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns>
		/// 	<c>true</c> if has next step; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasNextStep(Controller controller)
		{
			IRailsEngineContext context = controller.Context;

			String wizardName = WizardUtils.ConstructWizardNamespace(controller);

			IList stepList = (IList) context.Items["wizard.step.list"];

			int currentIndex = (int) context.Session[wizardName + "currentstepindex"];
						
			return (currentIndex + 1) < stepList.Count;
		}

		/// <summary>
		/// Gets the name of the previous step.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns></returns>
		public static String GetPreviousStepName(Controller controller)
		{
			IRailsEngineContext context = controller.Context;

			String wizardName = WizardUtils.ConstructWizardNamespace(controller);

			int curIndex = (int) context.Session[wizardName + "currentstepindex"];

			IList stepList = (IList) context.Items["wizard.step.list"];

			if ((curIndex - 1) >= 0)
			{
				return (String) stepList[curIndex - 1];
			}

			return null;
		}

		/// <summary>
		/// Gets the name of the next step.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <returns></returns>
		public static String GetNextStepName(Controller controller)
		{
			IRailsEngineContext context = controller.Context;

			String wizardName = WizardUtils.ConstructWizardNamespace(controller);

			int curIndex = (int) context.Session[wizardName + "currentstepindex"];

			IList stepList = (IList) context.Items["wizard.step.list"];

			if ((curIndex + 1) < stepList.Count)
			{
				return (String) stepList[curIndex + 1];
			}

			return null;
		}

		/// <summary>
		/// Registers the current step info/state.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <param name="actionName">Name of the action.</param>
		public static void RegisterCurrentStepInfo(Controller controller, String actionName)
		{
			IRailsEngineContext context = controller.Context;

			IList stepList = (IList) context.Items["wizard.step.list"];

			for(int i=0; i < stepList.Count; i++)
			{
				String stepName = (String) stepList[i];

				if (actionName == stepName)
				{
					RegisterCurrentStepInfo(controller, i, stepName);

					break;
				}
			}
		}

		/// <summary>
		/// Registers the current step info/state.
		/// </summary>
		/// <param name="controller">The controller.</param>
		/// <param name="stepIndex">Index of the step.</param>
		/// <param name="stepName">Name of the step.</param>
		public static void RegisterCurrentStepInfo(Controller controller, int stepIndex, String stepName)
		{
			IRailsEngineContext context = controller.Context;
			String wizardName = WizardUtils.ConstructWizardNamespace(controller);

			context.Session[wizardName + "currentstepindex"] = stepIndex;
			context.Session[wizardName + "currentstep"] = stepName;
		}
	}
}
