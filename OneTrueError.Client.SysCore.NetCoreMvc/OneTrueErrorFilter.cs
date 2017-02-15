using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;


namespace OneTrueError.Client.SysCore.NetCoreMvc
{
    using Microsoft.AspNetCore.Http;
    using System.Threading;

    public abstract class OneTrueErrorFilter : ExceptionFilterAttribute
    {
        private OneTrueCollector OneTrueCollector { get; set; }

        public override void OnException(ExceptionContext context)
        {
            if (Dismiss(context))
                return;
            this.OneTrueCollector = new OneTrueCollector();
            DefaultCollection(context);
            ElmahCollection(context);
            CustomCollection(OneTrueCollector, context);


            string errorId = null;
            try
            {
                errorId = OneTrueProxy.UploadMvcCollector(this.OneTrueCollector, this, context.Exception, context.HttpContext);
            }
            catch (Exception ex)
            {
                OnReportingException(context, ex);
            }
            context.HttpContext.Response.StatusCode = 500;
            context.Result = GetErrorView(context, errorId);
        }

        #region Private
        private void DefaultCollection(ExceptionContext filterContext)
        {
            var mvcDict = new Dictionary<string, string>();
            mvcDict.Add("DisplayName", filterContext.ActionDescriptor.DisplayName);
            foreach (var routeValue in filterContext.ActionDescriptor.RouteValues)
            {
                mvcDict.Add(routeValue.Key, routeValue.Value);
            }
            foreach (var key in filterContext.ModelState.Keys)
            {
                mvcDict.Add(key, filterContext.ModelState[key].AttemptedValue);
            }
            mvcDict.Add("UserNo", Thread.CurrentPrincipal.Identity.Name);
            OneTrueCollector.CollectDictionary("Controller", mvcDict);

            if (filterContext.Result != null && !(filterContext.Result is EmptyResult))
                OneTrueCollector.Collect("Result", filterContext.Result);
            if (filterContext.RouteData != null)
                OneTrueCollector.Collect("RouteData", filterContext.RouteData);
        }

        private void ElmahCollection(ExceptionContext filterContext)
        {
            var svrDict = new Dictionary<string, string>();
            svrDict.Add(nameof(Environment.MachineName), Environment.MachineName);
            svrDict.Add(nameof(filterContext.HttpContext.Request.Host.Host), filterContext.HttpContext.Request.Host.Host);
            svrDict.Add(nameof(filterContext.HttpContext.Request.Host.Port), filterContext.HttpContext.Request.Host.Port.ToString());
            svrDict.Add(nameof(filterContext.HttpContext.Request.Method), filterContext.HttpContext.Request.Method);
            svrDict.Add(nameof(filterContext.HttpContext.Request.Path), filterContext.HttpContext.Request.Path.ToString());
            svrDict.Add(nameof(filterContext.HttpContext.Request.Scheme), filterContext.HttpContext.Request.Scheme);
            svrDict.Add(nameof(filterContext.HttpContext.Connection.LocalIpAddress), filterContext.HttpContext.Connection.LocalIpAddress.ToString());
            svrDict.Add(nameof(filterContext.HttpContext.Connection.RemoteIpAddress), filterContext.HttpContext.Connection.RemoteIpAddress.ToString());
            OneTrueCollector.CollectDictionary("ServerVariables", svrDict);

            var headerDict = new Dictionary<string, string>();
            var header = filterContext.HttpContext.Request.Headers;
            foreach (var key in header.Keys)
            {
                if (key == "Cookie")
                    continue;
                headerDict.Add(key, header[key]);
            }
            OneTrueCollector.Collect("Header", headerDict);


            if (filterContext.HttpContext.Request.QueryString.HasValue)
                OneTrueCollector.Collect("QueryString", filterContext.HttpContext.Request.QueryString);

            if (filterContext.HttpContext.Request.Query.Count > 0)
                OneTrueCollector.Collect("RequestQuery", filterContext.HttpContext.Request.Query);

            if (filterContext.HttpContext.Request.Cookies.Count > 0)
                OneTrueCollector.Collect("Cookie", filterContext.HttpContext.Request.Cookies);
            try
            {
                if (filterContext.HttpContext.Request.Form.Count > 0)
                    OneTrueCollector.Collect("Form", filterContext.HttpContext.Request.Form);

            }
            catch
            {

            }

            try
            {
                if (filterContext.HttpContext.Request.Body.Length > 0)
                {
                    var contentType = filterContext.HttpContext.Request.ContentType;
                    if (contentType.Contains("application/json") || contentType.Contains("application/xml") || contentType.Contains("text/xml"))
                    {
                        filterContext.HttpContext.Request.Body.Position = 0;
                        System.IO.StreamReader reader = new System.IO.StreamReader(filterContext.HttpContext.Request.Body);
                        string requestFromPost = reader.ReadToEnd();
                        OneTrueCollector.Collect("RequestPayload", requestFromPost);
                    }
                }
            }
            catch
            {

            }

        }
        #endregion

        #region Virtual and Abstract
        /// <summary>
        /// 是否略過
        /// </summary>
        /// <param name="filterContext"></param>
        /// <returns></returns>
        protected virtual bool Dismiss(ExceptionContext filterContext)
        {
            return false;
        }

        /// <summary>
        /// 取得View for Error display
        /// </summary>
        /// <returns></returns>
        protected abstract IActionResult GetErrorView(ExceptionContext filterContext, string errorId);

        /// <summary>
        /// 當收集發生失敗時
        /// </summary>
        /// <param name="filterContext"></param>
        /// <param name="ex"></param>
        protected virtual void OnReportingException(ExceptionContext filterContext, Exception ex)
        {
        }

        /// <summary>
        /// 自訂收集
        /// </summary>
        /// <param name="items"></param>
        /// <param name="converter"></param>
        /// <param name="filterContext"></param>
        protected virtual void CustomCollection(OneTrueCollector collector, ExceptionContext filterContext)
        {
        }
        #endregion

    }

}

