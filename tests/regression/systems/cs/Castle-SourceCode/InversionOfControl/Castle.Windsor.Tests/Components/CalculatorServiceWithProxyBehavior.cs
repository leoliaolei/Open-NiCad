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

namespace Castle.Windsor.Tests.Components
{
	using System;
	using Castle.Core;
	using Castle.Core.Interceptor;

	/// <summary>
	/// Summary description for CalculatorServiceWithAttributes.
	/// </summary>
	[Interceptor(typeof(StandardInterceptor))]
	public class CalculatorServiceWithMultipleInterfaces : CalculatorService, IDisposable
	{
		public CalculatorServiceWithMultipleInterfaces()
		{
		}

		public void Dispose()
		{
		}
	}

	/// <summary>
	/// Summary description for CalculatorServiceWithAttributes.
	/// </summary>
	[Interceptor(typeof(StandardInterceptor))]
	[ComponentProxyBehavior(UseSingleInterfaceProxy = true)]
	public class CalculatorServiceWithSingleProxyBehavior : CalculatorService, IDisposable
	{
		public CalculatorServiceWithSingleProxyBehavior()
		{
		}

		public void Dispose()
		{
		}
	}

	/// <summary>
	/// Summary description for CalculatorServiceWithAttributes.
	/// </summary>
	[Interceptor(typeof(StandardInterceptor))]
	[ComponentProxyBehavior(UseSingleInterfaceProxy = false)]
	public class CalculatorServiceWithoutSingleProxyBehavior : CalculatorService, IDisposable
	{
		public CalculatorServiceWithoutSingleProxyBehavior()
		{
		}

		public void Dispose()
		{
		}
	}
}