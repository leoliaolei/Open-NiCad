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

namespace Castle.DynamicProxy.Tests
{
	using System;
	using System.Collections;
	using Castle.Core.Interceptor;
	using Castle.DynamicProxy.Tests.Classes;
	using Castle.DynamicProxy.Tests.Interceptors;
	using NUnit.Framework;

	[TestFixture]
	public class AccessLevelTestCase : BasePEVerifyTestCase
	{
		[Test]
		public void ProtectedConstructor()
		{
			ProxyGenerator generator = new ProxyGenerator();

			NonPublicConstructorClass proxy =
				generator.CreateClassProxy(
					typeof(NonPublicConstructorClass), new StandardInterceptor())
				as NonPublicConstructorClass;

			Assert.IsNotNull(proxy);

			proxy.DoSomething();
		}

		[Test]
		public void ProtectedMethods()
		{
			ProxyGenerator generator = new ProxyGenerator();

			LogInvocationInterceptor logger = new LogInvocationInterceptor();

			NonPublicMethodsClass proxy = (NonPublicMethodsClass)
			                              generator.CreateClassProxy(typeof(NonPublicMethodsClass), logger);

			proxy.DoSomething();

			Assert.AreEqual(2, logger.Invocations.Count);
			Assert.AreEqual("DoSomething", logger.Invocations[0]);
			Assert.AreEqual("DoOtherThing", logger.Invocations[1]);
		}

		[Test]
		public void InternalConstructorIsNotReplicated()
		{
			object proxy = generator.CreateClassProxy(typeof(Hashtable), new StandardInterceptor());
			Assert.IsNull(proxy.GetType().GetConstructor(new Type[] {typeof(IInterceptor[]), typeof(bool)}));
		}

		internal class InternalClass
		{
			internal InternalClass()
			{
			}
		}
		
#if !MONO

		[Test]
		public void InternalConstructorIsReplicatedWhenInternalsVisibleTo()
		{
			object proxy = generator.CreateClassProxy(typeof(InternalClass), new StandardInterceptor());
			Assert.IsNotNull(proxy.GetType().GetConstructor(new Type[] {typeof(IInterceptor[])}));
		}
		
#endif
	}
}