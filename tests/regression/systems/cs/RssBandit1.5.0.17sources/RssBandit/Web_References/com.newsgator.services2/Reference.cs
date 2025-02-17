//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 1.1.4322.2032.
// 
namespace RssBandit.com.newsgator.services2 {
    using System.Diagnostics;
    using System.Xml.Serialization;
    using System;
    using System.Web.Services.Protocols;
    using System.ComponentModel;
    using System.Web.Services;
    
    
    /// <remarks/>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="FeedWebServiceSoap", Namespace="http://services.newsgator.com/svc/Feed.asmx")]
    public class FeedWebService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        public NGAPIToken NGAPITokenValue;
        
        /// <remarks/>
        public FeedWebService() {
            this.Url = "http://services.newsgator.com/ngws/svc/Feed.asmx";
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Feed.asmx/GetNews", RequestNamespace="http://services.newsgator.com/svc/Feed.asmx", ResponseNamespace="http://services.newsgator.com/svc/Feed.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public System.Xml.XmlElement GetNews(int feedId, string locationName, string token, bool unreadOnly) {
            object[] results = this.Invoke("GetNews", new object[] {
                        feedId,
                        locationName,
                        token,
                        unreadOnly});
            return ((System.Xml.XmlElement)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetNews(int feedId, string locationName, string token, bool unreadOnly, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetNews", new object[] {
                        feedId,
                        locationName,
                        token,
                        unreadOnly}, callback, asyncState);
        }
        
        /// <remarks/>
        public System.Xml.XmlElement EndGetNews(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((System.Xml.XmlElement)(results[0]));
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Feed.asmx/MarkRead", RequestNamespace="http://services.newsgator.com/svc/Feed.asmx", ResponseNamespace="http://services.newsgator.com/svc/Feed.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void MarkRead(int feedId, string locationName, bool read, string syncToken) {
            this.Invoke("MarkRead", new object[] {
                        feedId,
                        locationName,
                        read,
                        syncToken});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginMarkRead(int feedId, string locationName, bool read, string syncToken, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("MarkRead", new object[] {
                        feedId,
                        locationName,
                        read,
                        syncToken}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndMarkRead(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Feed.asmx/MarkFeedsRead", RequestNamespace="http://services.newsgator.com/svc/Feed.asmx", ResponseNamespace="http://services.newsgator.com/svc/Feed.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void MarkFeedsRead(int[] feedIds, string locationName) {
            this.Invoke("MarkFeedsRead", new object[] {
                        feedIds,
                        locationName});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginMarkFeedsRead(int[] feedIds, string locationName, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("MarkFeedsRead", new object[] {
                        feedIds,
                        locationName}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndMarkFeedsRead(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Feed.asmx/GetFeedInfoSummaryFromXmlUrl", RequestNamespace="http://services.newsgator.com/svc/Feed.asmx", ResponseNamespace="http://services.newsgator.com/svc/Feed.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public FeedInfoSummary[] GetFeedInfoSummaryFromXmlUrl(string[] xmlurls) {
            object[] results = this.Invoke("GetFeedInfoSummaryFromXmlUrl", new object[] {
                        xmlurls});
            return ((FeedInfoSummary[])(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetFeedInfoSummaryFromXmlUrl(string[] xmlurls, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetFeedInfoSummaryFromXmlUrl", new object[] {
                        xmlurls}, callback, asyncState);
        }
        
        /// <remarks/>
        public FeedInfoSummary[] EndGetFeedInfoSummaryFromXmlUrl(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((FeedInfoSummary[])(results[0]));
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://services.newsgator.com/svc/Feed.asmx")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://services.newsgator.com/svc/Feed.asmx", IsNullable=false)]
    public class NGAPIToken : System.Web.Services.Protocols.SoapHeader {
        
        /// <remarks/>
        public string Token;
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://services.newsgator.com/svc/Feed.asmx")]
    public class FeedInfoSummary {
        
        /// <remarks/>
        public int FeedID;
        
        /// <remarks/>
        public string Title;
        
        /// <remarks/>
        public string Description;
        
        /// <remarks/>
        public string XmlUrl;
        
        /// <remarks/>
        public string HtmlLink;
    }
}
