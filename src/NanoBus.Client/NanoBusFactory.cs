namespace NanoBus.Client
{
    public class NanoBusFactory : INanoBusFactory
    {
        private readonly string _masterNanoBusConnectionString;
        private INanoBus _serviceBus;

        public NanoBusFactory(string masterNanoBusConnectionString)
        {
            _masterNanoBusConnectionString = masterNanoBusConnectionString;
        }

        public INanoBus GetNanoBus()
        {
            return _serviceBus ?? (_serviceBus = new NanoBusClient());
        }
    }
}