using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Test.GrainInterfaces;
using Orleans.Providers.MongoDB.Test.Grains;

namespace Orleans.Providers.MongoDB.Test.Host
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var silo = new SiloHostBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(EmployeeGrain).Assembly).WithReferences();
                })
                .UseMongoDBClustering(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .AddStartupTask(async (s, ct) =>
                {
                    var grainFactory = s.GetRequiredService<IGrainFactory>();

                    await grainFactory.GetGrain<IHelloWorldGrain>((int)DateTime.UtcNow.TimeOfDay.Ticks).SayHello("HI");
                })
                .UseMongoDBReminders(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .AddMongoDBGrainStorage("MongoDBStore", options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "helloworldcluster";
                    options.ServiceId = "helloworldcluster";
                })
                .ConfigureEndpoints(IPAddress.Loopback, 11111, 30000)
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            silo.StartAsync().Wait();

            var client = new ClientBuilder()
                .ConfigureApplicationParts(options =>
                {
                    options.AddApplicationPart(typeof(IHelloWorldGrain).Assembly);
                })
                .UseMongoDBClustering(options =>
                {
                    options.ConnectionString = "mongodb://localhost/OrleansTestApp";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "helloworldcluster";
                    options.ServiceId = "helloworldcluster";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            client.Connect().Wait();

            // get a reference to the grain from the grain factory
            var helloWorldGrain = client.GetGrain<IHelloWorldGrain>(1);

            // call the grain
            helloWorldGrain.SayHello("World").Wait();

            var reminderGrain = client.GetGrain<INewsReminderGrain>(1);

            reminderGrain.StartReminder("TestReminder", TimeSpan.FromMinutes(10)).Wait();

            // Test State 
            var employee = client.GetGrain<IEmployeeGrain>(1);
            var employeeId = employee.ReturnLevel().Result;

            if (employeeId == 100)
            {
                employee.SetLevel(50);
            }
            else
            {
                employee.SetLevel(100);
            }

            employeeId = employee.ReturnLevel().Result;

            Console.WriteLine(employeeId);
            Console.ReadKey();
            
            silo.StopAsync().Wait();
        }
    }
}