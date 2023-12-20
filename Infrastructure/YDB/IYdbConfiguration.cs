namespace Infrastructure.YDB;

public interface IYdbConfiguration
{
    public string YdbEndpoint { get; }
    public string YdbPath { get; }
    public string? IamTokenPath { get; }
}