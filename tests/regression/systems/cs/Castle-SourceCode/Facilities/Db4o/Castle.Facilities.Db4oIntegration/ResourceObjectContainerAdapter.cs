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

namespace Castle.Facilities.Db4oIntegration
{
	using Db4objects.Db4o;

	using Castle.Services.Transaction;

	public class ResourceObjectContainerAdapter : IResource
	{
		private readonly IObjectContainer _objectContainer;

		public ResourceObjectContainerAdapter(IObjectContainer objectContainer)
		{
			_objectContainer = objectContainer;

		}

		public void Start()
		{
		}

		public void Commit()
		{
			_objectContainer.Commit();

			System.Diagnostics.Debug.WriteLine("[castle][db4o facility] Resource Adapter Commit");
		}

		public void Rollback()
		{
			_objectContainer.Rollback();

			System.Diagnostics.Debug.WriteLine("[castle][db4o facility] Resource Adapter Rollback");
		}
	}
}
