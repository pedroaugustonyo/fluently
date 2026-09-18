using DotNetEnv;
using Fluently.API;

Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddApplication();

var app = builder.Build();

app.UseApplication();

app.MapApplicationRoutes();

app.Run();
