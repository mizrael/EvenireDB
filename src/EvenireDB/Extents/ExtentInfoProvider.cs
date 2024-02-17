namespace EvenireDB.Extents
{
    internal class ExtentInfoProvider : IExtentInfoProvider
    {
        private readonly ExtentInfoProviderConfig _config;

        public ExtentInfoProvider(ExtentInfoProviderConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (!Directory.Exists(_config.BasePath))
                Directory.CreateDirectory(config.BasePath);
        }

        public ExtentInfo GetLatest(Guid streamId)
        {
            // TODO: tests
            var key = streamId.ToString("N");
            int extentNumber = 0; // TODO: calculate
            return new ExtentInfo
            {
                DataPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_data.dat"),
                HeadersPath = Path.Combine(_config.BasePath, $"{key}_{extentNumber}_headers.dat"),
            };
        }
    }
}