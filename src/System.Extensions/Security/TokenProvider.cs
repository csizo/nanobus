namespace System.Security
{
    public static class TokenProvider
    {
        public static readonly ITokenProvider Default = new RngTokenProvider();
    }
}