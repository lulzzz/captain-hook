namespace CaptainHook.Common
{
    /// <remarks>
    /// Copied directly from Checkout-API.
    ///     It will definitely change once we get to EDA v1.
    /// </remarks>>
    public class HttpResponseDto
    {
        ///<summary>StatusCode</summary>
        public int StatusCode { get; set; }

        ///<summary>Content</summary>
        public string Content { get; set; }
    }
}
