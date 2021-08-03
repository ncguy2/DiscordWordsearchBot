using System.Reflection;
using Discord;
using WordSearchBot.Core.System;

namespace WordSearchBot.Core.Utils {
    public static class EmbedUtils {
        public static EmbedBuilder ExternalEmbed() {
            return AddFooter(new EmbedBuilder());
        }

        public static EmbedBuilder AddFooter(EmbedBuilder eb) {
            string informationalVersion = ((GitRevisionAttribute)Assembly
                                                                 .GetAssembly(typeof(EmbedUtils))
                                                                 ?.GetCustomAttributes(
                                                                     typeof(GitRevisionAttribute), false)[0])?.Hash;
            return eb.WithFooter($"EmbedBuilder, version {informationalVersion}");
        }
    }
}