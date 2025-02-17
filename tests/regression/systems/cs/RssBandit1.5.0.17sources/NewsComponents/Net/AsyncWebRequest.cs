#region CVS Version Header
/*
 * $Id: AsyncWebRequest.cs,v 1.55 2007/08/07 19:36:53 carnage4life Exp $
 * Last modified by $Author: carnage4life $
 * Last modified at $Date: 2007/08/07 19:36:53 $
 * $Revision: 1.55 $
 */
#endregion

using System;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;	// for cookie management
using System.Reflection;	// for unsafeHeaderParsingFix
using System.Security.Cryptography.X509Certificates;	// used for certificate issue handling
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

using NewsComponents.Utils;
using NewsComponents.News;

namespace NewsComponents.Net
{

	/// <summary>
	/// Extended HTTP Response Status Codes.
	/// </summary>
	public enum HttpExtendedStatusCode: int {
		/// <summary>
		/// Instance Manipulation used - Using RFC3229 with feeds.
		/// See also http://bobwyman.pubsub.com/main/2004/09/using_rfc3229_w.html
		/// </summary>
		IMUsed = 226
	}

	/// <summary>
	/// Summary description for AsyncWebRequest.
	/// </summary>
	public sealed class AsyncWebRequest {

		/// <summary>
		/// We use our own default MinValue for web requests to
		/// prevent first chance exceptions (InvalidRangeException on
		/// assigning to Request.IfModifiedSince). This value is expected
		/// in local Time, so we don't use DateTime.MinValue! It goes out
		/// of range if converted to universal time (e.g. if we have GMT +xy)
		/// </summary>
		private static DateTime MinValue = new DateTime(1901, 1, 1); 

		private static readonly log4net.ILog _log = RssBandit.Common.Logging.Log.GetLogger(typeof(AsyncWebRequest));

		/// <summary>
		/// Event triggered, if a not yet accepted CertificateIssue is raised by a web request.
		/// </summary>
		public static event CertificateIssueHandler OnCertificateIssue = null;

		/// <summary>
		/// Contains the url's as keys and the allowed (user interaction needed) 
		/// CertificateIssue's within an ICollection as values.
		/// </summary>
		/// <remarks>That content should be maintained completely from within
		/// the OnCertificateIssue event.</remarks>
		private static Hashtable trustedCertificateIssues = new Hashtable(5);

		/// <summary>
		/// Callback delegate used for OnAllRequestsComplete event.
		/// </summary>
		public delegate void RequestAllCompleteCallback();
		/// <summary>
		/// Event triggered, if all queued async. requests are done.
		/// </summary>
		public  event RequestAllCompleteCallback OnAllRequestsComplete = null;

		private const int DefaultTimeout = 2 * 60 * 1000; // 2 minute request timeout

		private Hashtable queuedRequests;

		private RequestThread requestThread; 

		/// <summary>
		/// Constructor initialize a AsyncWebRequest instance
		/// </summary>
		public AsyncWebRequest()	{ 
			queuedRequests = Hashtable.Synchronized(new Hashtable(17));	
			requestThread  = new RequestThread(this); 
		}

		/// <summary>
		/// Static constructor
		/// </summary>
		static AsyncWebRequest()	{	
			
			
			ServicePointManager.CertificatePolicy = new TrustSelectedCertificatePolicy();
			SetAllowUnsafeHeaderParsing();					
		
		}


		/// <summary>
		/// Returns the RequestThread used by this object. 
		/// </summary>
		internal RequestThread RequestThread{
			get { return this.requestThread; }
		}

		/// <summary>
		/// Gets the pending queued requests.
		/// </summary>
		public int PendingRequests {
			get { return queuedRequests.Count; }
		}

		/// <summary>
		/// Contains the url's as keys and the allowed (user interaction needed) 
		/// CertificateIssue's within an ICollection as values.
		/// </summary>
		/// <remarks>That content should be maintained completely from within
		/// the OnCertificateIssue event.</remarks>
		public static Hashtable TrustedCertificateIssues { 
			set {	trustedCertificateIssues = value; }
			get { return trustedCertificateIssues; }
		}

		/// <summary>
		/// Used to a queue an HTTP request for processing
		/// </summary>
		/// <param name="requestParameter"></param>
		/// <param name="webRequestQueued"></param>
		/// <param name="webRequestComplete"></param>
		/// <param name="webRequestException"></param>
		/// <param name="webRequestStart"></param>
		/// <param name="priority"></param>
		/// <exception cref="NotSupportedException">The request scheme specified in address has not been registered.</exception>
		/// <exception cref="ArgumentNullException">The requestParameter is a null reference</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have permission to connect to the requested URI or a URI that the request is redirected to.</exception>
		internal RequestState QueueRequest(RequestParameter requestParameter, 
			RequestQueuedCallback webRequestQueued,  
			RequestStartCallback webRequestStart, 
			RequestCompleteCallback webRequestComplete, 
			RequestExceptionCallback webRequestException, 
			int priority) {
			return QueueRequest(requestParameter, webRequestQueued, webRequestStart, webRequestComplete, webRequestException, null, priority, null);
		}

		/// <summary>
		/// Used to a queue an HTTP request for processing
		/// </summary>
		/// <param name="requestParameter"></param>
		/// <param name="webRequestQueued"></param>
		/// <param name="webRequestComplete"></param>
		/// <param name="webRequestException"></param>
		/// <param name="webRequestStart"></param>
		/// <param name="priority"></param>
		/// <exception cref="NotSupportedException">The request scheme specified in address has not been registered.</exception>
		/// <exception cref="ArgumentNullException">The requestParameter is a null reference</exception>
		/// <exception cref="System.Security.SecurityException">The caller does not have permission to connect to the requested URI or a URI that the request is redirected to.</exception>
		internal RequestState QueueRequest(RequestParameter requestParameter, 
			RequestQueuedCallback webRequestQueued,  
			RequestStartCallback webRequestStart, 
			RequestCompleteCallback webRequestComplete, 
			RequestExceptionCallback webRequestException, 
			RequestProgressCallback webRequestProgress,
			int priority) {
			return QueueRequest(requestParameter, webRequestQueued, webRequestStart, webRequestComplete, webRequestException, webRequestProgress, priority, null);
		}
		
