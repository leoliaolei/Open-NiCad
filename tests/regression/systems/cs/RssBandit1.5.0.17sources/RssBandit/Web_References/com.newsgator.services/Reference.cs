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
namespace RssBandit.com.newsgator.services {
    using System.Diagnostics;
    using System.Xml.Serialization;
    using System;
    using System.Web.Services.Protocols;
    using System.ComponentModel;
    using System.Web.Services;
    
    
    /// <remarks/>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="LocationWebServiceSoap", Namespace="http://services.newsgator.com/svc/Location.asmx")]
    public class LocationWebService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        public NGAPIToken NGAPITokenValue;
        
        /// <remarks/>
        public LocationWebService() {
            this.Url = "http://services.newsgator.com/ngws/svc/Location.asmx";
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Location.asmx/GetLocations", RequestNamespace="http://services.newsgator.com/svc/Location.asmx", ResponseNamespace="http://services.newsgator.com/svc/Location.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public Location[] GetLocations() {
            object[] results = this.Invoke("GetLocations", new object[0]);
            return ((Location[])(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetLocations(System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetLocations", new object[0], callback, asyncState);
        }
        
        /// <remarks/>
        public Location[] EndGetLocations(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((Location[])(results[0]));
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Location.asmx/CreateLocation", RequestNamespace="http://services.newsgator.com/svc/Location.asmx", ResponseNamespace="http://services.newsgator.com/svc/Location.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public int CreateLocation(string name, bool autoAddSubs) {
            object[] results = this.Invoke("CreateLocation", new object[] {
                        name,
                        autoAddSubs});
            return ((int)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginCreateLocation(string name, bool autoAddSubs, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("CreateLocation", new object[] {
                        name,
                        autoAddSubs}, callback, asyncState);
        }
        
        /// <remarks/>
        public int EndCreateLocation(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((int)(results[0]));
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Location.asmx/DeleteLocation", RequestNamespace="http://services.newsgator.com/svc/Location.asmx", ResponseNamespace="http://services.newsgator.com/svc/Location.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void DeleteLocation(int id) {
            this.Invoke("DeleteLocation", new object[] {
                        id});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginDeleteLocation(int id, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("DeleteLocation", new object[] {
                        id}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndDeleteLocation(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Location.asmx/Update", RequestNamespace="http://services.newsgator.com/svc/Location.asmx", ResponseNamespace="http://services.newsgator.com/svc/Location.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void Update(Location location) {
            this.Invoke("Update", new object[] {
                        location});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginUpdate(Location location, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("Update", new object[] {
                        location}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndUpdate(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("NGAPITokenValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://services.newsgator.com/svc/Location.asmx/GetUnreadCount", RequestNamespace="http://services.newsgator.com/svc/Location.asmx", ResponseNamespace="http://services.newsgator.com/svc/Location.asmx", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public int GetUnreadCount(string name) {
            object[] results = this.Invoke("GetUnreadCount", new object[] {
                        name});
            return ((int)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetUnreadCount(string name, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetUnreadCount", new object[] {
                        name}, callback, asyncState);
        }
        
        /// <remarks/>
        public int EndGetUnreadCount(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((int)(results[0]));
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://services.newsgator.com/svc/Location.asmx")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://services.newsgator.com/svc/Location.asmx", IsNullable=false)]
    public class NGAPIToken : System.Web.Services.Protocols.SoapHeader {
        
        /// <remarks/>
        public string Token;
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://services.newsgator.com/svc/Location.asmx")]
    public class Location {
        
        /// <remarks/>
        public int id;
        
        /// <remarks/>
        public string name;
        
        /// <remarks/>
        public bool contentOnline;
        
        /// <remarks/>
        public bool autoAddSubs;
    }
}