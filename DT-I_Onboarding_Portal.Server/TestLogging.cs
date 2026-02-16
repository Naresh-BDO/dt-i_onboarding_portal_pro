using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

public static class TestLogging
{
    public static ILoggerFactory CreateSqlLoggerFactory(string connectionString)
    {
        var columnOptions = new ColumnOptions();
        columnOptions.Store.Remove(StandardColumn.Properties);
        columnOptions.Store.Add(StandardColumn.LogEvent);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProperty("App", "DT-I-Onboarding-Portal.Tests")
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "AppLogs",
                    AutoCreateSqlTable = true
                },
                columnOptions: columnOptions)
            .CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }
}