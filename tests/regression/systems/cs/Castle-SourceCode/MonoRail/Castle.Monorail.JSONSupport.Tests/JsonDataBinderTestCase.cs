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

namespace Castle.Monorail.JSONSupport.Tests
{
	using NUnit.Framework;

	[TestFixture]
	public class JsonDataBinderTestCase
	{
		[Test]
		public void BindData()
		{
			Person p = (Person) JSONBinderAttribute.Bind("{\"Name\":\"Json\", \"Age\":23}", typeof(Person));

			Assert.AreEqual("Json", p.Name);
			Assert.AreEqual(23, p.Age);
		}
	}

	public class Person
	{
		private string name;
		private int age;

		public Person()
		{
		}

		public Person(string name, int age)
		{
			this.name = name;
			this.age = age;
		}

		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		public int Age
		{
			get { return age; }
			set { age = value; }
		}
	}
}
