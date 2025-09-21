using System.Net;

namespace ListeDeCourses.Api.Common
{
    public class DomainException : Exception
    {
        public string? Code { get; }
        public HttpStatusCode? HttpStatus { get; }

        public DomainException(string message, string? code = null, HttpStatusCode? httpStatus = null)
            : base(message)
        {
            Code = code;
            HttpStatus = httpStatus;
        }

        public DomainException(string message, Exception inner, string? code = null, HttpStatusCode? httpStatus = null)
            : base(message, inner)
        {
            Code = code;
            HttpStatus = httpStatus;
        }
    }
}
