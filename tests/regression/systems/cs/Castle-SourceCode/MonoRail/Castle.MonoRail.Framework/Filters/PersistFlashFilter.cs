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

namespace Castle.MonoRail.Framework.Filters
{
	using Castle.MonoRail.Framework.Attributes;

	/// <summary>
	/// Persist entire flash content after an action execution on the controller.
	/// <seealso cref="PersistFlashAttribute"/>
	/// </summary>
	public class PersistFlashFilter : IFilter
	{
		/// <summary>
		/// Implementors should perform they filter logic and
		/// return <c>true</c> if the action should be processed.
		/// </summary>
		/// <param name="exec">When this filter is being invoked</param>
		/// <param name="context">Current context</param>
		/// <param name="controller">The controller instance</param>
		/// <returns>
		/// 	<c>true</c> if the action
		/// should be invoked, otherwise <c>false</c>
		/// </returns>
		public bool Perform(ExecuteEnum exec, IRailsEngineContext context, Controller controller)
		{
			controller.Flash.Keep();

			return true;
		}
	}
}