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

namespace Castle.Services.Transaction.Tests
{
	using System;

	public class ThrowsExceptionResourceImpl : ResourceImpl
	{
		private bool throwOnCommit = false;
		private bool throwOnRollback = false;

		public ThrowsExceptionResourceImpl(bool throwOnCommit, bool throwOnRollback)
		{
			this.throwOnCommit = throwOnCommit;
			this.throwOnRollback = throwOnRollback;
		}

		public new void Rollback()
		{
			if (throwOnRollback)
			{
				throw new Exception("Simulated rollback error");
			}

			base.Rollback();
		}

		public new void Commit()
		{
			if (throwOnCommit)
			{
				throw new Exception("Simulated commit error");
			}

			base.Commit();
		}
	}
}