#region CVS Version Header
/*
 * $Id: IBlackWhiteList.cs,v 1.1 2005/10/21 13:07:42 t_rendelmann Exp $
 * Last modified by $Author: t_rendelmann $
 * Last modified at $Date: 2005/10/21 13:07:42 $
 * $Revision: 1.1 $
 */
#endregion

using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace ChannelServices.AdsBlocker.AddIn
{
	#region ListUpdateState
	/// <summary>
	/// Enumeration used for the return code of UpdateBlacklist() 
	/// interface procedure.
	/// </summary>
	public enum ListUpdateState {
		None, 
		Updated, 
		Failed 
	}
	#endregion

	#region IBlacklist
	/// <summary>
	/// IBlacklist. Blacklist processor's interface.
	/// </summary>
	public interface IBlacklist
	{
		/// <summary>
		/// Init a blacklist match processor with the blacklist
		/// </summary>
		/// <param name="blacklist">string</param>
		void Initialize(string blacklist);

		/// <summary>
		/// Called to update the blacklist, if it is provided by
		/// a blacklist server.
		/// </summary>
		/// <returns></returns>
		ListUpdateState UpdateBlacklist();

		/// <summary>
		/// Called for each url to inspect. Should return a Match,
		/// if the url is contained in or matches the blacklist.
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Match</returns>
		Match IsBlacklisted(Uri uri);
	}

	#endregion

	#region IWhitelist
	/// <summary>
	/// IWhitelist. Whitelist processor's interface.
	/// </summary>
	public interface IWhitelist {
		/// <summary>
		/// Init a whitelist match processor with the whitelist
		/// </summary>
		/// <param name="whitelist">string</param>
		void Initialize(string whitelist);

		/// <summary>
		/// Called to update the whitelist, if it is provided by
		/// a whitelist server.
		/// </summary>
		/// <returns></returns>
		ListUpdateState UpdateWhitelist();

		/// <summary>
		/// Called for each url to inspect. Should return a Match,
		/// if the url is contained in or matches the whitelist.
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Match</returns>
		Match IsWhitelisted(Uri uri);
	}

	#endregion

	#region Factory
	public class BlackWhiteListFactory {
		private static Hashtable blacklists = new Hashtable();
		private static Hashtable whitelists = new Hashtable();

		public static void AddBlacklist(IBlacklist blackList, string content) {
			if (blacklists.ContainsKey(blackList.GetType().Name) == false) {
				try {
					blackList.Initialize(content);
					blackList.UpdateBlacklist();
					blacklists.Add(blackList.GetType().Name, blackList);
				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
			else {
				// update the blacklist
				IBlacklist refr = blacklists[blackList.GetType().Name] as IBlacklist;
				try {
					refr.Initialize(content);
					ListUpdateState updateState = refr.UpdateBlacklist();
					
					if (updateState == ListUpdateState.Failed) {
						throw new InvalidOperationException(blackList.ToString() + " could not be updated.");
					}

				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
		}

		public static void AddWhitelist(IWhitelist whitelist, string content) {
			if (blacklists.ContainsKey(whitelist.GetType().Name) == false) {
				try {
					whitelist.Initialize(content);
					whitelist.UpdateWhitelist();
					whitelists.Add(whitelist.GetType().Name, whitelist);
				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
			else {
				// update the whitelist
				IWhitelist refr = whitelists[whitelist.GetType().Name] as IWhitelist;
				try {
					refr.Initialize(content);
					ListUpdateState updateState = refr.UpdateWhitelist();
					
					if (updateState == ListUpdateState.Failed) {
						throw new InvalidOperationException(whitelist.ToString() + " could not be updated.");
					}

				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
		}

		public static void RemoveBlacklist(Type type) {
			if (blacklists.ContainsKey(type.Name)== true) {
				try {
					blacklists.Remove(type.Name);
				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
		}

		public static void RemoveWhitelist(Type type) {
			if (whitelists.ContainsKey(type.Name)== true) {
				try {
					whitelists.Remove(type.Name);
				}
				catch (Exception) {
					//Console.Write(ex.ToString());
					throw;
				}
			}
		}

		public static bool HasBlacklists {
			get { return blacklists.Count > 0; }
		}

		public static IBlacklist[] Blacklists {
			get {
				ArrayList list = new ArrayList();
				foreach (IBlacklist blacklist in blacklists.Values) {
					list.Add(blacklist);
				}

				return list.ToArray(typeof(IBlacklist)) as IBlacklist[];
			}
		}

		public static bool HasWhitelists {
			get { return whitelists.Count > 0; }
		}

		public static IWhitelist[] Whitelists {
			get {
				ArrayList list = new ArrayList();
				foreach (IWhitelist whitelist in whitelists.Values) {
					list.Add(whitelist);
				}

				return list.ToArray(typeof(IWhitelist)) as IWhitelist[];
			}
		}
	}
	#endregion
}