		/// <summary>
		/// Called for first and subsequent requests.
		/// </summary>
		/// <param name="requestParameter">Could be modified for each subsequent request</param>
		/// <param name="webRequestQueued"></param>
		/// <param name="webRequestComplete"></param>
		/// <param name="webRequestException"></param>
		/// <param name="webRequestStart"></param>
		/// <param name="priority"></param>
		/// <param name="prevState">If subsequent request, this should contain the previous RequestState</param>
		internal RequestState QueueRequest(RequestParameter requestParameter, 
			RequestQueuedCallback webRequestQueued, 
			RequestStartCallback webRequestStart, 
			RequestCompleteCallback webRequestComplete, 
			RequestExceptionCallback webRequestException, 
			RequestProgressCallback  webRequestProgress,
			int priority, RequestState prevState) {
 
			if (requestParameter == null)
				throw new ArgumentNullException("requestParameter");

			if (prevState == null && queuedRequests.Contains(requestParameter.RequestUri.AbsoluteUri))
				return null;	// httpRequest already there

			// here are the exceptions caused:
			WebRequest webRequest = WebRequest.Create(requestParameter.RequestUri);
				
			HttpWebRequest httpRequest   = webRequest as HttpWebRequest;
			FileWebRequest fileRequest = webRequest as FileWebRequest;
			NntpWebRequest nntpRequest = webRequest as NntpWebRequest;

			if (httpRequest != null) {	
				// set extended HttpWebRequest params
				if(webRequestProgress != null){
					httpRequest.Timeout             = DefaultTimeout * 30; //one hour timeout for enclosures
				}else{
					httpRequest.Timeout				= DefaultTimeout; // two minutes timeout 
				}
				httpRequest.UserAgent			= FullUserAgent(requestParameter.UserAgent); 
				httpRequest.Proxy					= requestParameter.Proxy;
				httpRequest.AllowAutoRedirect = false; 
				httpRequest.Headers.Add("Accept-Encoding", "gzip, deflate"); 
	
				// due to the reported bug 893620 some web server fail with a server error 500
				// if we send DateTime.MinValue as IfModifiedSince. Smoe Unix derivates only know
				// about valid lowest DateTime around 1970. So in the case we use the
				// httpRequest class default setting:
				if (requestParameter.LastModified > MinValue) {
					httpRequest.IfModifiedSince = requestParameter.LastModified; 
				}

				/* #if DEBUG
							// further to investigate: with this setting we don't leak connections
							// (try TCPView from http://www.sysinternals.com)
							// read:
							// * http://support.microsoft.com/default.aspx?scid=kb%3Ben-us%3B819450
							// * http://cephas.net/blog/2003/10/29/the_intricacies_of_http.html
							// * http://weblogs.asp.net/jan/archive/2004/01/28/63771.aspx
			
							httpRequest.KeepAlive = false;		// to prevent open HTTP connection leak
							httpRequest.ProtocolVersion = HttpVersion.Version10;	// to prevent "Underlying connection closed" exception(s)
#endif */ 
			
				if (httpRequest.Proxy == null) {
					httpRequest.KeepAlive = false;	
					httpRequest.Proxy = GlobalProxySelection.GetEmptyWebProxy();
					httpRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
				}

				if(requestParameter.ETag != null){
					httpRequest.Headers.Add("If-None-Match", requestParameter.ETag); 
					httpRequest.Headers.Add("A-IM", "feed"); 
				}

				if(requestParameter.Credentials != null){
					httpRequest.KeepAlive = true;		// required for authentication to succeed
					httpRequest.ProtocolVersion = HttpVersion.Version11;	// switch back
					httpRequest.Credentials = requestParameter.Credentials;
				}

				if (requestParameter.SetCookies) {
					HttpCookieManager.SetCookies(httpRequest);
				}

				//this prevents the feed mixup issue that we've been facing. See 
				//http://www.davelemen.com/archives/2006/04/rss_bandit_feeds_mix_up.html
				//for a user complaint about the issue. 
				httpRequest.Pipelined = false; 

			} else if (fileRequest != null) {

				fileRequest.Timeout = DefaultTimeout; 

				if(requestParameter.Credentials != null){
					fileRequest.Credentials = requestParameter.Credentials; 
				}

			} else if (nntpRequest != null) {
				// ten minutes timeout. Large timeout is needed if this is first time we are fetching news
				nntpRequest.Timeout = DefaultTimeout * 5; 
				
				if(requestParameter.Credentials != null){
					nntpRequest.Credentials = requestParameter.Credentials; 
				}

				if (requestParameter.LastModified > MinValue) {
					nntpRequest.IfModifiedSince = requestParameter.LastModified; 
				}

			}else {
				
				Debug.Assert(false, "QueueRequest(): unsupported WebRequest type: " + webRequest.GetType().ToString());

			}

			RequestState state = null;

			if (prevState != null) {
				
				state = prevState;

				IDisposable dispResponse = state.Response as IDisposable;
				if (dispResponse != null) {
					dispResponse.Dispose();
					state.Response = null;
				}

				if (state.ResponseStream != null) {	// we don't want to get out of connections
					state.ResponseStream.Close(); 
				}

				if (state.Request != null ) {
					if (state.Request.Credentials != null) {
						state.Request.Credentials = null; 
					}
					// prevent NotImplementedExceptions:
					if (state.Request is HttpWebRequest)
						state.Request.Abort();
				}

			} else { 

				state = new RequestState(this);
				
				state.WebRequestQueued += webRequestQueued;
				state.WebRequestStarted += webRequestStart;
				state.WebRequestCompleted += webRequestComplete;
				state.WebRequestException += webRequestException;
				state.WebRequestProgress  += webRequestProgress;
				state.Priority = priority;		// needed for additional requests
				state.InitialRequestUri = webRequest.RequestUri;
			}

			state.Request = webRequest;
			state.RequestParams = requestParameter;

			if (prevState == null) {	// first httpRequest
				queuedRequests.Add(requestParameter.RequestUri.AbsoluteUri, null);
				state.OnRequestQueued(requestParameter.RequestUri);
			}
			
			RequestThread.QueueRequest(state, priority);

			return state;
		}

		/// <summary>
		/// Internal workaround. 
		/// DO NOT USE THIS IN YOUR OWN PROJECTS, UNLESS YOU REALLY KNOW WHAT
		/// YOU ARE DOING!!!
		/// Fixes the fix in .NET 1.1 SP1 and .NET 1.0 SP3
		/// they made the header parsing more standards compliant, but this
		/// leads to various HTTP protocol violation exceptions on subscribed feeds :-(
		/// </summary>
		private static void SetAllowUnsafeHeaderParsing() {
						
			// because we only need to modify statics, we just create a dummy reference to get it initialized
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost/dummyUrl");
			if (request != null) {
				Type reqestType = request.GetType();
				// this test also sets the internal used s_NetConfig variable on first call
				// and we did not have to test for any Framework version numbers 
				try {
					bool isSet = (bool) reqestType.InvokeMember("UseUnsafeHeaderParsing", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty, null, request, new object[]{});
					if (! isSet) {
						// get the s_NetConfig reference:
						FieldInfo fi = reqestType.GetField("s_NetConfig" , BindingFlags.Static | BindingFlags.NonPublic );
						object s_netConfig = (fi != null ? fi.GetValue(request) : null);
						if (s_netConfig != null) {
							// OK, we have it. Get the type for calling Field.SetValue() later
							Type netConfigType = s_netConfig.GetType();
							FieldInfo fi2 = netConfigType.GetField("useUnsafeHeaderParsing" , BindingFlags.NonPublic | BindingFlags.Instance);
							if (fi2 != null)
								fi2.SetValue(s_netConfig, true);	// that's it
						}
					}
				} catch (Exception ex) {
					_log.Warn("SetAllowUnsafeHeaderParsing() failed", ex);
				}
			}
		}

