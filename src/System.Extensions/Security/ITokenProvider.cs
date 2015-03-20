namespace System.Security
{
    public interface ITokenProvider
    {
        string GetToken(int length);
    }
}