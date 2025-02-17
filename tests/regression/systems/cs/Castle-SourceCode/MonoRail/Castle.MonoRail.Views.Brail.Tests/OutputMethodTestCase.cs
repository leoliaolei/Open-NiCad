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

namespace Castle.MonoRail.Views.Brail.Tests
{
	using Castle.MonoRail.Framework.Tests;
	using NUnit.Framework;

	[TestFixture]
	public class OutputMethodTestCase : AbstractTestCase
	{
		[Test]
		public void HtmlAttribute()
		{
			string expected = @"
Some text that will be &lt;html&gt; encoded";
			DoGet("OutputMethods/HtmlOutputAttribute.rails");
			AssertReplyEqualTo(expected);
		}

		[Test]
		public void RawAttribute()
		{
			string expected = @"
1<2 && 3>4";
			DoGet("OutputMethods/RawOutputAttribute.rails");
			AssertReplyEqualTo(expected);
		}

		[Test]
		public void MarkDownAttribute()
		{
			string expected = "\r\n<p><a href=\"http://www.ayende.com/\">Ayende Rahien</a>, <strong>Rahien</strong>.</p>\n";

			DoGet("OutputMethods/MarkDownOutputAttribute.rails");
			AssertReplyEqualTo(expected);
		}
	}
}
