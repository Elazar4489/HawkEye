using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.Configure<MongoOptions>(
    builder.Configuration.GetSection("Mongo"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

builder.Services.AddSingleton<IFlightStore, MongoFlightStore>();

var host = builder.Build();

await host.RunAsync();