		/// <summary>
		/// To be provided
		/// </summary>
		/// <param name="userAgent"></param>
		/// <returns></returns>
		public static string FullUserAgent(string userAgent) {
			return NewsHandler.UserAgentString(userAgent); 
		}


		/// <summary>
		/// Callback that is fired if an HTTP request times out
		/// </summary>
		/// <param name="input">the RequestState object</param>
		/// <param name="timedOut">indicates whether a time out occured</param>
		public void TimeoutCallback(object input, bool timedOut) { 
			if (timedOut) {
				RequestState state = (RequestState) input;
				_log.Info("Request Timeout: "+state.RequestUri.ToString());
				//TODO: translate exception message:
				state.OnRequestException(new WebException("Request timeout", WebExceptionStatus.Timeout));
				FinalizeWebRequest(state); 
			}
		}

	


		/// <summary>
		/// Cancels the request
		/// </summary>
		/// <param name="state"></param>
		internal void RequestStartCancelled(RequestState state) {
			if (state != null && !state.requestFinalized) {
				_log.Info("RequestStart cancelled: "+state.RequestUri.ToString());
				state.OnRequestCompleted(state.RequestParams.ETag, state.RequestParams.LastModified, RequestResult.NotModified);
				queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri);
				state.requestFinalized = true;
			
				if (queuedRequests.Count == 0 && RequestThread.RunningRequests <= 0)
					RaiseOnAllRequestsComplete();
			}
		}

