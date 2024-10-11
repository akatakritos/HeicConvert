// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using HeicConvert.Cli;
using HeicConvert.Core;
using HeicConvert.Core.Actors;

// setup load path
LibHeifSharpDllImportResolver.Register();

using var system = ActorSystem.Create("HiecConvert");
var reporter = system.ActorOf(ProgressReporterActor.Props(), "reporter");
var manager = system.ActorOf(ConvertProcessManager.Props(reporter), "manager");

manager.Tell(new StartConverting(Directory.GetCurrentDirectory()));

await manager.WatchAsync();
