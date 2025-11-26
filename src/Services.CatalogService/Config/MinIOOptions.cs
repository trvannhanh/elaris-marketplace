namespace Services.CatalogService.Config
{
    public class MinIOOptions
    {
        public string Endpoint { get; set; } = null!;
        public string AccessKey { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public bool UseSSL { get; set; }
        public string BaseUrl { get; set; } = null!;
    }
}
