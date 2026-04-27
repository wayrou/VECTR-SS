using System;
using System.Collections.Generic;

namespace GTX.Terminal
{
    public sealed class GTXQuacCommandRegistry
    {
        private readonly Dictionary<string, Func<string[], GTXQuacCommandResult>> handlers = new Dictionary<string, Func<string[], GTXQuacCommandResult>>();

        public GTXQuacCommandRegistry()
        {
            Register("/dev", _ => GTXQuacCommandResult.Info("Q.U.A.C.", "Commands: /dev, /help, /lobby, /mod, /loom, /tune."));
            Register("/help", _ => GTXQuacCommandResult.Info("Q.U.A.C.", "GTX command surface placeholder. Multiplayer and Loom commands are planned contracts."));
            Register("/lobby", _ => GTXQuacCommandResult.Info("LOBBY", "Lobby command contract reserved: create, join, rules, ready."));
            Register("/mod", _ => GTXQuacCommandResult.Info("MOD", "Customization package command contract reserved: list, load, verify."));
            Register("/loom", _ => GTXQuacCommandResult.Info("LOOM", "Loom command contract reserved: bind, run, unbind, inspect."));
            Register("/tune", _ => GTXQuacCommandResult.Info("TUNE", "Tuning command contract reserved: preset and parameter edits."));
        }

        public void Register(string command, Func<string[], GTXQuacCommandResult> handler)
        {
            if (string.IsNullOrWhiteSpace(command) || handler == null)
            {
                return;
            }

            handlers[Normalize(command)] = handler;
        }

        public GTXQuacCommandResult Run(string rawCommand)
        {
            if (string.IsNullOrWhiteSpace(rawCommand))
            {
                return GTXQuacCommandResult.Error("EMPTY", "No command entered.");
            }

            string[] parts = rawCommand.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = Normalize(parts[0]);
            if (!handlers.TryGetValue(command, out Func<string[], GTXQuacCommandResult> handler))
            {
                return GTXQuacCommandResult.Error("UNKNOWN", $"Unknown QUAC command: {command}");
            }

            string[] args = new string[Math.Max(0, parts.Length - 1)];
            Array.Copy(parts, 1, args, 0, args.Length);
            return handler(args);
        }

        private static string Normalize(string command)
        {
            return command.Trim().ToLowerInvariant();
        }
    }

    public readonly struct GTXQuacCommandResult
    {
        public GTXQuacCommandResult(bool handled, bool success, string title, string message)
        {
            Handled = handled;
            Success = success;
            Title = title;
            Message = message;
        }

        public bool Handled { get; }
        public bool Success { get; }
        public string Title { get; }
        public string Message { get; }

        public static GTXQuacCommandResult Info(string title, string message)
        {
            return new GTXQuacCommandResult(true, true, title, message);
        }

        public static GTXQuacCommandResult Error(string title, string message)
        {
            return new GTXQuacCommandResult(true, false, title, message);
        }
    }
}
