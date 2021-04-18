using System;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace WordSearchBot.Core {
    public abstract class Module {

        protected Core AssignedCore;

        public string CacheDir() {
            string cacheDirRoot = Core.CACHE_DIR_ROOT + "/" + InternalName();
            if (!Directory.Exists(cacheDirRoot))
                Directory.CreateDirectory(cacheDirRoot);
            return cacheDirRoot;
        }

        public string CacheFile(string fileRef) {
            string r = CacheDir() + "/" + fileRef;
            return r;
        }

        public async Task Log(Core.LogLevel level, string message) {
            await AssignedCore.Log(level, message, author: DisplayName());
        }

        public abstract string DisplayName();
        public abstract string InternalName();
        public abstract void RegisterEvents(DiscordContext context);

        public void AssignCore(Core core) {
            this.AssignedCore = core;
        }

        public void Throw(string msg) {
            throw new ModuleException(this, msg);
        }

    }

    public class ModuleException : Exception {

        public Module throwingModule;

        public ModuleException(Module module, string? message) : base(message) {
            throwingModule = module;
        }
    }

}