		/// <summary>
		/// Call it to cleanup any made request.
		/// </summary>
		/// <param name="state"></param>
		internal void FinalizeWebRequest(RequestState state) {
			if (state != null && !state.requestFinalized) {
				
				_log.Debug("Request finalized. Request of '"+state.InitialRequestUri.AbsoluteUri+"' took "+DateTime.Now.Subtract(state.StartTime)+" seconds");
				
				// ensure we close the resource so we do not get out of INet connections
				try {

					if (state.ResponseStream != null) {
						state.ResponseStream.Close(); 
					}

					IDisposable dispResponse = state.Response as IDisposable;
					if (dispResponse != null) {
						dispResponse.Dispose();
						state.Response = null;
					}// else {	// no response object
					//	state.Request.Abort();
					//}
					if (state.Request != null ) {
						if (state.Request.Credentials != null) {
							state.Request.Credentials = null; 
						}
						// prevent NotImplementedExceptions:
						if (state.Request is HttpWebRequest)
							state.Request.Abort();
					}
				} catch {}

				queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri);
				RequestThread.EndRequest(state);	// trigger next available threaded request
				state.requestFinalized = true;
			
				if (queuedRequests.Count == 0 && RequestThread.RunningRequests <= 0)
					RaiseOnAllRequestsComplete();

			}
		}

		
		/// <summary>
		/// Callback gets called if BeginGetResponse() has any result.
		/// </summary>
		/// <param name="result"></param>
		internal void ResponseCallback(IAsyncResult result) {
			
			RequestState state = null;
			try {
				state = result.AsyncState as RequestState;
			} catch {}

			if (state == null)
				return;

			HttpWebResponse httpResponse = null;
			FileWebResponse fileResponse = null;
			NntpWebResponse nntpResponse = null;
			string eTag = null;

			try {
				try{
					state.Response = state.Request.EndGetResponse(result);
				}catch(Exception exception){
					WebException we = exception as WebException; 

					if(we != null && we.Response != null){
						state.Response = we.Response; 						
					}else{
						throw; 
					}
				}
							
				httpResponse = state.Response as HttpWebResponse;
				fileResponse = state.Response as FileWebResponse;
				nntpResponse = state.Response as NntpWebResponse;

				if (httpResponse != null) {

					if(httpResponse.ResponseUri != state.RequestUri){
						_log.Debug(String.Format("httpResponse.ResponseUri != state.RequestUri: \r\n'{0}'\r\n'{1}'", httpResponse.ResponseUri.ToString(), state.RequestUri.ToString()));
					}

					if (HttpStatusCode.OK == httpResponse.StatusCode || 
					   HttpExtendedStatusCode.IMUsed == (HttpExtendedStatusCode)httpResponse.StatusCode){ 

						HttpCookieManager.GetCookies(httpResponse);

						// provide last request Uri and ETag:
						state.RequestParams.ETag = httpResponse.Headers.Get("ETag");
						try {
							state.RequestParams.LastModified = httpResponse.LastModified;
						} catch (Exception lmEx) {
							_log.Debug( "httpResponse.LastModified() parse failure: "+ lmEx.Message);		
							// Build in header parser failed on provided date format
							// Try our own parser (last chance)
							try {
								state.RequestParams.LastModified = DateTimeExt.Parse(httpResponse.Headers.Get("Last-Modified"));
							} catch {/* ignore */}
						}
					
						state.ResponseStream = httpResponse.GetResponseStream();
						state.ResponseStream.BeginRead(state.BufferRead, 0, RequestState.BUFFER_SIZE, new AsyncCallback(ReadCallback), state);
						// async read started, so we are done here:
						_log.Debug( "ResponseCallback() web response OK: "+ state.RequestUri);		

						return;

					} else if (httpResponse.StatusCode == HttpStatusCode.NotModified) { 

						HttpCookieManager.GetCookies(httpResponse);

						eTag = httpResponse.Headers.Get("ETag");
						// also if it was not modified, we receive a httpResponse.LastModified with current date!
						// so we did not store it (is is just the same as last-retrived)
						// provide last request Uri and ETag:
						state.OnRequestCompleted(state.InitialRequestUri, state.RequestParams.RequestUri, eTag, MinValue, RequestResult.NotModified);
						// cleanup:
						FinalizeWebRequest(state);

					} else if ((httpResponse.StatusCode == HttpStatusCode.MovedPermanently)
						|| (httpResponse.StatusCode == HttpStatusCode.Moved)) {
											
						state.RetryCount++;
						if (state.RetryCount > RequestState.MAX_RETRIES) {// there is no WebExceptionStatus.UnknownError in .NET 1.0 !!!
							throw new WebException("Repeated HTTP httpResponse: " + httpResponse.StatusCode.ToString(), null, WebExceptionStatus.RequestCanceled, httpResponse);
						}

						string url2 = httpResponse.Headers["Location"]; 
						//Check for any cookies
						HttpCookieManager.GetCookies(httpResponse);

						state.movedPermanently = true;
						//Remove Url from queue 
						queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri); 

						_log.Debug( "ResponseCallback() Moved: '"+ state.InitialRequestUri + " to " + url2);		

						// Enqueue the request with the new Url. 
						// We raise the queue priority a bit to get the retry request closer to the just
						// finished one. So the user get better feedback, because the whole processing
						// of one request (including the redirection/moved/... ) is visualized as one update
						// action.
						try {
							RequestParameter rqp = RequestParameter.Create(url2, state.RequestParams);
							QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
						} catch (UriFormatException) {
							try {
								url2 = HtmlHelper.ConvertToAbsoluteUrl(url2, httpResponse.ResponseUri, false);
								RequestParameter rqp = RequestParameter.Create(url2, state.RequestParams);
								QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
							} catch (UriFormatException uex) {
								throw new WebException("Original resource moved. Requesting new resource at '" + url2 + "' failed: " +uex.Message, uex);
							}
						}

						// ping the queue listener thread to Dequeue the next request
						RequestThread.EndRequest(state);

					} else if (IsRedirect(httpResponse.StatusCode)) {
						
						state.RetryCount++;
						if (state.RetryCount > RequestState.MAX_RETRIES) {// there is no WebExceptionStatus.UnknownError in .NET 1.0 !!!
							throw new WebException("Repeated HTTP httpResponse: " + httpResponse.StatusCode.ToString(), null, WebExceptionStatus.RequestCanceled, httpResponse);
						}

						string url2 = httpResponse.Headers["Location"]; 
						//Check for any cookies
						HttpCookieManager.GetCookies(httpResponse);

						//Remove Url from queue 
						queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri); 

						_log.Debug( "ResponseCallback() Redirect: '"+ state.InitialRequestUri + " to " + url2);		
						// Enqueue the request with the new Url. 
						// We raise the queue priority a bit to get the retry request closer to the just
						// finished one. So the user get better feedback, because the whole processing
						// of one request (including the redirection/moved/... ) is visualized as one update
						// action.
						try {
							RequestParameter rqp = RequestParameter.Create(url2, RebuildCredentials(state.RequestParams.Credentials, url2), state.RequestParams);
							QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
						} catch (UriFormatException) {
							try {
								url2 = HtmlHelper.ConvertToAbsoluteUrl(url2, httpResponse.ResponseUri, false);
								RequestParameter rqp = RequestParameter.Create(url2, RebuildCredentials(state.RequestParams.Credentials, url2), state.RequestParams);
								QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
							} catch (UriFormatException uex) {
								throw new WebException("Original resource temporary redirected. Request new resource at '" + url2 + "' failed: " +uex.Message, uex);
							}
						}

						// ping the queue listener thread to Dequeue the next request
						RequestThread.EndRequest(state);

					} else if (IsUnauthorized(httpResponse.StatusCode)) {
						
						if (state.RequestParams.Credentials == null)	{	// no initial credentials, try with default credentials
							state.RetryCount++;
						
							//Remove Url from queue 
							queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri); 

							// Enqueue the request with the new Url. 
							// We raise the queue priority a bit to get the retry request closer to the just
							// finished one. So the user get better feedback, because the whole processing
							// of one request (including the redirection/moved/... ) is visualized as one update
							// action.
							RequestParameter rqp = RequestParameter.Create(CredentialCache.DefaultCredentials, state.RequestParams);
							QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
							// ping the queue listener thread to Dequeue the next request
							RequestThread.EndRequest(state);
						
						} else {	// failed with provided credentials
						
							if (state.RequestParams.SetCookies) {	// one more request without cookies
								
								state.RetryCount++;
						
								//Remove Url from queue 
								queuedRequests.Remove(state.InitialRequestUri.AbsoluteUri); 

								// Enqueue the request with the new Url. 
								// We raise the queue priority a bit to get the retry request closer to the just
								// finished one. So the user get better feedback, because the whole processing
								// of one request (including the redirection/moved/... ) is visualized as one update
								// action.
								RequestParameter rqp = RequestParameter.Create(false, state.RequestParams);
								QueueRequest(rqp, null, null, null, null, null, state.Priority+1, state);
								// ping the queue listener thread to Dequeue the next request
								RequestThread.EndRequest(state);

							} else {
								throw new ResourceAuthorizationException();
							}
						}
					} else if (httpResponse.StatusCode == HttpStatusCode.Gone) { 
						throw new ResourceGoneException(); 
					} else {
						string statusCode = httpResponse.StatusCode.ToString(); 
						throw new WebException("Unexpected HTTP httpResponse: " + statusCode); 
					}
								
				} else if (fileResponse != null) {

					string reqFile = fileResponse.ResponseUri.LocalPath;
					
					if (File.Exists(reqFile)) {
						DateTime lwt = File.GetLastWriteTime(reqFile);
						state.RequestParams.ETag = lwt.ToString();
						state.RequestParams.LastModified = lwt;
					}
					
					state.ResponseStream = fileResponse.GetResponseStream();
					state.ResponseStream.BeginRead(state.BufferRead, 0, RequestState.BUFFER_SIZE, new AsyncCallback(ReadCallback), state);
					// async read started, so we are done here:
					_log.Debug( "ResponseCallback() file response OK: "+ state.RequestUri);		

					return;

				} else if (nntpResponse != null) {					
					
					state.RequestParams.LastModified = DateTime.Now; 
					state.ResponseStream = nntpResponse.GetResponseStream();
					state.ResponseStream.BeginRead(state.BufferRead, 0, RequestState.BUFFER_SIZE, new AsyncCallback(ReadCallback), state);
					// async read started, so we are done here:
					_log.Debug( "ResponseCallback() nntp response OK: "+ state.RequestUri);		

					return;

				}else {

					Debug.Assert(false, "ResponseCallback(): unhandled WebResponse type: " + state.Response.GetType().ToString());
					FinalizeWebRequest(state);

				}

			} catch (System.Threading.ThreadAbortException) {
				FinalizeWebRequest(state);
				return;	// ignore, just return
			} catch (Exception ex) {
				// does also cleanup, by calling FinalizeWebRequest(state):
				state.OnRequestException(state.InitialRequestUri, ex);
			}
			
		}

		private static ICredentials RebuildCredentials(ICredentials credentials, string redirectUrl) {
			CredentialCache cc = credentials as CredentialCache;
			
			if (cc != null) {
				
				IEnumerator iterate = cc.GetEnumerator();
				while (iterate.MoveNext()) {
					NetworkCredential c = iterate.Current as NetworkCredential;
					if (c != null) {	// we just take the first one to recreate 
						string domainUser = c.Domain;
						if (!StringHelper.EmptyOrNull(domainUser))
							domainUser = domainUser + @"\";
						domainUser = String.Concat(domainUser, c.UserName);
						return NewsHandler.CreateCredentialsFrom(redirectUrl, domainUser, c.Password);
					}
				}
			}
			// give up/forward original credentials:
			return credentials;	
		}

		/// <summary>
		/// Callback gets called (recursively) on subsequent response stream read requests
		/// </summary>
		/// <param name="result"></param>
		private void ReadCallback(IAsyncResult result) {

			RequestState state = null;
			try {
				state = result.AsyncState as RequestState;
			} catch {}

			if (state == null)
				return;

			try {

				Stream responseStream = state.ResponseStream;
				int read = responseStream.EndRead( result );
				
				// fix at least one of the leaks in CLR 1.1 (and 1.0?)
				// see also http://dturini.blogspot.com/2004/06/on-past-few-days-im-dealing-with-some.html
				// and: http://support.microsoft.com/?kbid=831138
				if (Common.ClrVersion.Major < 2 && result.AsyncWaitHandle != null)
					result.AsyncWaitHandle.Close();

				if (read > 0) {
					state.bytesTransferred += read; 
					state.RequestData.Write(state.BufferRead, 0, read);	// write buffer to mem stream, queue next read:
					responseStream.BeginRead(state.BufferRead, 0, RequestState.BUFFER_SIZE, new AsyncCallback(ReadCallback), state);
					
					if(((state.bytesTransferred / RequestState.BUFFER_SIZE) % 10) == 0){
						state.OnRequestProgress(state.InitialRequestUri, state.bytesTransferred); 
					}
					return;
				} else {
					// completed
					if (state.Response is HttpWebResponse) {
						state.ResponseStream = GetDeflatedResponse(((HttpWebResponse)state.Response).ContentEncoding, state.RequestData);
					} else {
						state.ResponseStream = GetDeflatedResponse(String.Empty, state.RequestData);
					}
					state.OnRequestCompleted(state.InitialRequestUri, state.RequestParams.RequestUri, state.RequestParams.ETag, state.RequestParams.LastModified, RequestResult.OK);
					// usual cleanup:
					responseStream.Close();
					state.RequestData.Close();
				}

			}
			catch(WebException e) {
				_log.Error("ReadCallBack WebException raised. Status: " + e.Status, e);
				state.OnRequestException(state.RequestParams.RequestUri, e);
			}
			catch(Exception e) {
				_log.Error("ReadCallBack Exception raised", e);
				state.OnRequestException(state.RequestParams.RequestUri, e);
			}

			FinalizeWebRequest(state);
		
		}

