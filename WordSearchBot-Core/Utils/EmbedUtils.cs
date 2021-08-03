using System.Reflection;
using Discord;

namespace WordSearchBot.Core.Utils {
    public static class EmbedFooter {

        public static void AddFooter(EmbedBuilder eb) {
            string informationalVersion = ((AssemblyInformationalVersionAttribute)Assembly
                    .GetAssembly(typeof(EmbedFooter))
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0])
                .InformationalVersion;
            eb.WithFooter($"EmbedBuilder, version {informationalVersion}");
        }

    }
}