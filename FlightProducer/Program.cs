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


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

var host = builder.Build();

await host.RunAsync();