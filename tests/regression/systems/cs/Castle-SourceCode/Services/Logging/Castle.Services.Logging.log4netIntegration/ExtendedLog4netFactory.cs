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

namespace Castle.Services.Logging.Log4netIntegration
{
	using System;
	using System.IO;
	using Castle.Core.Logging;
	using Castle.Core.Logging.Factories;
	using log4net;
	using log4net.Config;

	public class ExtendedLog4netFactory : AbstractExtendedLoggerFactory
	{
		public ExtendedLog4netFactory() : this("log4net.config")
		{
		}

		public ExtendedLog4netFactory(String configFile)
		{
			FileInfo file = GetConfigFile(configFile);
			XmlConfigurator.ConfigureAndWatch(file);
		}

		/// <summary>
		/// Creates a new extended logger.
		/// </summary>
		public override IExtendedLogger Create(string name)
		{
			ILog log = LogManager.GetLogger(name);
			return new ExtendedLog4netLogger(log, this);
		}

		/// <summary>
		/// Creates a new extended logger.
		/// </summary>
		public override IExtendedLogger Create(string name, LoggerLevel level)
		{
			throw new NotSupportedException("Logger levels cannot be set at runtime. Please review your configuration file.");
		}
	}
}
