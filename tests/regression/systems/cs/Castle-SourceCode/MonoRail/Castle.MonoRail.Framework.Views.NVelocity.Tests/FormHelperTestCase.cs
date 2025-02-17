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

namespace Castle.MonoRail.Framework.Views.NVelocity.Tests
{
	using Castle.MonoRail.Framework.Tests;
	using NUnit.Framework;

	[TestFixture]
	public class FormHelperTestCase : AbstractTestCase
	{
		[Test]
		public void ParamsAreUsedByFormHelper()
		{
			DoGet("formhelper/UseParamsToFillInputs.rails", "somearg=abc", "otherarg=123");
			AssertSuccess();
			AssertReplyEqualTo("<input type=\"hidden\" id=\"somearg\" name=\"somearg\" value=\"abc\" />\r\n<input type=\"text\" id=\"otherarg\" name=\"otherarg\" value=\"123\" />");
		}

		[Test]
		public void CheckBoxBinding()
		{
			DoGet("formhelper/save.rails");
			AssertSuccess();
			AssertReplyEqualTo("False False");

			DoGet("formhelper/save.rails", "model.SubscribeMe=true");
			AssertSuccess();
			AssertReplyEqualTo("True True");

			DoGet("formhelper/save.rails", "model.SubscribeMe=false");
			AssertSuccess();
			AssertReplyEqualTo("True False");

			DoGet("formhelper/save.rails", "model.SubscribeMe=0");
			AssertSuccess();
			AssertReplyEqualTo("True False");
		}
	}
}
