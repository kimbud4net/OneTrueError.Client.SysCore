using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneTrueError.Client.SysCore.NetCoreMvc.Demo
{
    public class GlobalExceptionFilter : OneTrueErrorFilter
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        public GlobalExceptionFilter(IHostingEnvironment hostingEnvironment, IModelMetadataProvider modelMetadataProvider)
        {
            _hostingEnvironment = hostingEnvironment;
            _modelMetadataProvider = modelMetadataProvider;

        }

        protected override IActionResult GetErrorView(ExceptionContext filterContext, string errorId)
        {
            var result = new ViewResult { ViewName = "Error500" };
            var viewData = new ViewDataDictionary(_modelMetadataProvider, filterContext.ModelState);
            result.ViewData = viewData;
            result.ViewData["Test"] = "From GlobalExceptionFilter";
            return result;
        }
        protected override bool Dismiss(ExceptionContext filterContext)
        {
            return false;
        }

        protected override void CustomCollection(OneTrueCollector collector, ExceptionContext filterContext)
        {
            Dictionary<string, string> customDict = new Dictionary<string, string>();
            customDict.Add("CustNameNull", null);
            customDict.Add("CustName", "Tester");
            collector.CollectDictionary("Custom", customDict);
        }
    }
}
