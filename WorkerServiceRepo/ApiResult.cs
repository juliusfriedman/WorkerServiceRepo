using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    /// <summary>
    /// Used to encapsualte a result from this controller.
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        ///  The default message for complete
        /// </summary>
        protected static string DefaultCompleteMessage = "Complete";

        /// <summary>
        ///  The default message for success
        /// </summary>
        protected static string DefaultSuccessMessage = "Success";

        /// <summary>
        ///  The default message for error.
        /// </summary>
        protected static string DefaultErrorMessage = "Error";

        /// <summary>
        /// Saves allocations
        /// </summary>
        public static ApiResult ErrorResult = new ApiResult { Error = true, Message = ApiResult.DefaultErrorMessage };

        /// <summary>
        /// Saves allocations
        /// </summary>
        public static ApiResult CompleteResult = new ApiResult { Message = ApiResult.DefaultCompleteMessage };

        /// <summary>
        /// Saves allocations
        /// </summary>
        public static ApiResult SuccessResult = new ApiResult { Message = ApiResult.DefaultSuccessMessage };

        /// <summary>
        /// Optional message supplied to user
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Indicates if the result represents an error
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ApiResult() { }
    }

    /// <summary>
    /// Adds a generic type to <see cref="ApiResult"/>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class ApiResult<T> : ApiResult
    {
        /// <summary>
        /// The result returned to the user
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ApiResult() : base() { }

        /// <summary>
        /// Creates the instance and assigns <see cref="Result"/>
        /// </summary>
        /// <param name="result">The result</param>
        public ApiResult(T result = default) : this()
        {
            Result = result;

            Message = DefaultSuccessMessage;
        }
    }

    //public class Controller
    //{

    //    [HttpGet("/subscribe/{email}")]
    //    [HttpPut("/subscribe/{email}")]
    //    public ActionResult SubscribeEmail(string email)
    //    {
    //        if (string.IsNullOrWhiteSpace(email) || false == email.Contains('@')) return StatusCode(412, new ApiResult
    //        {
    //            Error = true,
    //            Message = "a valid email is required and cannot be empty or consist of only whitespace."
    //        });

    //        //Todo MailAddress.TryCreate
    //        try
    //        {
    //            //LogMessage($"Valid Email Found = {mailAddress.Address}");
    //        }
    //        catch
    //        {
    //            //LogMessage($"Invalid Email Found = {email}");

    //            return StatusCode(412, new ApiResult
    //            {
    //                Error = true,
    //                Message = "a valid email is required.."
    //            });
    //        }

    //        return StatusCode(200, new ApiResult
    //        {
    //            Message = $"Success, {email} has been subscribed."
    //        });

    //    }
    //}

}
