#region CVS Version Header
/*
 * $Id: HtmlHelper.cs,v 1.19 2007/09/16 16:10:45 carnage4life Exp $
 * Last modified by $Author: carnage4life $
 * Last modified at $Date: 2007/09/16 16:10:45 $
 * $Revision: 1.19 $
 */
#endregion

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using NewsComponents.Collections;

namespace NewsComponents.Utils
{


	/// <summary>
	/// helper class used for expanding relative URLs. 
	/// </summary>
	internal class RelativeUrlExpander{

		internal string baseUrl; 
  
		/// <summary>
		/// Converts the URL in the regex matched to an absolute URL with the base as this.baseUrl 
		/// then returns the entire match
		/// </summary>
		/// <param name="m"></param>
		/// <returns>The matched string with the contained uRL replaced with its absolute URL</returns>
		internal string ConvertToAbsoluteUrl(Match m){

			/* skip non-HTTP URLs and URLs that are already absolute */ 
			string href = m.Groups[1].ToString();	
			//handle case where regex starts from quote character
			string test = ( href.StartsWith("\"") || href.StartsWith("'") ? href.Substring(1) : href) ; 
			if (test.StartsWith("http") || test.StartsWith("mailto:") || test.StartsWith("javascript:")){
				return m.Groups[0].ToString();
			}      
    
			return m.Groups[0].ToString().Replace(href, HtmlHelper.ConvertToAbsoluteUrl(href, baseUrl, true));
		}

	}


	/// <summary>
	/// Helper class to work on HTML content.
	/// </summary>
	public sealed class HtmlHelper {
		

