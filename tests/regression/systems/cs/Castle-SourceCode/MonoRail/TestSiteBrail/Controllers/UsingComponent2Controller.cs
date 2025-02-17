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

namespace Castle.MonoRail.Views.Brail.TestSite.Controllers
{
	using System;
	using System.Collections;

	using Castle.MonoRail.Framework;

    public class UsingComponent2Controller : SmartDispatcherController
	{
		public void ComponentWithInvalidSections()
		{
		}

		public void GridComponent1()
		{
			FillPropertyBag();
		}

		public void GridComponent2()
		{
            PropertyBag.Add("contacts", new ArrayList());
		}
	
		private void FillPropertyBag()
		{
			ArrayList items = new ArrayList();

			items.Add(new Contact("hammett", "111"));
			items.Add(new Contact("Peter Griffin", "222"));

			PropertyBag.Add("contacts", items);
		}
	}
	
	public class Contact
	{
		string email;
		string phone;

		public string Email
		{
			get { return email; }
			set { email = value; }
		}

		public string Phone
		{
			get { return phone; }
			set { phone = value; }
		}

        public Contact(string email, string phone)
		{
			this.email = email;
			this.phone = phone;
		}
	}
}
