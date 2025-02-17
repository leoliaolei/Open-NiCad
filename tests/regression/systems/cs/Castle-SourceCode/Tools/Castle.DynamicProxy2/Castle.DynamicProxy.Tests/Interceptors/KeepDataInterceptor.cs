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

namespace Castle.DynamicProxy.Tests.Interceptors
{
	using System;
	using System.Reflection;
	using Castle.Core.Interceptor;

	public class KeepDataInterceptor : IInterceptor
	{
		private IInvocation invocation;

		public IInvocation Invocation
		{
			get { return invocation; }
		}

		public void Intercept(IInvocation invocation)
		{
			this.invocation = invocation;
			MethodInfo concreteMethod = invocation.GetConcreteMethod();

			if (!invocation.MethodInvocationTarget.IsAbstract)
			{
				invocation.Proceed();
			}
			else if (concreteMethod.ReturnType.IsValueType && !concreteMethod.ReturnType.Equals(typeof(void)))
				// ensure valid return value
			{
				invocation.ReturnValue = Activator.CreateInstance(concreteMethod.ReturnType);
			}
		}
	}
}