		private static Regex RegExFindHrefOrSrc = new Regex(@"(?:<[iI][mM][gG]\s+([^>]*\s*)?src\s*=\s*(?:""(?<1>[/\a-z0-9_][^""]*)""|'(?<1>[/\a-z0-9_][^']*)'|(?<1>[/\a-z0-9_]\S*))(\s[^>]*)?>)|(?:<[aA]\s+([^>]*\s*)?href\s*=\s*(?:""(?<1>[/\a-z0-9_][^""]*)""|'(?<1>[/\a-z0-9_][^']*)'|(?<1>[/\a-z0-9_]\S*))(\s[^>]*)?>)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex RegExFindHref = new Regex(@"<a\s+([^>]*\s*)?href\s*=\s*(?:""(?<1>[/\a-z0-9_][^""]*)""|'(?<1>[/\a-z0-9_][^']*)'|(?<1>[/\a-z0-9_]\S*))(\s[^>]*)?>(?<2>.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex RegExBadTags = new Regex(@"<(?:script|object|meta|embed|frameset|i?frame|link)[\s>]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex RegExAnyTags = new Regex("</?[^<>]+>", RegexOptions.Compiled); 
		// according to http://www.w3.org/TR/REC-html40/charset.html#entities
		// Note: In SGML, it is possible to eliminate the final ";" after a character reference in some cases 
		// (e.g., at a line break or immediately before a tag). In other circumstances it may not be eliminated 
		// (e.g., in the middle of a word) - so we make use of the zero or one quantifier for ";":  
		private static Regex RegExAnyEntity = new Regex("(?:&#[0-9]+;?|&#x[a-f0-9]+;?|&[a-z0-9]+;?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static Regex RegExFindTitle = new Regex(@"<head\s*>.*<title\s*>(?<title>[^<]+)</title>.*</head>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		
		private static Hashtable _htmlEntities;
		// logging/tracing:
		private static readonly log4net.ILog _log = RssBandit.Common.Logging.Log.GetLogger(typeof(HtmlHelper));

		/// <summary>
		/// Converts a relative url to an absolute one. baseUrl is used as the base to fix the other.
		/// </summary>
		/// <param name="url">Url to fix</param>
		/// <param name="baseUrl">base Url to be used</param>
		/// <returns></returns>
		public static string ConvertToAbsoluteUrl(string url, string baseUrl) {
			if (url == null || url.Length == 0)
				return null;
			return ConvertToAbsoluteUrl(url, baseUrl, false);
		}

		/// <summary>
		/// Converts a relative url to an absolute one. baseUrl is used as the base to fix the other.
		/// </summary>
		/// <param name="url">Url to fix</param>
		/// <param name="baseUrl">base Url to be used</param>
		/// <param name="onlyValid">Provide true, if the url should only be handled if no UriFormatException happens.</param>
		/// <returns>converted Url</returns>
		public static string ConvertToAbsoluteUrl(string url, string baseUrl, bool onlyValid) {

			if (url == null || url.Length == 0)
				return null;

			Uri baseUri = null;
			try {
				if (baseUrl != null && baseUrl.Length > 0)
					baseUri = new Uri(baseUrl);
			} catch (UriFormatException) {}

			return ConvertToAbsoluteUrl(url, baseUri, onlyValid);

		}

		/// <summary>
		/// Converts a relative url to an absolute one. baseUrl is used as the base to fix the other.
		/// </summary>
		/// <param name="url">Url to fix</param>
		/// <param name="baseUri">base Uri to be used</param>
		/// <param name="onlyValid">Provide true, if the url should only be handled if no UriFormatException happens.</param>
		/// <returns>converted Url, or null</returns>
		public static string ConvertToAbsoluteUrl(string url, Uri baseUri, bool onlyValid) {
			
			Uri uri = ConvertToAbsoluteUri(url, baseUri, onlyValid);
			if (uri != null)
				return uri.AbsoluteUri;

			if (!onlyValid)	// return original:
				return url;
			return null;
		}
		
		/// <summary>
		/// Converts a relative url to an absolute one. baseUrl is used as the base to fix the other.
		/// </summary>
		/// <param name="url">Url to fix</param>
		/// <param name="baseUri">base Uri to be used</param>
		/// <param name="onlyValid">Provide true, if the url should only be handled if no UriFormatException happens.</param>
		/// <returns>converted Url, or null</returns>
		public static Uri ConvertToAbsoluteUri(string url, Uri baseUri, bool onlyValid) {
			
			if (url == null)
				return null;

			try {
				// we must check baseUri, because the Uri constructor
				// will raise a NullRefEx in case it is used internally:
				if (baseUri != null) {
					return new Uri(baseUri,url); 
				} else {
					return new Uri(url); 
				}
			} catch (UriFormatException) {
				if (onlyValid) return null;
			}
			// return original:
			return null;
		}
		
		/// <summary>
		/// Converts to absolute URL path. 
		/// E.g. a url "http://www.myserver.com/karli/feed.aspx?q=1"
		/// will return "http://www.myserver.com/karli/"
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>string</returns>
		public static string ConvertToAbsoluteUrlPath(string url) {
			
			if (url == null)
				return null;

			try {
				Uri uri = ConvertToAbsoluteUriPath(new Uri(url)); 
				return uri.AbsoluteUri;
			} catch (UriFormatException) {
				// one very last try:
				if (url.IndexOf("/") >= 0)
					return url.Substring(0, url.LastIndexOf("/")+1);
				return url;
			}
			
		}
		
		/// <summary>
		/// Converts to absolute URI path.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns></returns>
		public static Uri ConvertToAbsoluteUriPath(Uri uri) {
			
			if (uri == null)
				return null;

			UriBuilder b = new UriBuilder(uri);
			b.Path = b.Path.Substring(0, b.Path.LastIndexOf("/") + 1);
			b.Query = null;
			b.Fragment = null;
			return b.Uri;
		}
		
		/// <summary>
		/// Returns a RelationHRefDictionary with links found in HTML &lt;a href=""> attributes.
		/// </summary>
		/// <param name="html">String to work on</param>
		/// <param name="baseUrl">An absolute Url to be used to fix relative links</param>
		/// <returns>RelationHRefDictionary with string Urls as key(s), and link text as items</returns>
		public static RelationHRefDictionary RetrieveLinks(string html, string baseUrl) {
			if (html == null || html.Length == 0)
				return RelationHRefDictionary.Empty;
			Uri baseUri = null;
			try { 
				if(baseUrl != null){
					baseUri = new Uri(baseUrl);
				}
			} catch (UriFormatException) {}
			return RetrieveLinks(html, baseUri);
		}

		/// <summary>
		/// Returns a RelationHRefDictionary with links found in HTML &lt;a href=""> attributes.
		/// </summary>
		/// <param name="html">String to work on</param>
		/// <param name="baseUri">An absolute Uri to be used to fix relative links</param>
		/// <returns>RelationHRefDictionary with string Urls as key(s), and link text as items</returns>
		public static RelationHRefDictionary RetrieveLinks(string html, Uri baseUri) {

			if (html == null || html.Length == 0)
				return RelationHRefDictionary.Empty;

			RelationHRefDictionary tbl = new RelationHRefDictionary();

			for (Match m = RegExFindHref.Match(html); m.Success; m = m.NextMatch()) {
				
				string href = m.Groups[1].ToString();	// filter non-real relation urls:
				if (href.StartsWith("mailto:") || href.StartsWith("javascript:")){
					continue;
				}
				
				// We now do this in ExpandRelativeUrls 				
				// href = ConvertToAbsoluteUrl(href, baseUri, true);				  

				if (href == null || href.Length == 0)
					continue;
				
				href = RelationCosmos.RelationCosmos.UrlTable.Add(href);

				string linkText = m.Groups[2].ToString();	
				
				if (!tbl.ContainsKey(href))
					//tbl.Add(href, linkText);
					tbl.Add(href, new RelationHRefEntry(href, linkText, m.Index));
			}

			if (tbl.Count == 0)
				tbl = RelationHRefDictionary.Empty;

			return tbl; 
		}


		/// <summary>
		/// Expands relative links in the HTML input. 
		/// </summary>
		/// <param name="html">String to work on</param>
		/// <param name="baseUri">An absolute Uri to be used to fix relative links</param>
		/// <returns>The HTML content with all relative links in anchor and img tags expanded</returns></returns>
		public static string ExpandRelativeUrls(string html, string baseUrl) {
    
			if (html == null || html.Length == 0)
				return html;    

			RelativeUrlExpander expander = new RelativeUrlExpander(); 
			expander.baseUrl  = baseUrl;
			return RegExFindHrefOrSrc.Replace(html, new MatchEvaluator(expander.ConvertToAbsoluteUrl));
		}

		/// <summary>
		/// A replacement of the HttpUtility.HtmlDecode() function.
		/// </summary>
		/// <param name="s">String to decode</param>
		/// <returns>Decoded string</returns>
		public static string HtmlDecode(string s) {
			if (s == null) {
				return null;
			}
			return HtmlHelper.HtmlDecode(s, _htmlEntities);
		}

		private static string HtmlDecode(string s, Hashtable map) {
			
			Match m = RegExAnyEntity.Match(s);

			if (!m.Success) 
				return s;

			int cpos = 0;
			StringBuilder b = new StringBuilder(s.Length);

			while (m.Success) {

				b.Append(s.Substring(cpos, m.Index - cpos));
				string entity = m.ToString();
				string decoded = (string) map[entity];
				if (decoded != null) {
					b.Append(decoded);
				} else if (entity[1] == '#') {	// numeric entity
					try {
						int etp;
						int lCorr = 0;
						if (entity[entity.Length-1] == ';')
							lCorr = 1;
						if ((entity[2] == 'x') || (entity[2] == 'X')) {
							string ets = entity.Substring(3, entity.Length - 3 - lCorr);
							etp = int.Parse(ets, System.Globalization.NumberStyles.AllowHexSpecifier);
						}
						else {
							string ets = entity.Substring(2, entity.Length - 2 - lCorr);
							etp = int.Parse(ets);
						}
						// in contrast to HttpUtility.HtmlDecode() we use the Encoding class and do not
						// simply cast ushort to char and append...
						Encoding encoding = (etp <= 0xff) ? Encoding.Default : Encoding.Unicode;
						decoded = encoding.GetString(BitConverter.GetBytes(etp));
						if (decoded.Length > 0) {
							decoded = decoded.Substring(0, 1);
						}
						b.Append(decoded);
					}
					catch (Exception ex) {
						// Format/Overflow from Int32.Parse()
						_log.Error("HtmlDecode() match Exception for HTML entity '" + entity + "'", ex);
						b.Append(entity);		// 
					}
				}
				else {
					//_log.Warn("HtmlDecode() unknown HTML entity: " + entity);
					b.Append(entity);		// just take over
				}
	
				// move current position and get next match
				cpos = m.Index + m.Length;
				m = m.NextMatch();
			}

			// append all other missing chars after a match and return
			b.Append(s.Substring(cpos, s.Length - cpos));
			return b.ToString();

		}

		/// <summary>
		/// Removes any tag of the form &lt;xxx> or &lt;/xxx>.
		/// </summary>
		/// <param name="html">string to work on</param>
		/// <returns>A Text-Only version of the provided input.</returns>
		public static string StripAnyTags(string html) {
			if (html == null)
				return String.Empty;
			return RegExAnyTags.Replace(html, String.Empty); 
		}

		/// <summary>
		/// Gets the title out of the HTML head section.
		/// </summary>
		/// <param name="html">HTML string</param>
		/// <param name="defaultIfNoMatch">string to return, if no match was found</param>
		/// <returns></returns>
		public static string FindTitle(string html, string defaultIfNoMatch) {
			if (html == null)
				return defaultIfNoMatch;

			Match m = RegExFindTitle.Match(html);
			if (m.Success) {
				string t = m.Groups["title"].Value;
				if (StringHelper.EmptyTrimOrNull(t))
					return defaultIfNoMatch;
				return HtmlDecode(t);
			}

			return defaultIfNoMatch;
		}

		/// <summary>
		/// Replace any bad tag of the form &lt;object> by &lt;bject> to make
		/// it does not work on rendering later
		/// </summary>
		/// <remarks>THIS METHOD SHOULD NOT BE USED!!! IT CAUSES SCRIPT BLOCKS
		/// TO SHOW UP IN TEXT OF ITEMS. Users can control bad tags by disabling
		/// script instead which is more of a complete solution to the problem.</remarks>
		/// <param name="html">string to work on</param>
		/// <returns>A cleaner version of the provided input.</returns>
		public static string StripBadTags(string html) {
			if (html == null)
				return String.Empty;
			/* if (RegExBadTags.IsMatch(html)) {
				html = RegExBadTags.Replace(html, new MatchEvaluator(HtmlHelper.RegexTagEvaluate));
			} */ 
			return html;
		}

		/// <summary>
		/// URL-encode a provided string (Does NOT use the
		/// CLR function to do so).
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Null in case value is null</returns>
		public static string UrlEncode( string value ) {
			if (value == null)
				return null;
			
			StringBuilder result = new StringBuilder();

			foreach( char c in value ) {
				if( c < ' ' || c > 128 || c == '(' || c == ')' 
					|| c == '/' || c == ' ' || c == '+' || c == ',' || 
					c == ':' || c == '"' || c == '&' ) {
					result.Append( '%' );
					result.AppendFormat( System.Globalization.CultureInfo.InvariantCulture, "{0:X2}", (int)c );
				}
				else
					result.Append( c );
			}

			return result.ToString();
		}
		
		/// <summary>
		/// URL-decode the provided value. It use the CLR function to do 
		/// so, but protects against the "+" replacement issue of the
		/// framework function.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Null in case value is null</returns>
		public static string UrlDecode(string value) {
			if (value == null)
				return null;
			// this just safe on more replacement op. after calling CLR function:
			string result = value.Replace("+", "%2b");
			return System.Web.HttpUtility.UrlDecode(result);
		}
		
		private static string RegexTagEvaluate(Match m) {
			return string.Concat("<", m.ToString().Substring(2));
		}

		#region ctor's
		private HtmlHelper() {}

		static HtmlHelper() {
			_htmlEntities = new Hashtable();
			_htmlEntities.Add("&Aacute;", Convert.ToString('\x00c1'));
			_htmlEntities.Add("&aacute;", Convert.ToString('\x00e1'));
			_htmlEntities.Add("&Acirc;", Convert.ToString('\x00c2'));
			_htmlEntities.Add("&acirc;", Convert.ToString('\x00e2'));
			_htmlEntities.Add("&acute;", Convert.ToString('\x00b4'));
			_htmlEntities.Add("&AElig;", Convert.ToString('\x00c6'));
			_htmlEntities.Add("&aelig;", Convert.ToString('\x00e6'));
			_htmlEntities.Add("&Agrave;", Convert.ToString('\x00c0'));
			_htmlEntities.Add("&agrave;", Convert.ToString('\x00e0'));
			_htmlEntities.Add("&alefsym;", Convert.ToString('\u2135'));
			_htmlEntities.Add("&Alpha;", Convert.ToString('\u0391'));
			_htmlEntities.Add("&alpha;", Convert.ToString('\u03b1'));
			_htmlEntities.Add("&amp;", Convert.ToString('&'));
			_htmlEntities.Add("&and;", Convert.ToString('\u22a5'));
			_htmlEntities.Add("&ang;", Convert.ToString('\u2220'));
			_htmlEntities.Add("&apos;", "'");
			_htmlEntities.Add("&Aring;", Convert.ToString('\x00c5'));
			_htmlEntities.Add("&aring;", Convert.ToString('\x00e5'));
			_htmlEntities.Add("&asymp;", Convert.ToString('\u2248'));
			_htmlEntities.Add("&Atilde;", Convert.ToString('\x00c3'));
			_htmlEntities.Add("&atilde;", Convert.ToString('\x00e3'));
			_htmlEntities.Add("&Auml;", Convert.ToString('\x00c4'));
			_htmlEntities.Add("&auml;", Convert.ToString('\x00e4'));
			_htmlEntities.Add("&bdquo;", Convert.ToString('\u201e'));
			_htmlEntities.Add("&Beta;", Convert.ToString('\u0392'));
			_htmlEntities.Add("&beta;", Convert.ToString('\u03b2'));
			_htmlEntities.Add("&brvbar;", Convert.ToString('\x00a6'));
			_htmlEntities.Add("&bull;", Convert.ToString('\u2022'));
			_htmlEntities.Add("&cap;", Convert.ToString('\u2229'));
			_htmlEntities.Add("&Ccedil;", Convert.ToString('\x00c7'));
			_htmlEntities.Add("&ccedil;", Convert.ToString('\x00e7'));
			_htmlEntities.Add("&cedil;", Convert.ToString('\x00b8'));
			_htmlEntities.Add("&cent;", Convert.ToString('\x00a2'));
			_htmlEntities.Add("&Chi;", Convert.ToString('\u03a7'));
			_htmlEntities.Add("&chi;", Convert.ToString('\u03c7'));
			_htmlEntities.Add("&circ;", Convert.ToString('\u02c6'));
			_htmlEntities.Add("&clubs;", Convert.ToString('\u2663'));
			_htmlEntities.Add("&commat;", Convert.ToString('@'));
			_htmlEntities.Add("&cong;", Convert.ToString('\u2245'));
			_htmlEntities.Add("&copy;", Convert.ToString('\x00a9'));
			_htmlEntities.Add("&crarr;", Convert.ToString('\u21b5'));
			_htmlEntities.Add("&cup;", Convert.ToString('\u222a'));
			_htmlEntities.Add("&curren;", Convert.ToString('\x00a4'));
			_htmlEntities.Add("&dagger;", Convert.ToString('\u2020'));
			_htmlEntities.Add("&Dagger;", Convert.ToString('\u2021'));
			_htmlEntities.Add("&darr;", Convert.ToString('\u2193'));
			_htmlEntities.Add("&dArr;", Convert.ToString('\u21d3'));
			_htmlEntities.Add("&deg;", Convert.ToString('\x00b0'));
			_htmlEntities.Add("&Delta;", Convert.ToString('\u0394'));
			_htmlEntities.Add("&delta;", Convert.ToString('\u03b4'));
			_htmlEntities.Add("&diams;", Convert.ToString('\u2666'));
			_htmlEntities.Add("&divide;", Convert.ToString('\x00f7'));
			_htmlEntities.Add("&dollar;", Convert.ToString('$'));
			_htmlEntities.Add("&Eacute;", Convert.ToString('\x00c9'));
			_htmlEntities.Add("&eacute;", Convert.ToString('\x00e9'));
			_htmlEntities.Add("&Ecirc;", Convert.ToString('\x00ca'));
			_htmlEntities.Add("&ecirc;", Convert.ToString('\x00ea'));
			_htmlEntities.Add("&Egrave;", Convert.ToString('\x00c8'));
			_htmlEntities.Add("&egrave;", Convert.ToString('\x00e8'));
			_htmlEntities.Add("&empty;", Convert.ToString('\u2205'));
			_htmlEntities.Add("&emsp;", Convert.ToString('\u2003'));
			_htmlEntities.Add("&ensp;", Convert.ToString('\u2002'));
			_htmlEntities.Add("&Epsilon;", Convert.ToString('\u0395'));
			_htmlEntities.Add("&epsilon;", Convert.ToString('\u03b5'));
			_htmlEntities.Add("&equiv;", Convert.ToString('\u2261'));
			_htmlEntities.Add("&Eta;", Convert.ToString('\u0397'));
			_htmlEntities.Add("&eta;", Convert.ToString('\u03b7'));
			_htmlEntities.Add("&ETH;", Convert.ToString('\x00d0'));
			_htmlEntities.Add("&eth;", Convert.ToString('\x00f0'));
			_htmlEntities.Add("&Euml;", Convert.ToString('\x00cb'));
			_htmlEntities.Add("&euml;", Convert.ToString('\x00eb'));
			_htmlEntities.Add("&euro;", Convert.ToString('\u20ac'));
			_htmlEntities.Add("&exist;", Convert.ToString('\u2203'));
			_htmlEntities.Add("&fnof;", Convert.ToString('\u0192'));
			_htmlEntities.Add("&forall;", Convert.ToString('\u2200'));
			_htmlEntities.Add("&frac12;", Convert.ToString('\x00bd'));
			_htmlEntities.Add("&frac14;", Convert.ToString('\x00bc'));
			_htmlEntities.Add("&frac34;", Convert.ToString('\x00be'));
			_htmlEntities.Add("&frasl;", Convert.ToString('\u2044'));
			_htmlEntities.Add("&Gamma;", Convert.ToString('\u0393'));
			_htmlEntities.Add("&gamma;", Convert.ToString('\u03b3'));
			_htmlEntities.Add("&ge;", Convert.ToString('\u2265'));
			_htmlEntities.Add("&grave;", Convert.ToString('`'));
			_htmlEntities.Add("&gt;", Convert.ToString('>'));
			_htmlEntities.Add("&harr;", Convert.ToString('\u2194'));
			_htmlEntities.Add("&hArr;", Convert.ToString('\u21d4'));
			_htmlEntities.Add("&hearts;", Convert.ToString('\u2665'));
			_htmlEntities.Add("&hellip;", Convert.ToString('\u2026'));
			_htmlEntities.Add("&Iacute;", Convert.ToString('\x00cd'));
			_htmlEntities.Add("&iacute;", Convert.ToString('\x00ed'));
			_htmlEntities.Add("&Icirc;", Convert.ToString('\x00ce'));
			_htmlEntities.Add("&icirc;", Convert.ToString('\x00ee'));
			_htmlEntities.Add("&iexcl;", Convert.ToString('\x00a1'));
			_htmlEntities.Add("&Igrave;", Convert.ToString('\x00cc'));
			_htmlEntities.Add("&igrave;", Convert.ToString('\x00ec'));
			_htmlEntities.Add("&image;", Convert.ToString('\u2111'));
			_htmlEntities.Add("&infin;", Convert.ToString('\u221e'));
			_htmlEntities.Add("&int;", Convert.ToString('\u222b'));
			_htmlEntities.Add("&Iota;", Convert.ToString('\u0399'));
			_htmlEntities.Add("&iota;", Convert.ToString('\u03b9'));
			_htmlEntities.Add("&iquest;", Convert.ToString('\x00bf'));
			_htmlEntities.Add("&isin;", Convert.ToString('\u2208'));
			_htmlEntities.Add("&Iuml;", Convert.ToString('\x00cf'));
			_htmlEntities.Add("&iuml;", Convert.ToString('\x00ef'));
			_htmlEntities.Add("&Kappa;", Convert.ToString('\u039a'));
			_htmlEntities.Add("&kappa;", Convert.ToString('\u03ba'));
			_htmlEntities.Add("&Lambda;", Convert.ToString('\u039b'));
			_htmlEntities.Add("&lambda;", Convert.ToString('\u03bb'));
			_htmlEntities.Add("&lang;", Convert.ToString('\u2329'));
			_htmlEntities.Add("&laquo;", Convert.ToString('\x00ab'));
			_htmlEntities.Add("&larr;", Convert.ToString('\u2190'));
			_htmlEntities.Add("&lArr;", Convert.ToString('\u21d0'));
			_htmlEntities.Add("&lceil;", Convert.ToString('\u2308'));
			_htmlEntities.Add("&ldquo;", Convert.ToString('\u201c'));
			_htmlEntities.Add("&le;", Convert.ToString('\u2264'));
			_htmlEntities.Add("&lfloor;", Convert.ToString('\u230a'));
			_htmlEntities.Add("&lowast;", Convert.ToString('\u2217'));
			_htmlEntities.Add("&loz;", Convert.ToString('\u25ca'));
			_htmlEntities.Add("&lrm;", Convert.ToString('\u200e'));
			_htmlEntities.Add("&lsaquo;", Convert.ToString('\u2039'));
			_htmlEntities.Add("&lsquo;", Convert.ToString('\u2018'));
			_htmlEntities.Add("&lt;", Convert.ToString('<'));
			_htmlEntities.Add("&macr;", Convert.ToString('\x00af'));
			_htmlEntities.Add("&mdash;", Convert.ToString('\u2014'));
			_htmlEntities.Add("&micro;", Convert.ToString('\x00b5'));
			_htmlEntities.Add("&middot;", Convert.ToString('\x00b7'));
			_htmlEntities.Add("&minus;", Convert.ToString('\u2212'));
			_htmlEntities.Add("&Mu;", Convert.ToString('\u039c'));
			_htmlEntities.Add("&mu;", Convert.ToString('\u03bc'));
			_htmlEntities.Add("&nabla;", Convert.ToString('\u2207'));
			_htmlEntities.Add("&nbsp;", Convert.ToString('\x00a0'));
			_htmlEntities.Add("&ndash;", Convert.ToString('\u2013'));
			_htmlEntities.Add("&ne;", Convert.ToString('\u2260'));
			_htmlEntities.Add("&ni;", Convert.ToString('\u220b'));
			_htmlEntities.Add("&not;", Convert.ToString('\x00ac'));
			_htmlEntities.Add("&notin;", Convert.ToString('\u2209'));
			_htmlEntities.Add("&nsub;", Convert.ToString('\u2284'));
			_htmlEntities.Add("&Ntilde;", Convert.ToString('\x00d1'));
			_htmlEntities.Add("&ntilde;", Convert.ToString('\x00f1'));
			_htmlEntities.Add("&Nu;", Convert.ToString('\u039d'));
			_htmlEntities.Add("&nu;", Convert.ToString('\u03bd'));
			_htmlEntities.Add("&num;", Convert.ToString('#'));
			_htmlEntities.Add("&Oacute;", Convert.ToString('\x00d3'));
			_htmlEntities.Add("&oacute;", Convert.ToString('\x00f3'));
			_htmlEntities.Add("&Ocirc;", Convert.ToString('\x00d4'));
			_htmlEntities.Add("&ocirc;", Convert.ToString('\x00f4'));
			_htmlEntities.Add("&OElig;", Convert.ToString('\u0152'));
			_htmlEntities.Add("&oelig;", Convert.ToString('\u0153'));
			_htmlEntities.Add("&Ograve;", Convert.ToString('\x00d2'));
			_htmlEntities.Add("&ograve;", Convert.ToString('\x00f2'));
			_htmlEntities.Add("&oline;", Convert.ToString('\u203e'));
			_htmlEntities.Add("&Omega;", Convert.ToString('\u03a9'));
			_htmlEntities.Add("&omega;", Convert.ToString('\u03c9'));
			_htmlEntities.Add("&Omicron;", Convert.ToString('\u039f'));
			_htmlEntities.Add("&omicron;", Convert.ToString('\u03bf'));
			_htmlEntities.Add("&oplus;", Convert.ToString('\u2295'));
			_htmlEntities.Add("&or;", Convert.ToString('\u22a6'));
			_htmlEntities.Add("&ordf;", Convert.ToString('\x00aa'));
			_htmlEntities.Add("&ordm;", Convert.ToString('\x00ba'));
			_htmlEntities.Add("&Oslash;", Convert.ToString('\x00d8'));
			_htmlEntities.Add("&oslash;", Convert.ToString('\x00f8'));
			_htmlEntities.Add("&Otilde;", Convert.ToString('\x00d5'));
			_htmlEntities.Add("&otilde;", Convert.ToString('\x00f5'));
			_htmlEntities.Add("&otimes;", Convert.ToString('\u2297'));
			_htmlEntities.Add("&Ouml;", Convert.ToString('\x00d6'));
			_htmlEntities.Add("&ouml;", Convert.ToString('\x00f6'));
			_htmlEntities.Add("&para;", Convert.ToString('\x00b6'));
			_htmlEntities.Add("&part;", Convert.ToString('\u2202'));
			_htmlEntities.Add("&percnt;", Convert.ToString('%'));
			_htmlEntities.Add("&permil;", Convert.ToString('\u2030'));
			_htmlEntities.Add("&perp;", Convert.ToString('\u22a5'));
			_htmlEntities.Add("&Phi;", Convert.ToString('\u03a6'));
			_htmlEntities.Add("&phi;", Convert.ToString('\u03c6'));
			_htmlEntities.Add("&Pi;", Convert.ToString('\u03a0'));
			_htmlEntities.Add("&pi;", Convert.ToString('\u03c0'));
			_htmlEntities.Add("&piv;", Convert.ToString('\u03d6'));
			_htmlEntities.Add("&plusmn;", Convert.ToString('\x00b1'));
			_htmlEntities.Add("&pound;", Convert.ToString('\x00a3'));
			_htmlEntities.Add("&prime;", Convert.ToString('\u2032'));
			_htmlEntities.Add("&Prime;", Convert.ToString('\u2033'));
			_htmlEntities.Add("&prod;", Convert.ToString('\u220f'));
			_htmlEntities.Add("&prop;", Convert.ToString('\u221d'));
			_htmlEntities.Add("&Psi;", Convert.ToString('\u03a8'));
			_htmlEntities.Add("&psi;", Convert.ToString('\u03c8'));
			_htmlEntities.Add("&quot;", Convert.ToString('"'));
			_htmlEntities.Add("&radic;", Convert.ToString('\u221a'));
			_htmlEntities.Add("&rang;", Convert.ToString('\u232a'));
			_htmlEntities.Add("&raquo;", Convert.ToString('\x00bb'));
			_htmlEntities.Add("&rarr;", Convert.ToString('\u2192'));
			_htmlEntities.Add("&rArr;", Convert.ToString('\u21d2'));
			_htmlEntities.Add("&rceil;", Convert.ToString('\u2309'));
			_htmlEntities.Add("&rdquo;", Convert.ToString('\u201d'));
			_htmlEntities.Add("&real;", Convert.ToString('\u211c'));
			_htmlEntities.Add("&reg;", Convert.ToString('\x00ae'));
			_htmlEntities.Add("&rfloor;", Convert.ToString('\u230b'));
			_htmlEntities.Add("&Rho;", Convert.ToString('\u03a1'));
			_htmlEntities.Add("&rho;", Convert.ToString('\u03c1'));
			_htmlEntities.Add("&rlm;", Convert.ToString('\u200f'));
			_htmlEntities.Add("&rsaquo;", Convert.ToString('\u203a'));
			_htmlEntities.Add("&rsquo;", Convert.ToString('\u2019'));
			_htmlEntities.Add("&sbquo;", Convert.ToString('\u201a'));
			_htmlEntities.Add("&Scaron;", Convert.ToString('\u0160'));
			_htmlEntities.Add("&scaron;", Convert.ToString('\u0161'));
			_htmlEntities.Add("&sdot;", Convert.ToString('\u22c5'));
			_htmlEntities.Add("&sect;", Convert.ToString('\x00a7'));
			_htmlEntities.Add("&shy;", Convert.ToString('\x00ad'));
			_htmlEntities.Add("&Sigma;", Convert.ToString('\u03a3'));
			_htmlEntities.Add("&sigma;", Convert.ToString('\u03c3'));
			_htmlEntities.Add("&sigmaf;", Convert.ToString('\u03c2'));
			_htmlEntities.Add("&sim;", Convert.ToString('\u223c'));
			_htmlEntities.Add("&spades;", Convert.ToString('\u2660'));
			_htmlEntities.Add("&sub;", Convert.ToString('\u2282'));
			_htmlEntities.Add("&sube;", Convert.ToString('\u2286'));
			_htmlEntities.Add("&sum;", Convert.ToString('\u2211'));
			_htmlEntities.Add("&sup;", Convert.ToString('\u2283'));
			_htmlEntities.Add("&sup1;", Convert.ToString('\x00b9'));
			_htmlEntities.Add("&sup2;", Convert.ToString('\x00b2'));
			_htmlEntities.Add("&sup3;", Convert.ToString('\x00b3'));
			_htmlEntities.Add("&supe;", Convert.ToString('\u2287'));
			_htmlEntities.Add("&szlig;", Convert.ToString('\x00df'));
			_htmlEntities.Add("&Tau;", Convert.ToString('\u03a4'));
			_htmlEntities.Add("&tau;", Convert.ToString('\u03c4'));
			_htmlEntities.Add("&there4;", Convert.ToString('\u2234'));
			_htmlEntities.Add("&Theta;", Convert.ToString('\u0398'));
			_htmlEntities.Add("&theta;", Convert.ToString('\u03b8'));
			_htmlEntities.Add("&thetasym;", Convert.ToString('\u03d1'));
			_htmlEntities.Add("&thinsp;", Convert.ToString('\u2009'));
			_htmlEntities.Add("&THORN;", Convert.ToString('\x00de'));
			_htmlEntities.Add("&thorn;", Convert.ToString('\x00fe'));
			_htmlEntities.Add("&tilde;", Convert.ToString('\u02dc'));
			_htmlEntities.Add("&times;", Convert.ToString('\x00d7'));
			_htmlEntities.Add("&trade;", Convert.ToString('\u2122'));
			_htmlEntities.Add("&Uacute;", Convert.ToString('\x00da'));
			_htmlEntities.Add("&uacute;", Convert.ToString('\x00fa'));
			_htmlEntities.Add("&uarr;", Convert.ToString('\u2191'));
			_htmlEntities.Add("&uArr;", Convert.ToString('\u21d1'));
			_htmlEntities.Add("&Ucirc;", Convert.ToString('\x00db'));
			_htmlEntities.Add("&ucirc;", Convert.ToString('\x00fb'));
			_htmlEntities.Add("&Ugrave;", Convert.ToString('\x00d9'));
			_htmlEntities.Add("&ugrave;", Convert.ToString('\x00f9'));
			_htmlEntities.Add("&uml;", Convert.ToString('\x00a8'));
			_htmlEntities.Add("&upsih;", Convert.ToString('\u03d2'));
			_htmlEntities.Add("&Upsilon;", Convert.ToString('\u03a5'));
			_htmlEntities.Add("&upsilon;", Convert.ToString('\u03c5'));
			_htmlEntities.Add("&Uuml;", Convert.ToString('\x00dc'));
			_htmlEntities.Add("&uuml;", Convert.ToString('\x00fc'));
			_htmlEntities.Add("&weierp;", Convert.ToString('\u2118'));
			_htmlEntities.Add("&Xi;", Convert.ToString('\u039e'));
			_htmlEntities.Add("&xi;", Convert.ToString('\u03be'));
			_htmlEntities.Add("&Yacute;", Convert.ToString('\x00dd'));
			_htmlEntities.Add("&yacute;", Convert.ToString('\x00fd'));
			_htmlEntities.Add("&yen;", Convert.ToString('\x00a5'));
			_htmlEntities.Add("&Yuml;", Convert.ToString('\u0178'));
			_htmlEntities.Add("&yuml;", Convert.ToString('\x00ff'));
			_htmlEntities.Add("&Zeta;", Convert.ToString('\u0396'));
			_htmlEntities.Add("&zeta;", Convert.ToString('\u03b6'));
			_htmlEntities.Add("&zwj;", Convert.ToString('\u200d'));
			_htmlEntities.Add("&zwnj;", Convert.ToString('\u200c'));

		}
		#endregion

	}

}
#region CVS Version Log
/*
 * $Log: HtmlHelper.cs,v $
 * Revision 1.19  2007/09/16 16:10:45  carnage4life
 * Fixed issue where images in facebook feeds showed up as broken links
 *
 * Revision 1.18  2007/08/01 19:14:17  carnage4life
 * RetrieveLinks() now handles null baseUrl
 *
 * Revision 1.17  2007/07/08 07:14:45  carnage4life
 * Images don't show up on certain items when clicking on feed or category view if the feed uses relative links such as http://www.tbray.org/ongoing/ongoing.atom
 *
 * Revision 1.16  2007/05/03 15:58:06  t_rendelmann
 * fixed: toggle read state from within html detail pane (javascript initiated) not always toggle the read state within the listview and subscription tree (caused if item ID is Url-encoded)
 *
 * Revision 1.15  2006/10/19 19:48:28  t_rendelmann
 * commented the _log call
 *
 * Revision 1.14  2006/10/17 10:42:56  t_rendelmann
 * fixed: not all HTML entity encoding handled (like that of SGML without the trailing ";")
 * fixed: now trim the NewsItem.Title to get rid of tabs and spaces
 *
 * Revision 1.13  2006/10/15 17:22:47  t_rendelmann
 * fixed: relative urls in feed links (feed homepage urls)
 *
 */
#endregion
