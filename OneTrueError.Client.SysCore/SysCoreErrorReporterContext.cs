using OneTrueError.Client.Reporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneTrueError.Client.SysCore
{
    public class SysCoreErrorReporterContext : IErrorReporterContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="AspNetContext"/>.
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="exception"></param>
        /// <param name="httpContext"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SysCoreErrorReporterContext(object reporter, Exception exception, object httpContext)
        {
            if (reporter == null) throw new ArgumentNullException("reporter");
            if (exception == null) throw new ArgumentNullException("exception");
            Exception = exception;
            HttpContext = httpContext;
            Reporter = reporter;
        }

        /// <summary>
        /// Http context
        /// </summary>
        public object HttpContext { get; private set; }

        /// <inheritdoc/>
        public Exception Exception { get; private set; }

        /// <inheritdoc/>
        public object Reporter { get; private set; }
    }
}