//		/// <summary>
//		/// Returns a deflated stream of the response sent by a web request. If the 
//		/// web server did not send a compressed stream then the original stream is returned
//		/// as a seekable MemoryStream. 
//		/// </summary>
//		/// <param name="response">WebResponse</param>
//		/// <returns>seekable Stream</returns>
//		public static Stream GetDeflatedResponse(WebResponse response){
//			if (response is HttpWebResponse)
//				return GetDeflatedResponse((HttpWebResponse)response);
//			else
//				return ResponseToMemory(response.GetResponseStream());
//		}

		/// <summary>
		/// Returns a deflated version of the response sent by the web server. If the 
		/// web server did not send a compressed stream then the original stream is returned
		/// as a seekable MemoryStream. 
		/// </summary>
		/// <param name="response">HttpWebResponse</param>
		/// <returns>seekable Stream</returns>
		public static Stream GetDeflatedResponse(HttpWebResponse response){
			return GetDeflatedResponse(response.ContentEncoding, 
				ResponseToMemory(response.GetResponseStream()));
		}

		/// <summary>
		/// Overload for FileWebResponse.
		/// </summary>
		/// <param name="response">FileWebResponse</param>
		/// <returns>seekable Stream</returns>
		public static Stream GetDeflatedResponse(FileWebResponse response){
			return ResponseToMemory(response.GetResponseStream());
		}

		/// <summary>
		/// Returns a deflated version of the response sent by the web server. If the 
		/// web server did not send a compressed stream then the original stream is returned. 
		/// </summary>
		/// <param name="encoding">Encoding of the stream. One of 'deflate' or 'gzip' or Empty.</param>
		/// <param name="inputStream">Input Stream</param>
		/// <returns>Seekable Stream</returns>
		public static Stream GetDeflatedResponse(string encoding, Stream inputStream){
			
			const int BUFFER_SIZE = 4096;	// 4K read buffer

			Stream compressed = null, input = inputStream; 
			bool tryAgainDeflate = true;
			
			if (input.CanSeek)
				input.Seek(0, SeekOrigin.Begin);

			if (encoding=="deflate") {	//to solve issue "invalid checksum" exception with dasBlog and "deflate" setting:
				//input = ResponseToMemory(input);			// need them within mem to have a seekable stream
				compressed = new InflaterInputStream(input);	// try deflate with headers
			}else if (encoding=="gzip") {
				compressed = new GZipInputStream(input);
			} else {
				// allready seeked, just return
				return input;
			}

			while (true) {
			
				MemoryStream decompressed = new MemoryStream();

				try {

					int size = BUFFER_SIZE;
					byte[] writeData = new byte[BUFFER_SIZE];
					while (true) {
						size = compressed.Read(writeData, 0, size);
						if (size > 0) {
							decompressed.Write(writeData, 0, size);
						} 
						else {
							break;
						}
					}

					//reposition to beginning of decompressed stream then return
					decompressed.Seek(0, SeekOrigin.Begin);
					return decompressed;

				} catch (SharpZipBaseException) {
					if (tryAgainDeflate && (encoding=="deflate")) {
						input.Seek(0, SeekOrigin.Begin);	// reset position
						compressed = new InflaterInputStream(input, new ICSharpCode.SharpZipLib.Zip.Compression.Inflater(true));
						tryAgainDeflate = false; 
					} else
						throw;
				}
				
			} // while(true)

		}

		/// <summary>
		/// Helper to copy a non-seekable stream (like from a HttpResponse) to a seekable memory stream.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static Stream ResponseToMemory(Stream input) {
			const int BUFFER_SIZE = 4096;	// 4K read buffer
			MemoryStream output = new MemoryStream();
			int size = BUFFER_SIZE;
			byte[] writeData = new byte[BUFFER_SIZE];
			while (true) {
				size = input.Read(writeData, 0, size);
				if (size > 0) {
					output.Write(writeData, 0, size);
				} 
				else {
					break;
				}
			}
			output.Seek(0, SeekOrigin.Begin);
			return output;
		}

		/// <summary>
		/// Helper method checks if a status code is a redirect or not
		/// </summary>
		/// <param name="statusCode"></param>
		/// <returns>True if the status code is a redirect</returns>
		public static bool IsRedirect(HttpStatusCode statusCode){
		
			if( (statusCode == HttpStatusCode.Ambiguous)
				|| (statusCode == HttpStatusCode.Found)
				|| (statusCode == HttpStatusCode.MultipleChoices) 
				|| (statusCode == HttpStatusCode.Redirect)
				|| (statusCode == HttpStatusCode.RedirectKeepVerb)
				|| (statusCode == HttpStatusCode.RedirectMethod)
				|| (statusCode == HttpStatusCode.TemporaryRedirect)
				|| (statusCode == HttpStatusCode.SeeOther)){
				return true;
			}else{
				return false; 
			}

		}
		/// <summary>
		/// Helper method checks if a status code is a unauthorized or not
		/// </summary>
		/// <param name="statusCode"></param>
		/// <returns>True if the status code is unauthorized</returns>
		public static bool IsUnauthorized(HttpStatusCode statusCode) {
			if( statusCode == HttpStatusCode.Unauthorized)
				return true;
			return false;
		}
		
		/// <summary>
		/// Can be called syncronized to get a HttpWebResponse.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="userAgent"></param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="ifModifiedSince">Header date</param>
		/// <param name="eTag">Header tag</param>
		/// <param name="timeout">Request timeout. E.g. 60 * 1000, means one minute timeout. 
		/// If zero or less than zero, the default timeout of one minute will be used</param>
		/// <returns>WebResponse</returns>
		public static WebResponse GetSyncResponse(string address, ICredentials credentials, string userAgent,
			IWebProxy proxy, DateTime ifModifiedSince, string eTag, int timeout) {

			try{ 
				WebRequest webRequest = WebRequest.Create(address);
				
				HttpWebRequest httpRequest   = webRequest as HttpWebRequest;
				FileWebRequest fileRequest = webRequest as FileWebRequest;

				if (httpRequest != null) {
					httpRequest.Timeout				= (timeout <= 0 ? DefaultTimeout: timeout); //one minute timeout, if lower than zero
					httpRequest.UserAgent			= FullUserAgent(userAgent);  
					httpRequest.Proxy					= proxy;
					httpRequest.AllowAutoRedirect = false; 
					httpRequest.IfModifiedSince		= ifModifiedSince; 
					httpRequest.Headers.Add("Accept-Encoding", "gzip, deflate"); 
	
					if(eTag != null){
						httpRequest.Headers.Add("If-None-Match", eTag); 
						httpRequest.Headers.Add("A-IM", "feed"); 
					}

					if(credentials != null){
						httpRequest.Credentials = credentials; 
					}

				} else if (fileRequest != null) {
					
					fileRequest.Timeout = (timeout <= 0 ? DefaultTimeout: timeout); 
					if(credentials != null){
						fileRequest.Credentials = credentials; 
					}
					
				} else {

					Debug.Assert(false, "GetSyncResponse(): unhandled WebRequest type: " + webRequest.GetType().ToString());

				}

				return webRequest.GetResponse();
			
			}catch(Exception e){ //For some reason the HttpWebResponse class throws an exception on 3xx responses
			
				WebException we = e as WebException; 

				if((we != null) && (we.Response != null)){ 
					
					return we.Response;							
						
				}else{
					throw; 
				}

			}//end try/catch
			
		}

		#region GetSyncResponseHeadersOnly()

		/// <summary>
		/// Can be called syncronized to get a HttpWebResponse (Headers only!).
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="timeout">Request timeout. E.g. 60 * 1000, means one minute timeout. 
		/// If zero or less than zero, the default timeout of one minute will be used</param>
		/// <returns>WebResponse</returns>
		public static WebResponse GetSyncResponseHeadersOnly(string address, IWebProxy proxy, int timeout) {
			return GetSyncResponseHeadersOnly(address, proxy, timeout, null);
		}

		/// <summary>
		/// Can be called syncronized to get a HttpWebResponse (Headers only!).
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="timeout">Request timeout. E.g. 60 * 1000, means one minute timeout. 
		/// If zero or less than zero, the default timeout of one minute will be used</param>
		/// <param name="credentials">ICredentials</param>
		/// <returns>WebResponse</returns>
		public static WebResponse GetSyncResponseHeadersOnly(string address, IWebProxy proxy, int timeout, ICredentials credentials) {

			try{ 
				HttpWebRequest httpRequest   = (HttpWebRequest)WebRequest.Create(address);

				httpRequest.Timeout		= (timeout <= 0 ? DefaultTimeout: timeout); //one minute timeout, if lower than zero
				if (proxy != null)
					httpRequest.Proxy		= proxy;
				if (credentials != null)
					httpRequest.Credentials = credentials;
				httpRequest.Method		= "HEAD";

				return httpRequest.GetResponse();
			
			}catch(Exception e){ //For some reason the HttpWebResponse class throws an exception on 3xx responses
			
				WebException we = e as WebException; 

				if((we != null) && (we.Response != null)){ 
					return we.Response;							
				}else{
					throw; 
				}
			}//end try/catch
		}

		#endregion

		#region GetSyncResponseStream() 

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="proxy">Proxy to use</param>
		public static Stream GetSyncResponseStream(string address, ICredentials credentials, IWebProxy proxy){
			return GetSyncResponseStream(address, credentials, FullUserAgent(null), proxy, DefaultTimeout);
		}

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="timeout">Timeout in msecs</param>
		public static Stream GetSyncResponseStream(string address, ICredentials credentials, IWebProxy proxy, int timeout){
			return GetSyncResponseStream(address, credentials, FullUserAgent(null), proxy, timeout);
		}

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="userAgent"></param>
		/// <param name="proxy">Proxy to use</param>
		public static Stream GetSyncResponseStream(string address, ICredentials credentials, string userAgent, IWebProxy proxy){
			return GetSyncResponseStream(address, credentials, userAgent, proxy, DefaultTimeout);
		}

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="userAgent"></param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="timeout">Timeout in msecs</param>
		public static Stream GetSyncResponseStream(string address, ICredentials credentials, string userAgent, IWebProxy proxy, int timeout){
			string newAddress = null, eTag = null;
			RequestResult result; DateTime ifModifiedSince = MinValue;
			return GetSyncResponseStream(address, out newAddress, credentials, userAgent, proxy, ref ifModifiedSince, ref eTag, timeout, out result);
		}

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="newAddress">New Url, if redirected</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="userAgent"></param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="timeout">Timeout in msecs</param>
		public static Stream GetSyncResponseStream(string address, out string newAddress, ICredentials credentials, string userAgent, IWebProxy proxy, int timeout){
			string eTag = null;
			RequestResult result; DateTime ifModifiedSince = MinValue;
			return GetSyncResponseStream(address, out newAddress, credentials, userAgent, proxy, ref ifModifiedSince, ref eTag, timeout, out result);
		}

		/// <summary>
		/// Can be called syncronized to get a Http Web ResponseStream.
		/// </summary>
		/// <param name="address">Url to request</param>
		/// <param name="newAddress">out string. return a new url, if the original requested is permanent moved</param>
		/// <param name="credentials">Url credentials</param>
		/// <param name="userAgent"></param>
		/// <param name="proxy">Proxy to use</param>
		/// <param name="ifModifiedSince">Header date</param>
		/// <param name="eTag">Header tag</param>
		/// <param name="timeout">Request timeout. E.g. 60 * 1000, means one minute timeout.
		/// If zero or less than zero, the default timeout of one minute will be used</param>
		/// <param name="responseResult">out. Result of the request</param>
		/// <returns>Stream</returns>
		public static Stream GetSyncResponseStream(string address, out string newAddress, ICredentials credentials, string userAgent,
		                                           IWebProxy proxy, ref DateTime ifModifiedSince, ref string eTag, int timeout, out RequestResult responseResult) {

			bool useDefaultCred = false;
			int requestRetryCount = 0; const int MaxRetries = 25;
			
			newAddress = null;

			send_request:
			    
			string requestUri = address;	
			if (useDefaultCred)
				credentials = CredentialCache.DefaultCredentials;

			WebResponse wr = 
				GetSyncResponse(address, credentials, userAgent, proxy, ifModifiedSince, eTag, timeout);
			
			HttpWebResponse response = wr as HttpWebResponse;
			FileWebResponse fileresponse = wr as FileWebResponse;

			if (response != null) {
			
				if (HttpStatusCode.OK == response.StatusCode || 
					HttpExtendedStatusCode.IMUsed == (HttpExtendedStatusCode)response.StatusCode){ 

					responseResult = RequestResult.OK;
					Stream ret = AsyncWebRequest.GetDeflatedResponse(response); 
					response.Close();
					return ret; 							
						
				}else if((response.StatusCode == HttpStatusCode.MovedPermanently)
					|| (response.StatusCode == HttpStatusCode.Moved)){
					
					newAddress = HtmlHelper.ConvertToAbsoluteUrl(response.Headers["Location"], address, false); 		
					address = newAddress; 									
					response.Close(); 

					if(requestRetryCount < MaxRetries){
						requestRetryCount++;
						goto send_request;
					}

				}else if( IsUnauthorized(response.StatusCode)){ //try with default credentials
						
					useDefaultCred = true; 
					response.Close(); 
					
					if(requestRetryCount<MaxRetries){
						requestRetryCount++;
						goto send_request;
					}

				}else if( AsyncWebRequest.IsRedirect(response.StatusCode)){
						
					address = HtmlHelper.ConvertToAbsoluteUrl(response.Headers["Location"], address, false); 		
					response.Close(); 
					 
					if(requestRetryCount<MaxRetries){
						requestRetryCount++;
						goto send_request;
					}

				} else if (response.StatusCode == HttpStatusCode.Gone) { 
					throw new ResourceGoneException(); 
				}else{ 
					string statusCode = response.StatusCode.ToString(); 
					response.Close(); 
					throw new WebException("Unexpected HTTP response: " + statusCode); 
				}	

				// unauthorized more than MaxRetries
				if (IsUnauthorized(response.StatusCode)) {
					response.Close(); 
					throw new ResourceAuthorizationException();
				}
					
				//we got a moved, redirect more than MaxRetries
				string returnCode = response.StatusCode.ToString(); 
				response.Close(); 
				throw new WebException("Repeated HTTP response: " + returnCode); 
			
			} else if (fileresponse != null) {
			
				responseResult = RequestResult.OK;
				Stream ret = AsyncWebRequest.GetDeflatedResponse(fileresponse); 
				fileresponse.Close();
				return ret; 							

			} else {
				throw new ApplicationException("no handler for WebResponse. Address: " + requestUri);
			}
		                                           }

		#endregion
		
		private void RaiseOnAllRequestsComplete() {
			if (OnAllRequestsComplete != null) {
				try {
					OnAllRequestsComplete();
				} catch {}
			}
		}

		internal static void RaiseOnCertificateIssue(object sender, CertificateIssueCancelEventArgs e) {
			string url = e.WebRequest.RequestUri.AbsoluteUri.ToString();
			ICollection trusted = null;
			
			if (trustedCertificateIssues != null) {
				lock (trustedCertificateIssues.SyncRoot) {
					if (trustedCertificateIssues.ContainsKey(url)) 
						trusted = (ICollection)trustedCertificateIssues[url];
				}
			}

			if (trusted != null && trusted.Count > 0) {
				foreach (CertificateIssue trustedIssue in trusted) {
					if (trustedIssue == e.CertificateIssue) {
						e.Cancel = false;	// is an yet accepted certificate isse
						return;
					}
				}
			}

			if (OnCertificateIssue != null) {
				try {
					OnCertificateIssue(sender, e);
				} catch {}
			}
		}
	}

	#region Certificate policy handling

	/// <summary>
	/// Possible Certificate issues.
	/// </summary>
	/// <remarks> The .NET Framwork should expose these, but they don't.</remarks>
 	[Serializable]
	public enum CertificateIssue : long {
		CertEXPIRED						= 0x800B0101,
		CertVALIDITYPERIODNESTING	= 0x800B0102,
		CertROLE							= 0x800B0103,
		CertPATHLENCONST				= 0x800B0104,
		CertCRITICAL						= 0x800B0105,
		CertPURPOSE						= 0x800B0106,
		CertISSUERCHAINING			= 0x800B0107,
		CertMALFORMED					= 0x800B0108,
		CertUNTRUSTEDROOT			= 0x800B0109,
		CertCHAINING						= 0x800B010A,
		CertREVOKED						= 0x800B010C,
		CertUNTRUSTEDTESTROOT		= 0x800B010D,
		CertREVOCATION_FAILURE		= 0x800B010E,
		CertCN_NO_MATCH				= 0x800B010F,
		CertWRONG_USAGE				= 0x800B0110,
		CertUNTRUSTEDCA				= 0x800B0112
	}

	/// <summary>
	/// Method signature to enable external handling of certificate issues.
	/// </summary>
	public delegate void CertificateIssueHandler(object sender, CertificateIssueCancelEventArgs e);

	/// <summary>
	/// Cancelable Event Argument class to handle certificate issues on web requests.
	/// </summary>
	[ComVisible(false)]
	public class CertificateIssueCancelEventArgs: System.ComponentModel.CancelEventArgs {
		/// <summary>
		/// Problem/Issue caused
		/// </summary>
		public CertificateIssue CertificateIssue;
		/// <summary>
		/// The certificate, that casued the problem
		/// </summary>
		public X509Certificate Certificate;
		/// <summary>
		/// The involved WebRequest.
		/// </summary>
		public WebRequest WebRequest;
		private CertificateIssueCancelEventArgs() {}
		/// <summary>
		/// Designated initializer
		/// </summary>
		/// <param name="issue">CertificateIssue</param>
		/// <param name="cert">X509Certificate</param>
		/// <param name="request">WebRequest</param>
		/// <param name="cancel">bool</param>
		public CertificateIssueCancelEventArgs(CertificateIssue issue, X509Certificate cert, WebRequest request, bool cancel): base(cancel) {
			this.CertificateIssue = issue;
			this.Certificate = cert;
			this.WebRequest = request;
		}
	}

	/// <summary>
	/// Does enable certificate acceptance. 
	/// See also http://weblogs.asp.net/tgraham/archive/2004/08/12/213469.aspx
	/// and http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpguide/html/cpconhostingremoteobjectsininternetinformationservicesiis.asp
	/// </summary>
	internal class TrustSelectedCertificatePolicy : System.Net.ICertificatePolicy {
		
		public TrustSelectedCertificatePolicy() { }

		public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) {
			try {
				if (problem != 0) {
					// move bits around to get it casted from an signed int to a normal long enum type:
					CertificateIssue issue = (CertificateIssue) (( (problem << 1 ) >> 1) + 0x80000000);
					CertificateIssueCancelEventArgs args = new CertificateIssueCancelEventArgs(issue, cert, req, true);
					AsyncWebRequest.RaiseOnCertificateIssue(sp, args);
					return !args.Cancel;
				}
			} catch (Exception ex) {
				Trace.WriteLine("TrustSelectedCertificatePolicy.CheckValidationResult() error: " + ex.Message);
			}
			// The 1.1 framework calls this method with a problem of 0, even if nothing is wrong
			return (problem == 0);
		}

	}

	#endregion

	#region cookie handling

	/// <summary>
	/// Used to manage cookies
	/// </summary>
	/// <remarks>see http://www.rendelmann.info/blog/CommentView.aspx?guid=bd99bcd5-7088-4d46-801e-c0fe622dc2e5</remarks>
	internal class HttpCookieManager {

		private static readonly log4net.ILog _log = RssBandit.Common.Logging.Log.GetLogger(typeof(HttpCookieManager));

		/// <summary>
		/// Retrieves the cookie(s) from windows system and assign them to the request, 
		/// if available.
		/// </summary>
		/// <param name="request">HttpWebRequest</param>
		public static void SetCookies(HttpWebRequest request) {
			CookieContainer c = GetCookieContainerUri(request.RequestUri);
			if (c.Count > 0)
				request.CookieContainer = c;
		}

		/// <summary>
		/// Gets newly received cookie(s) and make them persistent within windows system.
		/// </summary>
		/// <param name="response">HttpWebResponse</param>
		public static void GetCookies(HttpWebResponse response) {
			if (response.Headers["Set-Cookie"] != null) {
				/* 
				 * It seems this may log users out of certain sites, 
				 * see http://www.rssbandit.org/forum/topic.asp?whichpage=1&TOPIC_ID=2080&#4080
				 *	- InternetSetCookie(response.ResponseUri.AbsoluteUri, null, response.Headers["Set-Cookie"]);
				 */ 
			}
		}

		[DllImport("wininet.dll", CharSet=CharSet.Auto , SetLastError=true)] 
		private static extern bool InternetGetCookie (
			string lpszUrl, string lpszCookieName, StringBuilder lpCookieData, ref int lpdwSize);
	
		[DllImport("wininet.dll", CharSet=CharSet.Auto , SetLastError=true)] 
		private static extern bool InternetSetCookie (
			string lpszUrl, string lpszCookieName, string lpszCookieData);

		private static CookieContainer GetCookieContainerUri(Uri url) {
			CookieContainer container = new CookieContainer();
			string cookieHeaders = RetrieveIECookiesForUrl(url.AbsoluteUri);
			if (cookieHeaders.Length > 0) {
				try{
					container.SetCookies(url, cookieHeaders);
				}catch(System.Net.CookieException ce){ //we might get an error on malformed cookies
					_log.Error(String.Format("GetCookieContainerUri() exception parsing '{0}' for url '{1}'", cookieHeaders, url.AbsoluteUri), ce);
				}
			}
			return container;
		}
	

		private static string RetrieveIECookiesForUrl(string url) {
			StringBuilder cookieHeader = new StringBuilder(new String(' ', 256), 256);
			int datasize = cookieHeader.Length;
			if (!InternetGetCookie(url, null, cookieHeader, ref datasize)) {
				if (datasize < 0) 
					return String.Empty;
				cookieHeader = new StringBuilder(datasize); // resize with new datasize
				InternetGetCookie(url, null, cookieHeader, ref datasize);
			}
			return FixupIECookies(cookieHeader);
		}

		/// <summary>
		/// Fixups the cookies IE may return. 
		/// If there is a comma AND a semicolon, we escape the comma
		/// first, then replace the semicolon with a comma (.CLR requires
		/// comma as a cookie separators).
		/// </summary>
		/// <param name="b">The b.</param>
		/// <returns></returns>
		private static string FixupIECookies(StringBuilder b) {
			string s = b.ToString();
			if (s.IndexOf(",") >= 0 && s.IndexOf(";") >= 0) {
				s = s.Replace(",", escapedComma).Replace(";", ",");
			}
			return s;
		}
		private static string escapedComma =HtmlHelper.UrlEncode(",");
	}

	#endregion
}

