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

namespace Castle.MonoRail.Framework.Tests
{
	using System;
	using System.Globalization;

	using NUnit.Framework;

	using Castle.MonoRail.TestSupport;


	[TestFixture]
	public class LocalizationTestCase : AbstractTestCase
	{
		private CultureInfo en = CultureInfo.CreateSpecificCulture( "en" );
		private CultureInfo nl = CultureInfo.CreateSpecificCulture( "nl" );
		private CultureInfo fr = CultureInfo.CreateSpecificCulture( "fr" );

		[Test]
		public void GetResource()
		{
			DoGet("resource/GetResource.rails");
			
			AssertSuccess();

			AssertReplyEqualTo("english");
		}

		[Test]
		public void GetResourceByCulture()
		{
			DoGet("resource/GetResourceByCulture.rails");
			
			AssertSuccess();

			AssertReplyEqualTo("deutsch");
		}

		[Test]
		public void SetLocaleCulture()
		{
			DoGet("resource/GetResource.rails", "locale=nl");
			
			AssertSuccess();

			AssertReplyEqualTo("nederlands");
		}

		[Test]
		public void GetFallbackResource()
		{
			DoGet("resource/GetResource.rails", "locale=fr");
			
			AssertSuccess();

			AssertReplyEqualTo("english");
		}
	}
}