#region CVS Version Log
/*
 * $Log: AsyncWebRequest.cs,v $
 * Revision 1.55  2007/08/07 19:36:53  carnage4life
 * Fixed issue where unsafe header parsing was disabled when running in .NET 2.0
 *
 * Revision 1.54  2007/08/02 13:36:11  t_rendelmann
 * added support to NewsSubscriptionWizard for rewire the credential step on authentication exceptions
 *
 * Revision 1.53  2007/07/29 15:20:28  carnage4life
 * Removed calls to InternetGetCookie due to issue reportred at http://www.rssbandit.org/forum/topic.asp?whichpage=1&TOPIC_ID=2080&#4080
 *
 * Revision 1.52  2007/07/13 16:05:03  t_rendelmann
 * added a cookie fixup procedure
 *
 * Revision 1.51  2007/06/19 15:01:22  t_rendelmann
 * fixed: [ 1702811 ] Cannot access .treestate.xml (now report a read error as a warning once, but log only at save time during autosave)
 *
 * Revision 1.50  2007/06/15 08:46:33  t_rendelmann
 * fixed: local file feeds were not working anymore (regression)
 *
 * Revision 1.49  2006/12/20 00:11:34  carnage4life
 * Made fix so that OnRequestProgress is now called
 *
 * Revision 1.48  2006/12/19 04:39:51  carnage4life
 * Made changes to AsyncRequest and RequestThread to become instance based instead of static
 *
 * Revision 1.47  2006/12/12 01:16:25  carnage4life
 * Disabled usage of HTTP pipelining
 *
 * Revision 1.46  2006/11/23 11:20:29  t_rendelmann
 * applied a fix to reduce resource leak on internet connections for CLR 1.0/1.1
 *
 * Revision 1.45  2006/10/17 11:39:12  t_rendelmann
 * fixed: the ZipException is not anymore specific enough to catch the wired Deflater exception (for fallback) with the new ziplib version 0.84. using the SharpZipBaseException enstead seems to correct this to get the old working behavior
 *
 * Revision 1.44  2006/10/17 10:39:55  t_rendelmann
 * fixed: relative urls on redirects not handled in AsyncWebRequest.GetSyncResponseStream() that is also called from within RssLocator
 *
 * Revision 1.43  2006/10/10 15:07:22  carnage4life
 * Referenced correct version of #ziplib assembly
 *
 * Revision 1.42  2006/10/10 15:03:11  carnage4life
 * Merged differences between local machine and CVS versions
 *
 * Revision 1.41  2006/10/10 12:42:04  carnage4life
 * Fixed some minor issues
 *
 * Revision 1.40  2006/10/05 06:24:48  carnage4life
 * Added call to FinalizeWebRequest() in TimeoutCallback()
 *
 * Revision 1.39  2006/08/08 17:06:19  carnage4life
 * Added debug statements to track feed mixup issues
 *
 * Revision 1.38  2006/08/08 10:17:24  t_rendelmann
 * changed: made CVS Log a separate code region
 *
 * Revision 1.37  2006/08/08 10:14:07  t_rendelmann
 * changed: replaced CVS Revision key word by the CVS log message supplied during commit
 */
#endregion
